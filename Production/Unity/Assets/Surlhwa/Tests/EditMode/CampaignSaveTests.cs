using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using Surlhwa.Domain;
using Surlhwa.Application;
using Surlhwa.ContentIO;

namespace Surlhwa.Tests
{
    public sealed class CampaignSaveTests
    {
        [Test] public void CampaignCheckpointRoundTripPreservesCommittedServiceWithoutBrewingAgain()
        {
            var c=CampaignFlowTests.Content(); var store=new CampaignFlowTests.Store(); var s=CampaignSession.StartAsync(c,store).GetAwaiter().GetResult();
            CampaignFlowTests.ReachBrew(s); CampaignFlowTests.Serve(s);
            var restored=CampaignSaveCodec.Decode(CampaignSaveCodec.Encode(s.State,c),c);
            Assert.That(restored,Is.Not.Null);
            Assert.That(restored.Phase,Is.EqualTo(CampaignPhase.Reaction)); Assert.That(restored.CurrentService.Snapshot.AttemptId,Is.EqualTo(s.State.CurrentService.Snapshot.AttemptId));
            Assert.That(restored.PrinceChoice,Is.EqualTo("keep"));
            Assert.That(CampaignSession.Continue(c,store,restored).Brew,Is.Null);
        }
        [Test] public void CampaignSaveNeverAcceptsSliceNamespaceOrFutureVersion()
        {
            var c=CampaignFlowTests.Content(); var s=CampaignSession.StartAsync(c,new CampaignFlowTests.Store()).GetAwaiter().GetResult();
            var root=JObject.Parse(CampaignSaveCodec.Encode(s.State,c)); root["mode"]="slice";
            Assert.Throws<ContentValidationException>(()=>CampaignSaveCodec.Decode(root.ToString(),c));
            root["schemaVersion"]=999; Assert.Throws<UnsupportedSaveVersionException>(()=>CampaignSaveCodec.Decode(root.ToString(),c));
        }
        [Test] public void ChangedPayloadCannotCreateMemoryOrKnowledge()
        {
            var c=CampaignFlowTests.Content(); var s=CampaignSession.StartAsync(c,new CampaignFlowTests.Store()).GetAwaiter().GetResult();
            var root=JObject.Parse(CampaignSaveCodec.Encode(s.State,c)); Assert.That(root["payload"],Is.Not.Null); root["payload"]["princeChoice"]="seek";
            Assert.Throws<ContentValidationException>(()=>CampaignSaveCodec.Decode(root.ToString(),c));
        }
        [Test] public void TwoGenerationCampaignStoreRecoversAndDoesNotOverwriteFutureSchema()
        {
            string folder=Path.GetFullPath(Path.Combine(SliceContentTests.ContentRoot(),"../Validation/Evidence/campaign-save-tests",Guid.NewGuid().ToString("N")));
            {
                var c=CampaignFlowTests.Content(); var store=new CampaignSaveStore(folder,c); var s=Task.Run(()=>CampaignSession.StartAsync(c,store)).GetAwaiter().GetResult();
                Assert.That(s.SaveError,Is.Null,"Initial publication failed");
                CampaignFlowTests.Advance(s); Assert.That(s.SaveError,Is.Null,"Second publication failed");
                Assert.That(File.Exists(store.PreviousPath),Is.True,"Second publication did not retain previous generation");
                File.WriteAllText(store.CurrentPath,"broken");
                var recovery=store.Load(); Assert.That(recovery.RecoveryRequired,Is.True); Assert.That(recovery.Checkpoint.Phase,Is.EqualTo(CampaignPhase.Welcome));
                File.WriteAllText(store.CurrentPath,"{\"schemaVersion\":999}");
                Assert.Throws<UnsupportedSaveVersionException>(()=>store.Load());
                Assert.Throws<UnsupportedSaveVersionException>(()=>Task.Run(()=>store.SaveAsync(s.State)).GetAwaiter().GetResult());
                Assert.That(File.ReadAllText(store.CurrentPath),Is.EqualTo("{\"schemaVersion\":999}"));
            }
        }
        [Test] public void ShakeAndFloatEvidenceSurviveSerializationWithOriginalOutcome()
        {
            var shake=ShakeDomainTests.Session(); ShakeDomainTests.Fill(shake); ShakeDomainTests.Motion(shake); var shakeOutcome=ShakeDomainTests.Finish(shake);
            var saved=ServiceEvidenceCodec.Read(ServiceEvidenceCodec.Write(new CommittedService("run:0",shake.Freeze(),shakeOutcome,"good",true)),ShakeDomainTests.Catalog(),BartendingDomainTests.Tuning());
            Assert.That(saved,Is.Not.Null);
            Assert.That(saved.Snapshot.Shake.Reversals,Is.EqualTo(8)); Assert.That(saved.Snapshot.Shake.Amounts["gin"],Is.EqualTo(45m)); Assert.That(saved.Outcome.QualityGood,Is.True);
            var floating=FloatDomainTests.Session(); floating.Pour("vodka",ContainerRole.Serving,59.14706m); FloatDomainTests.Layer(floating,"coffee_liqueur",29.57353m,1m); FloatDomainTests.Layer(floating,"cream",29.57353m);
            var snap=floating.Freeze(); var outcome=ServiceEvaluator.Evaluate(FloatDomainTests.Catalog(),BartendingDomainTests.Tuning(),snap,new DrinkRequest("clarity"));
            saved=ServiceEvidenceCodec.Read(ServiceEvidenceCodec.Write(new CommittedService("run:0",snap,outcome,"weak",true)),FloatDomainTests.Catalog(),BartendingDomainTests.Tuning());
            Assert.That(saved.Snapshot.Layers.Count,Is.EqualTo(3)); Assert.That(saved.Snapshot.Layers[1].LowerBoundaryGood,Is.False); Assert.That(saved.Snapshot.Layers[2].LowerBoundaryGood,Is.True);
            Assert.That(saved.Outcome.QualityGood,Is.False); Assert.That(saved.Snapshot.TotalMl,Is.EqualTo(118.29412m));
        }
        [Test] public void WholeActPersistsEveryReadingAndMemoryBoundary()
        {
            var c=CampaignFlowTests.Content(); string folder=Path.GetFullPath(Path.Combine(SliceContentTests.ContentRoot(),"../Validation/Evidence/campaign-save-tests",Guid.NewGuid().ToString("N")));
            var store=new CampaignSaveStore(folder,c); var s=Task.Run(()=>CampaignSession.StartAsync(c,store)).GetAwaiter().GetResult();
            Task.Run(()=> {
                for(int i=0;i<3;i++)
                {
                    CampaignFlowTests.ReachBrew(s); CampaignFlowTests.Serve(s,i==1 ? null : "recipe_test_stir"); CampaignFlowTests.ReachDeparture(s);
                    s.TickAsync(2).GetAwaiter().GetResult(); Assert.That(s.SaveError,Is.Null);
                }
            }).GetAwaiter().GetResult();
            var saved=store.Load().Checkpoint; Assert.That(saved.ActComplete,Is.True); Assert.That(saved.Memories.Single().Variant,Is.EqualTo("star_unlit"));
            Assert.That(saved.Services.Count,Is.EqualTo(3)); Assert.That(saved.PrinceChoice,Is.EqualTo("keep"));
        }
        [Test] public void CompletedDepartureCannotClaimUnreadFarewellEvenWithValidChecksum()
        {
            var c=CampaignFlowTests.Content(); var s=CampaignSession.StartAsync(c,new CampaignFlowTests.Store()).GetAwaiter().GetResult();
            CampaignFlowTests.ReachBrew(s); CampaignFlowTests.Serve(s); CampaignFlowTests.ReachDeparture(s); s.TickAsync(2).GetAwaiter().GetResult();
            var root=JObject.Parse(CampaignSaveCodec.Encode(s.State,c)); var payload=(JObject)root["payload"];
            payload["consumedLines"]=new JArray(((JArray)payload["consumedLines"]).Values<string>().Where(x=>!x.Contains(":Farewell:")));
            using(var hash=System.Security.Cryptography.SHA256.Create()) root["payloadSha256"]=BitConverter.ToString(hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload.ToString(Newtonsoft.Json.Formatting.None)))).Replace("-","").ToLowerInvariant();
            Assert.Throws<ContentValidationException>(()=>CampaignSaveCodec.Decode(root.ToString(),c));
        }
        [TestCase("before_write")][TestCase("after_write")][TestCase("after_flush")][TestCase("after_validate")][TestCase("after_replace")][TestCase("after_final_read")]
        public void EveryCampaignPublicationInterruptionRetainsAValidGeneration(string boundary)
        {
            var c=CampaignFlowTests.Content(); string folder=Path.GetFullPath(Path.Combine(SliceContentTests.ContentRoot(),"../Validation/Evidence/campaign-save-tests",Guid.NewGuid().ToString("N")));
            var store=new CampaignSaveStore(folder,c); var s=Task.Run(()=>CampaignSession.StartAsync(c,store)).GetAwaiter().GetResult(); CampaignFlowTests.Advance(s);
            store.Fault=point=> { if(point==boundary) throw new IOException("injected "+point); };
            CampaignFlowTests.Advance(s); Assert.That(s.SaveError,Is.Not.Null); Assert.That(store.Load().Checkpoint,Is.Not.Null);
            store.Fault=null; Assert.That(Task.Run(()=>s.RetrySaveAsync()).GetAwaiter().GetResult(),Is.True);
            Assert.That(store.Load().Checkpoint.Sequence,Is.EqualTo(s.State.Sequence));
        }
    }
}
