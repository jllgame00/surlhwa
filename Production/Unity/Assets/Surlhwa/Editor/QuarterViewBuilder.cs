using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Surlhwa.Presentation;
using Object=UnityEngine.Object;

namespace Surlhwa.Editor
{
    public static class QuarterViewBuilder
    {
        public const string ScenePath="Assets/Surlhwa/Scenes/Bar_QuarterView.unity";
        private const string Root="Assets/Surlhwa/QuarterView";
        private static Transform dressing;
        [MenuItem("Surlhwa/Quarter View spike/Build separate review Player")]
        public static void Build()
        {
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Unsaved scene: no owner scene replaced.");
            Directory.CreateDirectory(Root); AssetDatabase.Refresh();
            var scene=EditorSceneManager.OpenScene(CampaignBuilder.ScenePath,OpenSceneMode.Single);
            var c=Object.FindFirstObjectByType<CampaignController>();
            if(c==null || c.Art==null) throw new InvalidOperationException("Accepted ACT 1 scene/art required.");
            var stage=new GameObject("Quarter View disposable comparison").AddComponent<QuarterViewSpike>();
            stage.Owner=c; stage.Candidate=1;
            dressing=new GameObject("Quarter View dressing ONLY").transform;
            stage.Dressing=dressing.gameObject;
            stage.PrinceBackQuarter=Sprite("PrinceBackQuarter"); stage.OrdinaryBackQuarter=Sprite("OrdinaryBackQuarter");
            Box("Quiet snowy world",new Vector3(0,-.23f,.8f),new Vector3(42,.20f,42),Snow());
            Material dark=Mat("SnowPath","40535d"), snow=Mat("SnowRidge","81939b"), pale=Mat("SnowCrest","a1b0b3"), wood=Mat("WeatheredFence","34444c");
            // Shallow stepped snow on the same ground; no raised island / enclosing blue moat.
            for(int i=0;i<18;i++) {
                float z=-3.5f-i*.31f, x=-4.20f-i*.14f;
                Box("Footprint",new Vector3(x+(i%2==0 ? -.09f : .09f),-.114f,z),new Vector3(.105f,.004f,.20f),dark);
            }
            for(int side=-1;side<=1;side+=2) for(int i=0;i<8;i++) {
                float x=side*(4.94f+(i%3)*.12f), z=-2.2f+i*.82f;
                Box("Low drift",new Vector3(x,-.08f,z),new Vector3(.42f+(i%2)*.25f,.07f,.95f),snow);
                Box("Drift crest",new Vector3(x,-.04f,z+.06f),new Vector3(.28f,.018f,.65f),pale);
            }
            // Sparse far boundary, deliberately disconnected from any overworld/gameplay.
            for(int i=0;i<5;i++) Box("Distant fence post",new Vector3(-8+i*.85f,.15f,6.1f),new Vector3(.10f,.6f,.12f),wood);
            Box("Distant fence rail",new Vector3(-6.3f,.28f,6.1f),new Vector3(3.5f,.08f,.07f),wood);
            // A few physical shelf lips clarify the support under existing pixel bottle art.
            var shelf=AssetDatabase.LoadAssetAtPath<Material>("Assets/Surlhwa/Act1Assets/ProductionArt/CounterWood.mat");
            foreach(float x in new[]{-2.45f,2.45f}) foreach(float y in new[]{.52f,1.21f})
                Box("Shelf lip",new Vector3(x,y,3.03f),new Vector3(2.4f,.045f,.17f),shelf);
            dressing.gameObject.SetActive(false);
            stage.gameObject.AddComponent<QuarterViewCapture>();
            // Save a sibling; neither original scene nor its assets are rewritten.
            EditorSceneManager.SaveScene(scene,ScenePath);
            string output=Path.GetFullPath("../Validation/Builds/QuarterView/SurlhwaQuarterView.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            File.WriteAllText("../Validation/Manifests/quarter-view-build.json",JsonUtility.ToJson(new BuildRecord{result=report.summary.result.ToString(),errors=report.summary.totalErrors,warnings=report.summary.totalWarnings,bytes=report.summary.totalSize,seconds=report.summary.totalTime.TotalSeconds},true));
            File.Copy("Assets/Surlhwa/SliceAssets/Noto-OFL.txt",Path.Combine(Path.GetDirectoryName(output),"Noto-OFL.txt"),true);
            if(report.summary.result!=BuildResult.Succeeded) throw new InvalidOperationException("Quarter View build failed");
            Debug.Log("QUARTER_VIEW_BUILD_SUCCEEDED");
        }
        private static Material Mat(string name,string hex)
        {
            string path=Root+"/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null) { material=new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(material,path); }
            ColorUtility.TryParseHtmlString("#"+hex,out var color); material.SetColor("_BaseColor",color);
            AssetDatabase.SaveAssetIfDirty(material); return material;
        }
        private static Material Sprite(string name)
        {
            string path=Root+"/"+name+".png";
            if(!File.Exists(path)) return null;
            ImportPixels(path);
            var material=Mat(name,"ffffff"); material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path));
            material.SetFloat("_AlphaClip",1); material.SetFloat("_Cutoff",.4f); material.SetFloat("_Cull",0); material.SetFloat("_ZWrite",1);
            material.EnableKeyword("_ALPHATEST_ON"); material.renderQueue=2450; AssetDatabase.SaveAssetIfDirty(material); return material;
        }
        private static Material Snow()
        {
            const int size=512; var tex=new Texture2D(size,size,TextureFormat.RGB24,false);
            ColorUtility.TryParseHtmlString("#596e7a",out var dark); ColorUtility.TryParseHtmlString("#6b808a",out var light);
            ColorUtility.TryParseHtmlString("#b39b70",out var warm);
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++) for(int x=0;x<size;x++) {
                float wx=(x/(float)size-.5f)*42,wz=(y/(float)size-.5f)*42+.8f;
                // Bounded broad shapes, five value bands, no glossy normals or post-process filter.
                float n=Mathf.PerlinNoise(wx*.22f+31,wz*.34f+19)*.70f+Mathf.PerlinNoise(wx*.65f+12,wz*.48f+47)*.30f;
                float band=Mathf.Floor(n*5)/5; Color color=Color.Lerp(dark,light,band);
                float spill=0;
                foreach(float lx in new[]{-5.25f,5.25f}) spill=Mathf.Max(spill,Mathf.Clamp01(1-Mathf.Pow((wx-lx)/1.5f,2)-Mathf.Pow((wz+2.5f)/1.6f,2)));
                color=Color.Lerp(color,warm,Mathf.Floor(spill*5)/5*.34f);
                pixels[y*size+x]=color;
            }
            tex.SetPixels(pixels); tex.Apply(); string path=Root+"/QuietSnow.png"; File.WriteAllBytes(path,tex.EncodeToPNG()); Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport); ImportPixels(path);
            var material=Mat("QuietSnow","ffffff"); material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path)); AssetDatabase.SaveAssetIfDirty(material); return material;
        }
        private static void ImportPixels(string path)
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path); importer.filterMode=FilterMode.Point; importer.mipmapEnabled=false;
            importer.npotScale=TextureImporterNPOTScale.None; importer.textureCompression=TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
        }
        private static void Box(string name,Vector3 position,Vector3 size,Material mat)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name; go.transform.SetParent(dressing);
            go.transform.position=position; go.transform.localScale=size; Object.DestroyImmediate(go.GetComponent<Collider>());
            var r=go.GetComponent<Renderer>(); r.sharedMaterial=mat; r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows=false;
        }
        [Serializable] private sealed class BuildRecord { public string result; public int errors,warnings; public ulong bytes; public double seconds; }
    }
}
