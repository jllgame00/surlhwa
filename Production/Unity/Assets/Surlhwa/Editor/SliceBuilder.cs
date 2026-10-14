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
using Surlhwa.Presentation;
using Object=UnityEngine.Object;

namespace Surlhwa.Editor
{
    public static class SliceBuilder
    {
        public const string ScenePath="Assets/Surlhwa/Scenes/Bar_Slice.unity";
        private const string Root="Assets/Surlhwa/SliceAssets";
        [MenuItem("Surlhwa/Build approved C1 ACT 1 Slice")]
        public static void BuildScene()
        {
            SliceContentGenerator.Sync();
            if(Shader.Find("TextMeshPro/Distance Field")==null)
            {
                string package=UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_Text).Assembly).resolvedPath;
                AssetDatabase.ImportPackage(Path.Combine(package,"Package Resources/TMP Essential Resources.unitypackage"),false);
            }
            foreach(string file in Directory.GetFiles(Root,"*.png"))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(file.Replace('\\','/'));
                importer.filterMode=FilterMode.Point; importer.mipmapEnabled=false; importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency=true; importer.npotScale=TextureImporterNPOTScale.None; importer.SaveAndReimport();
            }
            var font=MakeFont(); var scene=EditorSceneManager.OpenScene("Assets/Surlhwa/Scenes/Bar_C1.unity",OpenSceneMode.Single);
            var reference=Object.FindFirstObjectByType<PixelCutawayStage>(); var bartender=reference.Bartender; var ordinary=reference.Ordinary; var fixture=reference.Quixote; var camera=reference.Camera;
            reference.Select(3); Object.DestroyImmediate(reference.gameObject);
            var prince=Object.Instantiate(fixture.gameObject,fixture.transform.parent).GetComponent<CharacterView>(); prince.name="Slice_LittlePrince";
            prince.GuestId="guest_little_prince"; prince.VisualBindingId="visual_prince"; prince.transform.position=ordinary.transform.position;
            // Authored pixel pivots align the bent seated hips with the accepted C1 seat contact.
            prince.StandingMesh=Plane("PrinceStanding",new Vector2(30.5f,0),70);
            prince.SeatedMesh=Plane("PrinceSeated",new Vector2(38,15),70);
            prince.StandingMaterial=Sprite("PrinceStanding"); prince.SeatedMaterial=Sprite("PrinceSeated");
            prince.WalkMeshes=new Mesh[4]; prince.WalkMaterials=new Material[4];
            for(int i=0;i<4;i++) { prince.WalkMeshes[i]=Plane("PrinceWalk"+i,new Vector2(48,0),70); prince.WalkMaterials[i]=Sprite("PrinceWalk"+i); }
            prince.SeatAnchor.localPosition=Quaternion.Euler(28,0,0)*new Vector3(12f/70,26f/70,0);
            Object.DestroyImmediate(fixture.gameObject); prince.Show(false,true); ordinary.Show(false,true); bartender.Show(true,false);
            if(camera.GetComponent<AudioListener>()==null) camera.gameObject.AddComponent<AudioListener>();
            var controller=new GameObject("Production ACT 1 Stir Slice").AddComponent<SliceController>();
            controller.gameObject.AddComponent<SliceEvidenceRun>();
            controller.Bundle=AssetDatabase.LoadAssetAtPath<TextAsset>(SliceContentGenerator.Destination); controller.Font=font; controller.Grip=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Grip.png");
            controller.Camera=camera; controller.Bartender=bartender; controller.Ordinary=ordinary; controller.Prince=prince;
            controller.Room=Clip("RoomTone"); controller.Winter=Clip("WinterAir"); controller.Pour=Clip("Pour"); controller.Stir=Clip("Stir"); controller.Strain=Clip("Strain");
            controller.Ice=Clip("Ice"); controller.Glass=Clip("GlassSet"); controller.Seat=Clip("Seat"); controller.Entrance=Clip("EntranceAir"); controller.Foot=Clip("FootLeft");
            EditorSceneManager.SaveScene(scene,ScenePath);
            foreach(string guid in AssetDatabase.FindAssets("",new[]{Root,"Assets/GeneratedContent"})) AssetDatabase.SaveAssetIfDirty(new GUID(guid));
            Debug.Log("SLICE_SCENE_BUILT: C1 camera and environment retained; Prince ACT 1 excerpt");
        }
        private static AudioClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/"+name+".wav");
        private static TMP_FontAsset MakeFont()
        {
            string path=Root+"/SliceKorean.asset"; var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if(font==null) {
                font=TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>(Root+"/NotoSansKR-Regular.ttf"),64,8,GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic,true);
                font.name="Slice Korean"; font.fallbackFontAssetTable=new List<TMP_FontAsset>(); AssetDatabase.CreateAsset(font,path);
                AssetDatabase.AddObjectToAsset(font.material,font); foreach(var texture in font.atlasTextures) AssetDatabase.AddObjectToAsset(texture,font);
            }
            string corpus=File.ReadAllText(SliceContentGenerator.Destination)+string.Join("",Directory.GetFiles("Assets/Surlhwa/Presentation","Slice*.cs").Select(File.ReadAllText));
            string characters=new string(corpus.Where(x=>!char.IsControl(x)).Distinct().ToArray());
            if(!font.TryAddCharacters(characters,out string missing)) throw new InvalidOperationException("Missing Slice glyphs: "+missing);
            EditorUtility.SetDirty(font); AssetDatabase.SaveAssetIfDirty(font); return font;
        }
        private static Mesh Plane(string file,Vector2 pivot,float ppu)
        {
            var t=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/"+file+".png"); float l=-pivot.x/ppu,r=(t.width-pivot.x)/ppu,b=-pivot.y/ppu,top=(t.height-pivot.y)/ppu;
            var mesh=new Mesh { vertices=new[]{new Vector3(l,b,0),new Vector3(l,top,0),new Vector3(r,top,0),new Vector3(r,b,0)},uv=new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right},triangles=new[]{0,1,2,0,2,3} };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return Save(mesh,file+"Mesh.asset");
        }
        private static Material Sprite(string file)
        {
            var mat=new Material(Shader.Find("Universal Render Pipeline/Unlit")); mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/"+file+".png"));
            mat.SetColor("_BaseColor",new Color(.96f,.88f,.77f)); mat.SetFloat("_AlphaClip",1); mat.SetFloat("_Cutoff",.4f); mat.SetFloat("_Cull",0); mat.SetFloat("_ZWrite",1); mat.EnableKeyword("_ALPHATEST_ON"); mat.renderQueue=2450;
            return Save(mat,file+".mat");
        }
        private static T Save<T>(T asset,string file) where T:Object
        { string path=Root+"/"+file; asset.name=Path.GetFileNameWithoutExtension(file); var old=AssetDatabase.LoadAssetAtPath<T>(path); if(old!=null) { EditorUtility.CopySerialized(asset,old); Object.DestroyImmediate(asset); return old; } AssetDatabase.CreateAsset(asset,path); return asset; }
        public static void BuildPlayer()
        {
            BuildScene(); BuildExistingPlayer();
        }
        public static void BuildExistingPlayer()
        {
            string output=Path.GetFullPath("../Validation/Builds/Slice/SurlhwaSlice.exe"); Directory.CreateDirectory(Path.GetDirectoryName(output));
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[]{UnityEngine.Rendering.GraphicsDeviceType.Direct3D11});
            PlayerSettings.defaultScreenWidth=1280; PlayerSettings.defaultScreenHeight=720; PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            PlayerSettings.runInBackground=true;
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            File.Copy(Root+"/Noto-OFL.txt",Path.Combine(Path.GetDirectoryName(output),"Noto-OFL.txt"),true);
            File.WriteAllText("../Validation/Manifests/build-slice.json",JsonUtility.ToJson(new BuildEvidence{result=report.summary.result.ToString(),errors=report.summary.totalErrors,warnings=report.summary.totalWarnings,bytes=report.summary.totalSize,seconds=report.summary.totalTime.TotalSeconds,unity=UnityEngine.Application.unityVersion,output=output},true));
            if(report.summary.result!=BuildResult.Succeeded) throw new InvalidOperationException("Slice Player build failed.");
            Debug.Log("SLICE_PLAYER_BUILD_SUCCEEDED: "+output);
        }
        [Serializable] private sealed class BuildEvidence { public string result,unity,output; public int errors,warnings; public ulong bytes; public double seconds; }
    }
}
