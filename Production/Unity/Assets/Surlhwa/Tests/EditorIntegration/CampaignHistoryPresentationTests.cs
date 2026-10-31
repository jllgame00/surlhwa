using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Surlhwa.ContentIO;
using Surlhwa.Presentation;
using Object=UnityEngine.Object;

namespace Surlhwa.SpatialTests
{
    public sealed class CampaignHistoryPresentationTests
    {
        private GameObject go; private CampaignController controller; private string root;
        private TextAsset one,two,three;
        private const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        [SetUp] public void Setup()
        {
            root=Path.GetFullPath("../Validation/Evidence/act3-history-selection-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root); go=new GameObject("Isolated history-selection fixture");
            controller=go.AddComponent<CampaignController>();
            one=new TextAsset(File.ReadAllText("../Content/Campaign/act1.json"));
            two=new TextAsset(File.ReadAllText("../Content/Campaign/act2.json"));
            three=new TextAsset(File.ReadAllText("../Content/Campaign/act3.json"));
            controller.Bundle=three; controller.LegacyBundle=one; controller.PriorBundles=new[]{two};
        }
        [TearDown] public void Teardown()
        {
            Object.DestroyImmediate(go); Object.DestroyImmediate(one); Object.DestroyImmediate(two); Object.DestroyImmediate(three);
            Directory.Delete(root,true);
        }
        private string History(string scope) => Path.Combine(root,"history",scope);
        private void Configure()
        {
            var method=typeof(CampaignController).GetMethod("ConfigureStorage",Hidden);
            Assert.That(method,Is.Not.Null,"Storage selection must isolate every accepted source and the current namespace.");
            typeof(CampaignController).GetProperty("Content").SetValue(controller,CampaignContentLoader.Load(three.text));
            method.Invoke(controller,new object[]{new[]{"-campaignHistoryRoot",Path.Combine(root,"history"),"-campaignSaveRoot",Path.Combine(root,"current")}});
        }
        private string SelectedRoot => (string)typeof(CampaignController).GetField("legacyRoot",Hidden).GetValue(controller);
        private void HistoryFile(string scope,string name,string value)
        {
            string path=Path.Combine(History(scope),"slot1"); Directory.CreateDirectory(path); File.WriteAllText(Path.Combine(path,name),value);
        }
        [TestCase("current.json")][TestCase("previous.json")]
        public void LatestAcceptedHistoryWinsEvenWhenItsBytesAreInvalid(string generation)
        {
            HistoryFile("campaign-act1","current.json","older retained evidence");
            HistoryFile("campaign-through-act-02",generation,"invalid or future bytes are not permission to roll back");
            Configure();
            Assert.That(SelectedRoot,Is.EqualTo(History("campaign-through-act-02")));
            Assert.That(controller.StorageRoot,Is.EqualTo(Path.Combine(root,"current")));
            Assert.That(File.ReadAllText(Path.Combine(SelectedRoot,"slot1",generation)),Is.EqualTo("invalid or future bytes are not permission to roll back"));
            Assert.That(Directory.Exists(controller.StorageRoot),Is.False,"Selection is read-only.");
        }
        [Test] public void EmptyIsolatedHistoryCannotConsultOwnerDefaultNamespaces()
        {
            Configure(); Assert.That(SelectedRoot,Is.EqualTo(History("campaign-act1")));
            Assert.That(Directory.GetFiles(root,"*",SearchOption.AllDirectories),Is.Empty);
        }
        [Test] public void SettingsFollowLatestAcceptedPreferencesIndependentlyOfGameHistory()
        {
            HistoryFile("campaign-act1","current.json","existing older history");
            Directory.CreateDirectory(History("campaign-through-act-02"));
            string settings="{\"schemaVersion\":1,\"master\":0.27,\"largeText\":true,\"highContrast\":true}";
            string path=Path.Combine(History("campaign-through-act-02"),"settings.json"); File.WriteAllText(path,settings);
            Configure(); typeof(CampaignController).GetMethod("LoadSettings",Hidden).Invoke(controller,null);
            Assert.That(SelectedRoot,Is.EqualTo(History("campaign-act1")));
            Assert.That(controller.MasterVolume,Is.EqualTo(.27f)); Assert.That(controller.LargeText && controller.HighContrast,Is.True);
            Assert.That(File.ReadAllText(path),Is.EqualTo(settings)); Assert.That(Directory.Exists(controller.StorageRoot),Is.False);
        }
        [Test] public void ExplicitQuixoteSelectionNeverBorrowsPrinceOrOrdinaryActor()
        {
            var method=typeof(CampaignController).GetMethod("SelectGuestActor",Hidden);
            Assert.That(method,Is.Not.Null,"Special guest identity must select its own renderer.");
            typeof(CampaignController).GetProperty("Content").SetValue(controller,CampaignContentLoader.Load(three.text));
            controller.Prince=go.AddComponent<CharacterView>(); controller.Quixote=go.AddComponent<CharacterView>(); controller.Ordinary=go.AddComponent<CharacterView>();
            Assert.That(method.Invoke(controller,new object[]{"guest_quixote"}),Is.SameAs(controller.Quixote));
            Assert.That(method.Invoke(controller,new object[]{"guest_little_prince"}),Is.SameAs(controller.Prince));
            Assert.That(method.Invoke(controller,new object[]{"guest_postman"}),Is.SameAs(controller.Ordinary));
        }
    }
}
