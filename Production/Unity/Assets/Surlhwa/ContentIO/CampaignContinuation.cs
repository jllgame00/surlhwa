using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Surlhwa.Domain;
using static Surlhwa.ContentIO.StrictJson;

namespace Surlhwa.ContentIO
{
    /// <summary>Source-preserving continuation from the accepted ACT 1 package.</summary>
    public sealed class CampaignContinuation
    {
        public const string AcceptedActOneHash="0d347e6976825ba61ab6a9fec39c73decc8f13ac10c5f0bf4d80558b1f33e3d4";
        public CampaignContent SourceContent { get; }
        public CampaignContent ExpandedContent { get; }

        public CampaignContinuation(string acceptedAct1Json,string expandedJson)
        {
            if(acceptedAct1Json==null || Hash(acceptedAct1Json)!=AcceptedActOneHash)
                throw Invalid("Continuation requires the exact accepted ACT 1 definitions.");
            SourceContent=CampaignContentLoader.Load(acceptedAct1Json);
            ExpandedContent=CampaignContentLoader.Load(expandedJson);
            if(SourceContent.Scope!="act_01" || ExpandedContent.Scope!="through_act_02" || ExpandedContent.Slots.Count<=SourceContent.Slots.Count)
                throw Invalid("Unsupported campaign continuation boundary.");
            ValidatePrefix(Parse(acceptedAct1Json),Parse(expandedJson));
            for(int slot=0;slot<SourceContent.Slots.Count;slot++)
                if(!SourceContent.AvailableSupplyAt(slot).SequenceEqual(ExpandedContent.AvailableSupplyAt(slot)))
                    throw Invalid("Continuation changes accepted ACT 1 physical supply.");
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
                            foreach(string field in new[]{"companionGuestId","memoryId","memoryVariant"})
                            {
                                if(encounter[field]?.Type!=JTokenType.Null) throw Invalid("Continuation changes accepted encounter ownership.");
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
