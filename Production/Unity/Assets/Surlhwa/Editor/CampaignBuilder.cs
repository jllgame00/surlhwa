using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using Surlhwa.ContentIO;
using Surlhwa.Presentation;
using Object=UnityEngine.Object;
namespace Surlhwa.Editor
{
    public static class CampaignBuilder
    {
        public const string ScenePath="Assets/Surlhwa/Scenes/Bar_Act1.unity";
        public const string BundlePath="Assets/GeneratedContent/act1-content.json";
        private const string Root="Assets/Surlhwa/Act1Assets";
        public static void Sync()
        {
            string json=File.ReadAllText("../Content/Campaign/act1.json"); CampaignContentLoader.Load(json);
            if(!File.Exists(BundlePath) || File.ReadAllText(BundlePath)!=json) File.WriteAllText(BundlePath,json,new System.Text.UTF8Encoding(false));
            AssetDatabase.ImportAsset(BundlePath,ImportAssetOptions.ForceSynchronousImport);
        }
        [MenuItem("Surlhwa/Build ACT 1 campaign")]
        public static void BuildScene()
        {
            Sync(); Directory.CreateDirectory(Root); AssetDatabase.Refresh();
            var scene=EditorSceneManager.OpenScene(SliceBuilder.ScenePath,OpenSceneMode.Single); var old=Object.FindFirstObjectByType<SliceController>();
            var c=new GameObject("Production ACT 1 campaign").AddComponent<CampaignController>();
            c.gameObject.AddComponent<CampaignEvidenceProbe>();
            c.gameObject.AddComponent<CampaignPlayerProof>();
            c.Bundle=AssetDatabase.LoadAssetAtPath<TextAsset>(BundlePath); c.Font=Font(); c.Grip=old.Grip;
            c.Camera=old.Camera; c.Bartender=old.Bartender; c.Ordinary=old.Ordinary; c.Prince=old.Prince;
            c.Room=old.Room; c.Winter=old.Winter; c.Pour=old.Pour; c.Stir=old.Stir; c.Strain=old.Strain; c.Ice=old.Ice; c.Glass=old.Glass; c.Seat=old.Seat; c.Entrance=old.Entrance;
            c.Foot=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/FootGrounded.wav") ?? old.Foot;
            c.Music=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/FirstNightMotif.wav");
            CampaignArtBuilder.Apply(c);
            CampaignProductionArtBuilder.Apply(c);
            Object.DestroyImmediate(old.gameObject); EditorSceneManager.SaveScene(scene,ScenePath);
            Debug.Log("ACT1_SCENE_BUILT: accepted C1 scene geometry retained");
        }
        private static TMP_FontAsset Font()
        {
            string path=Root+"/Act1Korean.asset"; var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if(font==null) {
                font=TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>("Assets/Surlhwa/SliceAssets/NotoSansKR-Regular.ttf"),64,8,GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic,true);
                font.name="ACT 1 Korean"; font.fallbackFontAssetTable=new List<TMP_FontAsset>(); AssetDatabase.CreateAsset(font,path);
                AssetDatabase.AddObjectToAsset(font.material,font); foreach(var texture in font.atlasTextures) AssetDatabase.AddObjectToAsset(texture,font);
            }
            string corpus=File.ReadAllText(BundlePath)+string.Join("",Directory.GetFiles("Assets/Surlhwa/Presentation","Campaign*.cs").Select(File.ReadAllText));
            if(!font.TryAddCharacters(new string(corpus.Where(x=>!char.IsControl(x)).Distinct().ToArray()),out string missing)) throw new InvalidOperationException("Missing ACT 1 glyphs: "+missing);
            EditorUtility.SetDirty(font); AssetDatabase.SaveAssetIfDirty(font); return font;
        }
        public static void BuildPlayer() { BuildScene(); BuildExistingPlayer(); }
        public static void BuildExistingPlayer()
        {
            Sync(); string output=Path.GetFullPath("../Validation/Builds/Act1/SurlhwaAct1.exe"); Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            File.Copy("Assets/Surlhwa/SliceAssets/Noto-OFL.txt",Path.Combine(Path.GetDirectoryName(output),"Noto-OFL.txt"),true);
            File.WriteAllText("../Validation/Manifests/build-act1.json",JsonUtility.ToJson(new BuildEvidence{result=report.summary.result.ToString(),errors=report.summary.totalErrors,warnings=report.summary.totalWarnings,bytes=report.summary.totalSize,seconds=report.summary.totalTime.TotalSeconds,unity=UnityEngine.Application.unityVersion,output=output},true));
            if(report.summary.result!=BuildResult.Succeeded) throw new InvalidOperationException("ACT 1 Player build failed.");
            Debug.Log("ACT1_PLAYER_BUILD_SUCCEEDED: "+output);
        }
        [Serializable] private sealed class BuildEvidence { public string result,unity,output; public int errors,warnings; public ulong bytes; public double seconds; }
    }
}
