using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TextCore.LowLevel;
using Surlhwa.ContentIO;
using Surlhwa.Presentation;
using Object=UnityEngine.Object;

namespace Surlhwa.Editor
{
    // Additive ACT 2 candidate. Neither the approved scene nor the default ACT 1
    // build selection is changed; their art, camera and complete original cast stay intact.
    public static class CampaignExpansionBuilder
    {
        public const string ScenePath="Assets/Surlhwa/Scenes/Bar_Q1_Act2.unity";
        public const string BundlePath="Assets/GeneratedContent/act2-content.json";
        public const string Root="Assets/Surlhwa/Act2Assets";

        public static void Sync()
        {
            Directory.CreateDirectory("Assets/GeneratedContent");
            CopyContent("../Content/Campaign/act1.json",CampaignBuilder.BundlePath);
            CopyContent("../Content/Campaign/act2.json",BundlePath);
        }
        private static void CopyContent(string source,string destination)
        {
            CampaignContentLoader.Load(File.ReadAllText(source)); var bytes=File.ReadAllBytes(source);
            if(!File.Exists(destination) || !File.ReadAllBytes(destination).SequenceEqual(bytes)) File.WriteAllBytes(destination,bytes);
            AssetDatabase.ImportAsset(destination,ImportAssetOptions.ForceSynchronousImport);
        }
        private static void EnsureNoUnsavedScene()
        {
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save or close modified scenes before preparing ACT 2. No owner scene was replaced.");
        }
        [MenuItem("Surlhwa/Prepare separate Q1 ACT 2 candidate")]
        public static void PrepareScene()
        {
            EnsureNoUnsavedScene();
            var approved=File.ReadAllBytes(Q1ProductionBuilder.ScenePath);
            Sync(); Directory.CreateDirectory(Root); AssetDatabase.Refresh();
            if(!File.Exists(ScenePath) && !AssetDatabase.CopyAsset(Q1ProductionBuilder.ScenePath,ScenePath))
                throw new InvalidOperationException("Could not copy approved Q1 to the separate ACT 2 candidate.");
            var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            var controller=Object.FindFirstObjectByType<CampaignController>();
            var view=Object.FindFirstObjectByType<Q1ProductionView>();
            if(controller==null || view==null || controller.Ordinary==null)
                throw new InvalidOperationException("ACT 2 must extend the approved Q1 composition and its existing campaign controller.");
            controller.Bundle=AssetDatabase.LoadAssetAtPath<TextAsset>(BundlePath);
            controller.LegacyBundle=AssetDatabase.LoadAssetAtPath<TextAsset>(CampaignBuilder.BundlePath);
            controller.Font=PrepareFont();
            var cast=controller.GetComponent<CampaignCastView>() ?? controller.gameObject.AddComponent<CampaignCastView>();
            Vector3 primary=controller.Ordinary.transform.position;
            if(controller.Chunhyang==null) controller.Chunhyang=CreateActor(controller,"Chunhyang","guest_chunhyang",primary);
            if(controller.Mongryong==null) controller.Mongryong=CreateActor(controller,"Mongryong","guest_mongryong",primary+Vector3.right*1.7f);
            BindActor(controller.Chunhyang,"Chunhyang"); BindActor(controller.Mongryong,"Mongryong");
            cast.Initialize(controller,primary); cast.ApplyView(false);
            view.ApplyView(false);
            EditorUtility.SetDirty(controller); EditorUtility.SetDirty(cast);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene,ScenePath);
            if(!approved.SequenceEqual(File.ReadAllBytes(Q1ProductionBuilder.ScenePath)))
                throw new InvalidOperationException("The approved Q1 scene changed during candidate preparation.");
            Debug.Log("ACT2_SCENE_PREPARED: separate Q1 candidate, exact ACT 1 legacy bundle and two grounded cast anchors.");
        }
        [MenuItem("Surlhwa/Build separate Q1 ACT 2 Player")]
        public static void Build()
        {
            PrepareScene();
            string output=Path.GetFullPath("../Validation/Builds/Act2Production/SurlhwaAct2.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(output)); Directory.CreateDirectory("../Validation/Manifests");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=output,
                target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            File.WriteAllText("../Validation/Manifests/act2-production-build.json",JsonUtility.ToJson(new BuildRecord{
                result=report.summary.result.ToString(),errors=report.summary.totalErrors,warnings=report.summary.totalWarnings,
                bytes=report.summary.totalSize,seconds=report.summary.totalTime.TotalSeconds,scene=ScenePath,output=output,
                unity=UnityEngine.Application.unityVersion,scope="through_act_02"},true));
            File.Copy("Assets/Surlhwa/SliceAssets/Noto-OFL.txt",Path.Combine(Path.GetDirectoryName(output),"Noto-OFL.txt"),true);
            if(report.summary.result!=BuildResult.Succeeded) throw new InvalidOperationException("ACT 2 candidate Player build failed.");
            Debug.Log("ACT2_PRODUCTION_BUILD_SUCCEEDED: "+output);
        }
        private static TMP_FontAsset PrepareFont()
        {
            string path=Root+"/Act2Korean.asset"; var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if(font==null) {
                font=TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>("Assets/Surlhwa/SliceAssets/NotoSansKR-Regular.ttf"),
                    64,8,GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic,true);
                font.name="ACT 2 Korean"; font.fallbackFontAssetTable=new List<TMP_FontAsset>(); AssetDatabase.CreateAsset(font,path);
                AssetDatabase.AddObjectToAsset(font.material,font); foreach(var texture in font.atlasTextures) AssetDatabase.AddObjectToAsset(texture,font);
            }
            string corpus=File.ReadAllText(BundlePath)+string.Join("",Directory.GetFiles("Assets/Surlhwa/Presentation","Campaign*.cs").Select(File.ReadAllText));
            if(!font.TryAddCharacters(new string(corpus.Where(x=>!char.IsControl(x)).Distinct().ToArray()),out string missing))
                throw new InvalidOperationException("Missing ACT 2 glyphs: "+missing);
            EditorUtility.SetDirty(font); AssetDatabase.SaveAssetIfDirty(font); return font;
        }
        private static CharacterView CreateActor(CampaignController owner,string name,string id,Vector3 position)
        {
            var actor=new GameObject("ACT 2 "+name).AddComponent<CharacterView>();
            actor.transform.SetParent(owner.Ordinary.transform.parent,false); actor.transform.position=position;
            actor.GuestId=id; actor.VisualBindingId="visual_"+name.ToLowerInvariant();
            actor.FeetAnchor=Anchor(actor.transform,"FeetAnchor",Vector3.zero);
            actor.SeatAnchor=Anchor(actor.transform,"SeatAnchor",owner.Ordinary.SeatAnchor.localPosition);
            var plane=new GameObject(name+"_Plane"); plane.transform.SetParent(actor.transform,false);
            plane.transform.localRotation=Q1ProductionView.FocusAngle;
            actor.Plane=plane.AddComponent<MeshFilter>(); actor.Renderer=plane.AddComponent<MeshRenderer>();
            actor.Renderer.shadowCastingMode=ShadowCastingMode.Off; actor.Renderer.receiveShadows=false;
            var contact=new GameObject(name+"_Contact"); contact.transform.SetParent(actor.transform,false);
            contact.transform.localPosition=owner.Ordinary.Contact.transform.localPosition;
            contact.transform.localScale=owner.Ordinary.Contact.transform.localScale;
            contact.AddComponent<MeshFilter>().sharedMesh=owner.Ordinary.Contact.GetComponent<MeshFilter>().sharedMesh;
            actor.Contact=contact.AddComponent<MeshRenderer>(); actor.Contact.sharedMaterials=owner.Ordinary.Contact.sharedMaterials;
            actor.Contact.shadowCastingMode=ShadowCastingMode.Off; actor.Contact.receiveShadows=false; return actor;
        }
        private static Transform Anchor(Transform parent,string name,Vector3 position)
        {
            var t=new GameObject(name).transform; t.SetParent(parent,false); t.localPosition=position; return t;
        }
        private static void BindActor(CharacterView actor,string name)
        {
            const float ppu=70;
            var standing=Texture(name+"Standing");
            actor.StandingMesh=Plane(name+"Standing",new Vector2(standing.width/2f,name=="Mongryong" ? 1 : 0),ppu);
            actor.StandingMaterial=Sprite(name+"Standing");
            // Seat contact is the source pelvis, not the center of its transparent canvas.
            // Source pelvis pixels: Chunhyang (44,29 bottom-up), Mongryong (47,29).
            // The earlier y45 waist binding buried the opaque hem in the floor.
            actor.SeatedMesh=Plane(name+"Seated",new Vector2(name=="Chunhyang" ? 32 : 35,3),ppu);
            actor.SeatedMaterial=Sprite(name+"Seated");
            actor.WalkMeshes=Enumerable.Range(0,4).Select(i=>Plane(name+"Walk"+i,
                new Vector2(48,name=="Mongryong" ? (i==0 ? 2 : 1) : 0),ppu)).ToArray();
            actor.WalkMaterials=Enumerable.Range(0,4).Select(i=>Sprite(name+"Walk"+i)).ToArray();
            actor.Show(false,true); EditorUtility.SetDirty(actor);
        }
        private static Texture2D Texture(string name)
        {
            string path=Root+"/"+name+".png"; var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null) throw new InvalidOperationException("Missing source-tracked ACT 2 art: "+path);
            if(importer.filterMode!=FilterMode.Point || importer.mipmapEnabled || importer.textureCompression!=TextureImporterCompression.Uncompressed || importer.npotScale!=TextureImporterNPOTScale.None) {
                importer.filterMode=FilterMode.Point; importer.mipmapEnabled=false; importer.npotScale=TextureImporterNPOTScale.None;
                importer.textureCompression=TextureImporterCompression.Uncompressed; importer.alphaIsTransparency=true; importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        private static Material Sprite(string name)
        {
            var material=new Material(Shader.Find("Universal Render Pipeline/Unlit")); material.SetColor("_BaseColor",Color.white);
            material.SetTexture("_BaseMap",Texture(name)); material.SetFloat("_AlphaClip",1); material.SetFloat("_Cutoff",.4f);
            material.SetFloat("_Cull",0); material.SetFloat("_ZWrite",1); material.EnableKeyword("_ALPHATEST_ON"); material.renderQueue=2450;
            return Save(material,name+".mat");
        }
        private static Mesh Plane(string name,Vector2 pivot,float ppu)
        {
            var texture=Texture(name); float l=-pivot.x/ppu,r=(texture.width-pivot.x)/ppu,b=-pivot.y/ppu,t=(texture.height-pivot.y)/ppu;
            var mesh=new Mesh{vertices=new[]{new Vector3(l,b,0),new Vector3(l,t,0),new Vector3(r,t,0),new Vector3(r,b,0)},
                uv=new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right},triangles=new[]{0,1,2,0,2,3}};
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return Save(mesh,name+"Mesh.asset");
        }
        private static T Save<T>(T value,string name) where T:Object
        {
            string path=Root+"/"+name; value.name=Path.GetFileNameWithoutExtension(name); var existing=AssetDatabase.LoadAssetAtPath<T>(path);
            if(existing==null) { AssetDatabase.CreateAsset(value,path); return value; }
            EditorUtility.CopySerialized(value,existing); Object.DestroyImmediate(value); AssetDatabase.SaveAssetIfDirty(existing); return existing;
        }
        [Serializable] private sealed class BuildRecord { public string result,scene,output,unity,scope; public int errors,warnings; public ulong bytes; public double seconds; }
    }
}
