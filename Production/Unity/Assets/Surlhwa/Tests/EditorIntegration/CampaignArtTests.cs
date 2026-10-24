using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using Surlhwa.ContentIO;
using Surlhwa.Domain;
using Surlhwa.Presentation;

namespace Surlhwa.SpatialTests
{
    public sealed class CampaignArtTests
    {
        [Test] public void AuthoredShellsPreserveActualFloatLayersAndDoNotFillEmptyVessels()
        {
            var content=CampaignContentLoader.Load(File.ReadAllText("../Content/Campaign/act1.json"));
            var brew=new BrewingSession(content.Catalog,content.Tuning,content.Supply);
            var go=new GameObject("Authored shell fixture");
            try {
                var view=go.AddComponent<SliceWorkbenchView>(); view.ExtendedTools=true; view.Initialize(null);
                var art=AssetDatabase.LoadAssetAtPath<CampaignArtSet>("Assets/Surlhwa/Act1Assets/ProductionArt/Act1Art.asset");
                Assert.That(art,Is.Not.Null,"The production scene must bind an actual art set.");
                view.ApplyArt(art); view.Refresh(brew,null,0);
                Assert.That(view.ServingFillMl,Is.Zero); Assert.That(view.VisibleLayerCount,Is.Zero);
                Assert.That(go.GetComponentsInChildren<Transform>(true).Single(x=>x.name=="Actual bowl liquid").gameObject.activeSelf,Is.False);
                brew.Pour("vodka",ContainerRole.Jigger,30m); view.Refresh(brew,null,0);
                var jigger=go.GetComponentsInChildren<MeshFilter>().Single(x=>x.name=="Actual jigger liquid");
                var shell=go.GetComponentsInChildren<MeshFilter>().Single(x=>x.name=="Authored jigger");
                Assert.That(jigger.sharedMesh.bounds.max.z,Is.LessThan(shell.sharedMesh.bounds.min.z),"Opaque metal must not conceal the measured upper-cup fill.");
                brew.BeginFloat(); brew.UpdateFloat(30000000,.2m,.1); brew.EndFlow();
                // New composition for the full three-layer assertion below.
                brew=new BrewingSession(content.Catalog,content.Tuning,content.Supply);
                foreach(var id in content.Catalog.Recipes["recipe_manwol"].LayerOrder) {
                    brew.Pour(id,ContainerRole.Jigger,content.Catalog.Recipes["recipe_manwol"].Amounts[id]);
                    brew.BeginFloat(); brew.UpdateFloat(30000000,.2m,.1); brew.EndFlow();
                }
                var before=brew.Freeze(); int revision=brew.Revision;
                view.Refresh(brew,null,0); view.CocktailGlass=true; view.Refresh(brew,null,0);
                Assert.That(view.VisibleLayerCount,Is.EqualTo(3)); Assert.That(view.ServingFillMl,Is.EqualTo(before.TotalMl));
                Assert.That(brew.Revision,Is.EqualTo(revision)); Assert.That(brew.Freeze().Amounts,Is.EqualTo(before.Amounts));
                foreach(var fill in go.GetComponentsInChildren<MeshFilter>().Where(x=>x.name.StartsWith("Actual Float layer")))
                {
                    // Source CocktailGlass bowl apex is at .267 world units, independently
                    // measured from the preserved 80x88 shell at source pixel (39,46).
                    Assert.That(fill.sharedMesh.bounds.min.y,Is.GreaterThan(.267f),"Liquid must stay above the authored stem.");
                    Assert.That(fill.sharedMesh.bounds.max.y,Is.LessThan(.54f),"Liquid must remain below the rim.");
                }
                brew.AddIce(ContainerRole.Serving); view.Refresh(brew,null,0);
                var ice=go.transform.Find("Serving glass 180 ml/Actual ice/Authored actual ice").GetComponent<MeshFilter>();
                Assert.That(ice.gameObject.activeInHierarchy,Is.True);
                Assert.That(ice.sharedMesh.bounds.min.y,Is.GreaterThan(.267f),"Authored ice cannot sit in the cocktail stem.");
                Assert.That(ice.sharedMesh.bounds.max.y,Is.LessThan(.54f));
            } finally { Object.DestroyImmediate(go); }
        }

        [Test] public void ProductionArtUpgradeCannotDiscardAnUnsavedEditorScene()
        {
            var scene=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);
            var sentinel=new GameObject("Unsaved owner scene content");
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            try {
                Assert.That(()=>Surlhwa.Editor.CampaignProductionArtBuilder.UpgradeScene(),Throws.InvalidOperationException);
                Assert.That(sentinel!=null && sentinel.scene==UnityEngine.SceneManagement.SceneManager.GetActiveScene(),Is.True);
            } finally { Object.DestroyImmediate(sentinel); UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single); }
        }

        [Test] public void ProductionCharacterFramesRetainGroundAndSeatContacts()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Surlhwa/Scenes/Bar_Act1.unity");
            var c=Object.FindFirstObjectByType<CampaignController>(); Assert.That(c.Art,Is.Not.Null);
            Assert.That(c.Art.OrdinaryCoat.Length,Is.EqualTo(4)); Assert.That(c.Art.OrdinaryBun.Length,Is.EqualTo(4));
            foreach(var actor in new[]{c.Bartender,c.Ordinary,c.Prince}) {
                actor.Show(true,false);
                Assert.That(actor.StandingMesh.bounds.min.y,Is.EqualTo(0).Within(.001f),actor.name+" standing baseline");
                Assert.That(actor.Renderer.sharedMaterial.mainTexture,Is.Not.Null);
            }
            Physics.SyncTransforms(); var seat=GameObject.Find("C_Stool").GetComponent<Collider>().bounds;
            Assert.That(Vector3.Distance(c.Prince.SeatAnchor.position,seat.ClosestPoint(c.Prince.SeatAnchor.position)),Is.LessThan(.003f));
            Assert.That(c.Camera.orthographicSize,Is.EqualTo(3.7f).Within(.001f));
        }
    }
}
