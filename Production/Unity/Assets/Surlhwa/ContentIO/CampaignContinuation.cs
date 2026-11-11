using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Surlhwa.Domain;
using static Surlhwa.ContentIO.StrictJson;

namespace Surlhwa.ContentIO
{
    /// <summary>Source-preserving continuation from accepted ACT 1 through ACT 6 packages.</summary>
    public sealed class CampaignContinuation
    {
        public const string AcceptedActOneHash="0d347e6976825ba61ab6a9fec39c73decc8f13ac10c5f0bf4d80558b1f33e3d4";
        public const string AcceptedActTwoHash="e38fe3dce7cdcb004dd0731df730925c2531562379fda7fde95d922c0d620fa1";
        public const string AcceptedActThreeHash="9db4998cd4f3dd8fc1c0371e8396504c07c0cce253e504caf39f48ec370555f0";
        public const string AcceptedActFourHash="fcc2aeb9c4269c2c9fd34018e4c82b79553ffc3645ad2b8be3970abd673ad6fe";
        public const string AcceptedActFiveHash="bc76a5546c7f30d6dfa417fd3754a81fe4c52f8040b15bc86b42c1a954f34339";
        public const string AcceptedActSixHash="e6bb7131fc5424186bc2cf4b1b15160b8cead275bcf7c9824ee1ccd1bfb5dfcf";
        public CampaignContent SourceContent { get; }
        public CampaignContent ExpandedContent { get; }

        public CampaignContinuation(string acceptedSourceJson,string expandedJson)
        {
            string sourceHash=acceptedSourceJson==null ? null : Hash(acceptedSourceJson);
            if(sourceHash!=AcceptedActOneHash && sourceHash!=AcceptedActTwoHash && sourceHash!=AcceptedActThreeHash && sourceHash!=AcceptedActFourHash && sourceHash!=AcceptedActFiveHash && sourceHash!=AcceptedActSixHash)
                throw Invalid("Continuation requires exact accepted source campaign definitions.");
            SourceContent=CampaignContentLoader.Load(acceptedSourceJson);
            ExpandedContent=CampaignContentLoader.Load(expandedJson);
            bool supported=SourceContent.Scope=="act_01" && (ExpandedContent.Scope=="through_act_02" || ExpandedContent.Scope=="through_act_03" || ExpandedContent.Scope=="through_act_04" || ExpandedContent.Scope=="through_act_05" || ExpandedContent.Scope=="through_act_06" || ExpandedContent.Scope=="through_act_07") ||
                SourceContent.Scope=="through_act_02" && (ExpandedContent.Scope=="through_act_03" || ExpandedContent.Scope=="through_act_04" || ExpandedContent.Scope=="through_act_05" || ExpandedContent.Scope=="through_act_06" || ExpandedContent.Scope=="through_act_07") ||
                SourceContent.Scope=="through_act_03" && (ExpandedContent.Scope=="through_act_04" || ExpandedContent.Scope=="through_act_05" || ExpandedContent.Scope=="through_act_06" || ExpandedContent.Scope=="through_act_07") ||
                SourceContent.Scope=="through_act_04" && (ExpandedContent.Scope=="through_act_05" || ExpandedContent.Scope=="through_act_06" || ExpandedContent.Scope=="through_act_07") ||
                SourceContent.Scope=="through_act_05" && (ExpandedContent.Scope=="through_act_06" || ExpandedContent.Scope=="through_act_07") ||
                SourceContent.Scope=="through_act_06" && ExpandedContent.Scope=="through_act_07";
            if(!supported || ExpandedContent.Slots.Count<=SourceContent.Slots.Count)
                throw Invalid("Unsupported campaign continuation boundary.");
            ValidatePrefix(Parse(acceptedSourceJson),Parse(expandedJson));
            for(int slot=0;slot<SourceContent.Slots.Count;slot++)
                if(!SourceContent.AvailableSupplyAt(slot).SequenceEqual(ExpandedContent.AvailableSupplyAt(slot)))
                    throw Invalid("Continuation changes accepted source campaign physical supply.");
        }

        private static void ValidatePrefix(JObject source,JObject expanded)
        {
            foreach(var property in source.Properties())
            {
                string key=property.Name;
                if(key=="schemaVersion" || key=="act" || key=="contentRevision") continue;
                if(property.Value is JArray oldRows && key!="known")
                {
                    var newRows=expanded[key] as JArray;
                    if(newRows==null || newRows.Count<oldRows.Count) throw Invalid("Continuation removes accepted "+key+".");
                    for(int index=0;index<oldRows.Count;index++)
                    {
                        var candidate=newRows[index].DeepClone();
                        if(key=="encounters")
                        {
                            var encounter=(JObject)candidate;
                            foreach(string field in new[]{"companionGuestId","memoryId","memoryVariant","discoveryRetryTextId"})
                            {
                                if(((JObject)oldRows[index]).Property(field)!=null) continue;
                                if(encounter.Property(field)!=null && encounter[field].Type!=JTokenType.Null) throw Invalid("Continuation changes accepted encounter ownership.");
                                encounter.Remove(field);
                            }
                        }
                        if(!JToken.DeepEquals(oldRows[index],candidate)) throw Invalid("Continuation changes accepted "+key+".");
                    }
                }
                else if(!JToken.DeepEquals(property.Value,expanded[key])) throw Invalid("Continuation changes accepted "+key+".");
            }
        }

        public CampaignCheckpoint Convert(string legacySaveJson)
        {
            // Decode with the original definition first; a caller cannot replace an arbitrary hash.
            var source=CampaignSaveCodec.Decode(legacySaveJson,SourceContent);
            bool completed=source.Phase==CampaignPhase.Complete;
            var selected=source.SelectedEncounters.ToList();
            if(completed)
            {
                var next=ExpandedContent.Slots[SourceContent.Slots.Count];
                selected.Add(next.WhenKnownRecipe!=null && source.KnownReceipts.ContainsKey(next.WhenKnownRecipe) ? next.FallbackId : next.EncounterId);
            }
            if(completed && source.Sequence==long.MaxValue) throw Invalid("Campaign sequence cannot advance.");
            var continued=new CampaignCheckpoint(completed ? source.Sequence+1 : source.Sequence,source.RunId,
                completed ? SourceContent.Slots.Count : source.SlotIndex,completed ? CampaignPhase.NightEnd : source.Phase,
                completed ? 0 : source.LineIndex,source.PrinceChoice,source.Farewell,selected,source.ConsumedLines,source.CompletedOccurrences,
                source.Services,source.Memories,source.KnownReceipts.ToDictionary(x=>x.Key,x=>x.Value),source.Facts.ToDictionary(x=>x.Key,x=>x.Value.ToArray()));
            // Validate every preserved receipt under the appended definitions without re-evaluation.
            CampaignSaveCodec.Encode(continued,ExpandedContent);
            return continued;
        }

        public CampaignLoadResult LoadSource(string folder)
        {
            var loaded=new CampaignSaveStore(folder,SourceContent).Load();
            return new CampaignLoadResult(loaded.Checkpoint==null ? null : Convert(CampaignSaveCodec.Encode(loaded.Checkpoint,SourceContent)),
                loaded.RecoveryRequired,loaded.Message);
        }

        public async Task<CampaignLoadResult> LoadOrImportAsync(string legacyFolder,CampaignSaveStore destination,bool acceptRecovery=false)
        {
            if(destination==null) throw new ArgumentNullException(nameof(destination));
            if(destination.Content.BundleHash!=ExpandedContent.BundleHash || destination.Content.Scope!=ExpandedContent.Scope)
                throw Invalid("Continuation destination uses different campaign definitions.");
            // Occupied destination history always wins, even if corrupt or from a future schema.
            if(destination.HasHistory) return destination.Load();
            var source=LoadSource(legacyFolder);
            if(source.Checkpoint==null || (source.RecoveryRequired && !acceptRecovery)) return source;
            await destination.ImportAsync(source.Checkpoint).ConfigureAwait(false);
            return new CampaignLoadResult(source.Checkpoint,false,source.Message);
        }
    }
}
