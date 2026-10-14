using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Surlhwa.ContentIO;
using Surlhwa.Domain;
using Surlhwa.Presentation;
namespace Surlhwa.SpatialTests
{
    public sealed class CampaignPresentationTests
    {
        [Test] public void AllAuthoredRequestSummariesFitTheProductionReadingBox()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Surlhwa/Scenes/Bar_Act1.unity");
            var c=UnityEngine.Object.FindFirstObjectByType<CampaignController>();
            var content=CampaignContentLoader.Load(c.Bundle.text);
            typeof(CampaignController).GetProperty("Content").SetValue(c,content);
            typeof(CampaignController).GetProperty("Input").SetValue(c,c.gameObject.AddComponent<SliceInputAdapter>());
            var ui=c.gameObject.AddComponent<CampaignUi>();
            var events=new GameObject("Reading fixture events",typeof(EventSystem));
            var on=typeof(EventSystem).GetMethod("OnEnable",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            var off=typeof(EventSystem).GetMethod("OnDisable",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            try {
                on.Invoke(events.GetComponent<EventSystem>(),null); ui.Initialize(c,c.Font);
                var text=(TMPro.TextMeshProUGUI)typeof(CampaignUi).GetField("request",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(ui);
                text.transform.parent.gameObject.SetActive(true);
                foreach(var line in content.Texts.Values.Where(x=>x.Role=="request_summary")) {
                    text.text=line.Value; text.ForceMeshUpdate(); Assert.That(text.isTextOverflowing,Is.False,line.Value);
                }
            } finally { off.Invoke(events.GetComponent<EventSystem>(),null); UnityEngine.Object.DestroyImmediate(events); UnityEngine.Object.DestroyImmediate(c.gameObject); }
        }
        [Test] public void ObserverRetriesLockedPublicationWithoutBreakingGameplay()
        {
            var publish=typeof(CampaignEvidenceProbe).GetMethod("TryPublish",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
            Assert.That(publish,Is.Not.Null);
            string folder=Path.GetFullPath("../Validation/Evidence/probe-lock-test-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
            string path=Path.Combine(folder,"observation.json"); File.WriteAllText(path,"old");
            try {
                using(var locked=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read))
                    Assert.That(publish.Invoke(null,new object[]{path,"new"}),Is.False);
                Assert.That(File.ReadAllText(path),Is.EqualTo("old"));
                Assert.That(publish.Invoke(null,new object[]{path,"new"}),Is.True);
                Assert.That(File.ReadAllText(path),Is.EqualTo("new"));
            } finally { Directory.Delete(folder,true); }
        }
        [Test] public void DisabledButtonCannotKeepTheVisibleFocusMarker()
        {
            var events=new GameObject("Focus test events",typeof(EventSystem)); var go=new GameObject("Focus test",typeof(RectTransform),typeof(Image),typeof(Button));
            try {
                // EditMode does not dispatch EventSystem's runtime OnEnable registration.
                typeof(EventSystem).GetMethod("OnEnable",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(events.GetComponent<EventSystem>(),null);
                var b=go.GetComponent<Button>(); var outline=go.AddComponent<SliceFocusOutline>(); outline.Initialize(b);
                var refresh=typeof(SliceFocusOutline).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                EventSystem.current.SetSelectedGameObject(go); refresh.Invoke(outline,null); Assert.That(go.transform.Find("Focus outline").gameObject.activeSelf,Is.True);
                b.enabled=false; refresh.Invoke(outline,null); Assert.That(go.transform.Find("Focus outline").gameObject.activeSelf,Is.False);
            } finally { typeof(EventSystem).GetMethod("OnDisable",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(events.GetComponent<EventSystem>(),null); UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(events); }
        }
        [Test] public void ActualFloatLayersAndShakerVolumeAreReadOnlyPresentation()
        {
            var c=CampaignContentLoader.Load(File.ReadAllText("../Content/Campaign/act1.json")); var b=new BrewingSession(c.Catalog,c.Tuning,c.Supply); var go=new GameObject("Four method workbench");
            try {
                foreach(var id in c.Catalog.Recipes["recipe_manwol"].LayerOrder) { b.Pour(id,ContainerRole.Jigger,c.Catalog.Recipes["recipe_manwol"].Amounts[id]); b.BeginFloat(); b.UpdateFloat(30000000,.2m,.1); b.EndFlow(); }
                b.Pour("gin",ContainerRole.Shaker,15m); var before=b.Freeze(); int revision=b.Revision;
                var view=go.AddComponent<SliceWorkbenchView>(); view.ExtendedTools=true; view.Initialize(null); view.Refresh(b,null,0); view.CocktailGlass=true; view.Refresh(b,null,0);
                Assert.That(view.VisibleLayerCount,Is.EqualTo(3)); Assert.That(view.ShakerFillMl,Is.EqualTo(15m)); Assert.That(view.ServingFillMl,Is.EqualTo(before.TotalMl)); Assert.That(b.Revision,Is.EqualTo(revision));
                Assert.That(b.Freeze().Amounts,Is.EqualTo(before.Amounts));
            } finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        [Test] public void CampaignSceneRetainsC1CameraCounterAndPhysicalContacts()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Surlhwa/Scenes/Bar_Act1.unity"); Physics.SyncTransforms();
            var c=UnityEngine.Object.FindFirstObjectByType<CampaignController>();
            Assert.That(c,Is.Not.Null); Assert.That(UnityEngine.Object.FindFirstObjectByType<SliceController>(),Is.Null);
            Assert.That(c.Camera.orthographic,Is.True); Assert.That(c.Camera.orthographicSize,Is.EqualTo(3.7f).Within(.001f)); Assert.That(c.Camera.transform.eulerAngles.x,Is.EqualTo(28).Within(.001f));
            var seat=GameObject.Find("C_Stool").GetComponent<Collider>().bounds;
            Assert.That(Vector3.Distance(c.Prince.SeatAnchor.position,seat.ClosestPoint(c.Prince.SeatAnchor.position)),Is.LessThan(.003f));
            var front=GameObject.Find("C_CounterTop").GetComponent<Renderer>().bounds;
            foreach(string prop in new[]{"Folded bar cloth","Small resting tray"})
                Assert.That(GameObject.Find(prop).GetComponent<Renderer>().bounds.min.y,Is.EqualTo(front.max.y).Within(.003f),prop+" must rest on the counter");
            Assert.That(c.Bartender.transform.position.z,Is.GreaterThan(front.max.z+.2f)); Assert.That(c.Prince.SeatAnchor.position.z,Is.LessThan(front.min.z-.25f));
            Assert.That(c.Bundle.text,Is.EqualTo(File.ReadAllText("../Content/Campaign/act1.json"))); Assert.That(UnityEditor.AssetDatabase.GetAssetPath(c.Font),Is.EqualTo("Assets/Surlhwa/Act1Assets/Act1Korean.asset"));
            Assert.That(c.Foot,Is.Not.Null); Assert.That(c.Music,Is.Not.Null);
            var snow=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath("Assets/Surlhwa/Act1Assets/WinterGround.png");
            Assert.That(snow.filterMode,Is.EqualTo(FilterMode.Point)); Assert.That(snow.npotScale,Is.EqualTo(UnityEditor.TextureImporterNPOTScale.None),"Source pixels must not be resampled during import.");
            Assert.That(c.Prince.StandingMesh.bounds.size.y/c.Bartender.StandingMesh.bounds.size.y,Is.InRange(.72f,.84f));
        }
    }
}
