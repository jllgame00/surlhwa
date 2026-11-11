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
    public static class CampaignFinalArtBuilder
    {
        public const string ScenePath="Assets/Surlhwa/Scenes/Bar_Q1_Final.unity";
        public const string Root="Assets/Surlhwa/FinalArt";
        [MenuItem("Surlhwa/Prepare final Q1 presentation candidate")]
        public static void PrepareScene()
        {
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Close modified scenes before preparing the separate final candidate.");
            var protectedPaths=new[]{Q1ProductionBuilder.ScenePath,CampaignExpansionBuilder.ScenePath,ActThreeExpansionBuilder.ScenePath,ActFourExpansionBuilder.ScenePath,ActFiveExpansionBuilder.ScenePath,ActSixExpansionBuilder.ScenePath,ActSevenExpansionBuilder.ScenePath};
            var preserved=protectedPaths.ToDictionary(x=>x,File.ReadAllBytes);
            Directory.CreateDirectory(Root); AssetDatabase.Refresh();
            if(!File.Exists(ScenePath) && !AssetDatabase.CopyAsset(ActSevenExpansionBuilder.ScenePath,ScenePath)) throw new InvalidOperationException("Cannot copy the accepted ACT7 scene.");
            var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            var c=Object.FindFirstObjectByType<CampaignController>();
            if(c==null || c.Odysseus==null || c.Art==null) throw new InvalidOperationException("Final candidate requires the complete ACT7 cast and art.");
            var transforms=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).ToDictionary(x=>x.GetInstanceID(),x=>x.localToWorldMatrix);
            Inventory("../Validation/Manifests/p10-candidate-before.json",false);
            var art=Object.Instantiate(AssetDatabase.LoadAssetAtPath<CampaignArtSet>("Assets/Surlhwa/Act1Assets/ProductionArt/Act1Art.asset"));
            art.TallGlass=Texture("TallGlass"); art.Flute=Texture("Flute"); art.OrangeSlice=Texture("OrangeSlice"); art.OrangeZest=Texture("OrangeZest"); art.ShakerSheet=Texture("ShakerSheet"); art.RefineMemorySurfaces=true; art.RefineToolMotion=true;
            c.Art=Save(art,"FinalArt.asset");
            c.Foot=Audio("FootGroundedFinal"); c.Shake=Audio("ShakeContact"); c.Music=Audio("BarReturn");
            var ground=GameObject.Find("Quiet snowy world"); if(ground==null) throw new InvalidOperationException("Missing Q1 snow surface.");
            var groundRenderer=ground.GetComponent<Renderer>(); var snow=new Material(groundRenderer.sharedMaterial); snow.SetTexture("_BaseMap",Texture("QuietSnow")); groundRenderer.sharedMaterial=Save(snow,"QuietSnow.mat");
            Material cap=null;
            foreach(var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None)) {
                var materials=r.sharedMaterials; bool changed=false;
                for(int i=0;i<materials.Length;i++) if(materials[i]!=null && materials[i].name=="SnowCap") {
                    if(cap==null) { var m=new Material(materials[i]); m.SetTexture("_BaseMap",null); m.SetColor("_BaseColor",new Color(.66f,.73f,.77f)); cap=Save(m,"QuietSnowCap.mat"); }
                    materials[i]=cap; changed=true;
                }
                if(changed) r.sharedMaterials=materials;
            }
            c.Font=Font(); EditorUtility.SetDirty(c);
            foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(!transforms.TryGetValue(t.GetInstanceID(),out var matrix) || matrix!=t.localToWorldMatrix) throw new InvalidOperationException("Art finishing moved a protected scene transform: "+t.name);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene,ScenePath);
            foreach(var p in preserved) if(!p.Value.SequenceEqual(File.ReadAllBytes(p.Key))) throw new InvalidOperationException("An accepted source scene changed: "+p.Key);
            Inventory("../Validation/Manifests/p10-candidate-after.json",true);
            Debug.Log("FINAL_Q1_PREPARED: separate bindings, unchanged camera/geometry/cast/content/history.");
        }
        private static Texture2D Texture(string name)
        {
            string path=Root+"/"+name+".png"; var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null) throw new InvalidOperationException("Missing source-tracked final art: "+path);
            importer.filterMode=FilterMode.Point; importer.mipmapEnabled=false; importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.npotScale=TextureImporterNPOTScale.None; importer.alphaIsTransparency=true; importer.maxTextureSize=2048; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        private static TMP_FontAsset Font()
        {
            string path=Root+"/FinalKorean.asset";
            if(!File.Exists(path) && !AssetDatabase.CopyAsset(ActSevenExpansionBuilder.Root+"/Act7Korean.asset",path)) throw new InvalidOperationException("Cannot copy the owned ACT7 font.");
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path); font.name="FinalKorean";
            if(AssetDatabase.GetAssetPath(font.material)!=path || font.atlasTextures.Any(x=>AssetDatabase.GetAssetPath(x)!=path)) throw new InvalidOperationException("Final font must own its material and atlas.");
            string corpus=File.ReadAllText(ActSevenExpansionBuilder.BundlePath)+string.Join("",Directory.GetFiles("Assets/Surlhwa/Presentation","*.cs").Select(File.ReadAllText));
            if(!font.TryAddCharacters(new string(corpus.Where(x=>!char.IsControl(x)).Distinct().ToArray()),out string missing)) throw new InvalidOperationException("Missing final glyphs: "+missing);
            EditorUtility.SetDirty(font); AssetDatabase.SaveAssetIfDirty(font); return font;
        }
        private static AudioClip Audio(string name)
        {
            string path=Root+"/Audio/"+name+".wav"; var importer=AssetImporter.GetAtPath(path) as AudioImporter;
            if(importer==null) throw new InvalidOperationException("Missing original audio source: "+path);
            var settings=importer.defaultSampleSettings; settings.compressionFormat=AudioCompressionFormat.PCM; settings.loadType=AudioClipLoadType.DecompressOnLoad;
            settings.preloadAudioData=true; importer.defaultSampleSettings=settings; importer.SaveAndReimport(); return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        private static T Save<T>(T value,string name) where T:Object
        {
            string path=Root+"/"+name; value.name=Path.GetFileNameWithoutExtension(name); var prior=AssetDatabase.LoadAssetAtPath<T>(path);
            if(prior==null) { AssetDatabase.CreateAsset(value,path); return value; }
            EditorUtility.CopySerialized(value,prior); Object.DestroyImmediate(value); AssetDatabase.SaveAssetIfDirty(prior); return prior;
        }
        private static void Inventory(string path,bool replace)
        {
            if(File.Exists(path) && !replace) return;
            string Location(Transform t)=>t.parent==null ? t.name : Location(t.parent)+"/"+t.name;
            var rows=Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(r=>new RenderRow{
                path=Location(r.transform),active=r.gameObject.activeInHierarchy && r.enabled,
                materials=r.sharedMaterials.Select(m=>m==null ? "MISSING" : AssetDatabase.GetAssetPath(m)).ToArray(),
                position=r.transform.position,scale=r.transform.lossyScale}).OrderBy(x=>x.path).ToArray();
            Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(path,JsonUtility.ToJson(new InventoryRecord{scene=ScenePath,renderers=rows},true));
        }
        [MenuItem("Surlhwa/Build final Q1 candidate Player")]
        public static void Build()
        {
            PrepareScene(); string output=Path.GetFullPath("../Validation/Builds/FinalProduction/SurlhwaFinal.exe"); Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            File.WriteAllText("../Validation/Manifests/final-production-build.json",JsonUtility.ToJson(new BuildRecord{result=report.summary.result.ToString(),errors=report.summary.totalErrors,warnings=report.summary.totalWarnings,bytes=report.summary.totalSize,seconds=report.summary.totalTime.TotalSeconds,scene=ScenePath,output=output,unity=UnityEngine.Application.unityVersion},true));
            File.Copy("Assets/Surlhwa/SliceAssets/Noto-OFL.txt",Path.Combine(Path.GetDirectoryName(output),"Noto-OFL.txt"),true);
            if(report.summary.result!=BuildResult.Succeeded) throw new InvalidOperationException("Final candidate build failed.");
            Debug.Log("FINAL_PRODUCTION_BUILD_SUCCEEDED: "+output);
        }
        [Serializable] private sealed class RenderRow { public string path; public bool active; public string[] materials; public Vector3 position,scale; }
        [Serializable] private sealed class InventoryRecord { public string scene; public RenderRow[] renderers; }
        [Serializable] private sealed class BuildRecord { public string result,scene,output,unity; public int errors,warnings; public ulong bytes; public double seconds; }
    }
}
