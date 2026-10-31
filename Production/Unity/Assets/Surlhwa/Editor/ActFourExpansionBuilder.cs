using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Surlhwa.Presentation;
using Object=UnityEngine.Object;

namespace Surlhwa.Editor
{
    public static class ActFourExpansionBuilder
    {
        public const string ScenePath="Assets/Surlhwa/Scenes/Bar_Q1_Act4.unity";
        public const string BundlePath="Assets/GeneratedContent/act4-content.json";
        public const string Root="Assets/Surlhwa/Act4Assets";
        public static void Sync()
        {
            ActThreeExpansionBuilder.Sync();
            CampaignExpansionBuilder.CopyContent("../Content/Campaign/act4.json",BundlePath);
        }
        [MenuItem("Surlhwa/Prepare separate Q1 ACT 4 candidate")]
        public static void PrepareScene()
        {
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save or close modified scenes before preparing ACT 4. No owner scene was replaced.");
            var preserved=new[]{Q1ProductionBuilder.ScenePath,CampaignExpansionBuilder.ScenePath,ActThreeExpansionBuilder.ScenePath}.ToDictionary(x=>x,File.ReadAllBytes);
            Sync(); Directory.CreateDirectory(Root); AssetDatabase.Refresh();
            if(!File.Exists(ScenePath) && !AssetDatabase.CopyAsset(ActThreeExpansionBuilder.ScenePath,ScenePath))
                throw new InvalidOperationException("Could not copy accepted ACT 3 to the separate ACT 4 candidate.");
            var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            var c=Object.FindFirstObjectByType<CampaignController>(); var view=Object.FindFirstObjectByType<Q1ProductionView>();
            if(c==null || view==null || c.Ordinary==null || c.Chunhyang==null || c.Mongryong==null || c.Quixote==null)
                throw new InvalidOperationException("ACT 4 must extend the accepted ACT 3 Q1 composition and cast.");
            c.Bundle=AssetDatabase.LoadAssetAtPath<TextAsset>(BundlePath);
            c.PriorBundles=new[]{AssetDatabase.LoadAssetAtPath<TextAsset>(ActThreeExpansionBuilder.BundlePath),AssetDatabase.LoadAssetAtPath<TextAsset>(CampaignExpansionBuilder.BundlePath)};
            c.LegacyBundle=AssetDatabase.LoadAssetAtPath<TextAsset>(CampaignBuilder.BundlePath); c.Font=PrepareFont();
            if(c.Hamlet==null) c.Hamlet=CampaignExpansionBuilder.CreateActor(c,"Hamlet","guest_hamlet",c.Ordinary.transform.position);
            c.Hamlet.name="ACT 4 Hamlet"; BindHamlet(c.Hamlet); view.ApplyView(false);
            EditorUtility.SetDirty(c); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene,ScenePath);
            foreach(var item in preserved) if(!item.Value.SequenceEqual(File.ReadAllBytes(item.Key)))
                throw new InvalidOperationException("An accepted scene changed while preparing ACT 4: "+item.Key);
            Debug.Log("ACT4_SCENE_PREPARED: separate candidate, preserved ACT 1/2/3 sources and distinct Hamlet.");
        }
        [MenuItem("Surlhwa/Build separate Q1 ACT 4 Player")]
        public static void Build()
        {
            PrepareScene(); string output=Path.GetFullPath("../Validation/Builds/Act4Production/SurlhwaAct4.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(output)); Directory.CreateDirectory("../Validation/Manifests");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=output,
                target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            File.WriteAllText("../Validation/Manifests/act4-production-build.json",JsonUtility.ToJson(new BuildRecord{
                result=report.summary.result.ToString(),errors=report.summary.totalErrors,warnings=report.summary.totalWarnings,
                messages=report.steps.SelectMany(step=>step.messages).Where(message=>message.type==LogType.Warning || message.type==LogType.Error || message.type==LogType.Exception).Select(message=>message.type+": "+message.content).ToArray(),
                bytes=report.summary.totalSize,seconds=report.summary.totalTime.TotalSeconds,scene=ScenePath,output=output,
                unity=UnityEngine.Application.unityVersion,scope="through_act_04"},true));
            File.Copy("Assets/Surlhwa/SliceAssets/Noto-OFL.txt",Path.Combine(Path.GetDirectoryName(output),"Noto-OFL.txt"),true);
            if(report.summary.result!=BuildResult.Succeeded) throw new InvalidOperationException("ACT 4 candidate Player build failed.");
            Debug.Log("ACT4_PRODUCTION_BUILD_SUCCEEDED: "+output);
        }
        private static TMP_FontAsset PrepareFont()
        {
            string path=Root+"/Act4Korean.asset";
            if(!File.Exists(path) && !AssetDatabase.CopyAsset(ActThreeExpansionBuilder.Root+"/Act3Korean.asset",path))
                throw new InvalidOperationException("The accepted ACT 3 font must be available for an independent ACT 4 copy.");
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path); font.name=Path.GetFileNameWithoutExtension(path);
            if(AssetDatabase.GetAssetPath(font.material)!=path || font.atlasTextures.Any(x=>AssetDatabase.GetAssetPath(x)!=path))
                throw new InvalidOperationException("ACT 4 font must own its atlas and material before adding glyphs.");
            string corpus=File.ReadAllText(BundlePath)+string.Join("",Directory.GetFiles("Assets/Surlhwa/Presentation","Campaign*.cs").Select(File.ReadAllText));
            if(!font.TryAddCharacters(new string(corpus.Where(x=>!char.IsControl(x)).Distinct().ToArray()),out string missing))
                throw new InvalidOperationException("Missing ACT 4 glyphs: "+missing);
            EditorUtility.SetDirty(font); AssetDatabase.SaveAssetIfDirty(font); return font;
        }
        private static void BindHamlet(CharacterView actor)
        {
            // Source sole center and bent-leg seam were inspected for this actor. These are
            // independent of the Prince/lovers' poses; the shared physical seat stays fixed.
            actor.StandingMesh=Plane("HamletStanding",new Vector2(25,0)); actor.StandingMaterial=Sprite("HamletStanding");
            actor.SeatedMesh=Plane("HamletSeated",new Vector2(34,8)); actor.SeatedMaterial=Sprite("HamletSeated");
            actor.WalkMeshes=Enumerable.Range(0,4).Select(i=>Plane("HamletWalk"+i,new Vector2(46,0))).ToArray();
            actor.WalkMaterials=Enumerable.Range(0,4).Select(i=>Sprite("HamletWalk"+i)).ToArray();
            actor.Show(false,true); EditorUtility.SetDirty(actor);
        }
        private static Texture2D Texture(string name)
        {
            string path=Root+"/"+name+".png"; var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null) throw new InvalidOperationException("Missing source-tracked ACT 4 art: "+path);
            if(importer.filterMode!=FilterMode.Point || importer.mipmapEnabled || importer.textureCompression!=TextureImporterCompression.Uncompressed || importer.npotScale!=TextureImporterNPOTScale.None) {
                importer.filterMode=FilterMode.Point; importer.mipmapEnabled=false; importer.npotScale=TextureImporterNPOTScale.None;
                importer.textureCompression=TextureImporterCompression.Uncompressed; importer.alphaIsTransparency=true; importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        private static Material Sprite(string name)
        {
            var material=new Material(Shader.Find("Universal Render Pipeline/Unlit")); material.SetTexture("_BaseMap",Texture(name));
            material.SetColor("_BaseColor",Color.white); material.SetFloat("_AlphaClip",1); material.SetFloat("_Cutoff",.4f);
            material.SetFloat("_Cull",0); material.SetFloat("_ZWrite",1); material.EnableKeyword("_ALPHATEST_ON"); material.renderQueue=2450;
            return Save(material,name+".mat");
        }
        private static Mesh Plane(string name,Vector2 pivot)
        {
            const float ppu=70; var texture=Texture(name);
            float left=-pivot.x/ppu,right=(texture.width-pivot.x)/ppu,bottom=-pivot.y/ppu,top=(texture.height-pivot.y)/ppu;
            var mesh=new Mesh{vertices=new[]{new Vector3(left,bottom,0),new Vector3(left,top,0),new Vector3(right,top,0),new Vector3(right,bottom,0)},
                uv=new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right},triangles=new[]{0,1,2,0,2,3}};
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return Save(mesh,name+"Mesh.asset");
        }
        private static T Save<T>(T value,string name) where T:Object
        {
            string path=Root+"/"+name; value.name=Path.GetFileNameWithoutExtension(name); var existing=AssetDatabase.LoadAssetAtPath<T>(path);
            if(existing==null) { AssetDatabase.CreateAsset(value,path); return value; }
            EditorUtility.CopySerialized(value,existing); Object.DestroyImmediate(value); AssetDatabase.SaveAssetIfDirty(existing); return existing;
        }
        [Serializable] private sealed class BuildRecord { public string result,scene,output,unity,scope; public string[] messages; public int errors,warnings; public ulong bytes; public double seconds; }
    }
}
