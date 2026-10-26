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
    public static class Q1ProductionBuilder
    {
        public const string ScenePath="Assets/Surlhwa/Scenes/Bar_Q1.unity";
        private const string Root="Assets/Surlhwa/Q1Polish";
        private const string Art="Assets/Surlhwa/Act1Assets/ProductionArt";
        private static Transform dressing;

        [MenuItem("Surlhwa/Build active Q1 ACT 1 Player")]
        public static void Build()
        {
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Unsaved scene; no owner scene replaced.");
            Directory.CreateDirectory(Root); CampaignBuilder.Sync(); AssetDatabase.Refresh();
            // The retained review scene supplies the accepted Q1 geometry. Never save over it.
            var scene=EditorSceneManager.OpenScene(QuarterViewBuilder.ScenePath,OpenSceneMode.Single);
            var old=Object.FindFirstObjectByType<QuarterViewSpike>(); var c=old.Owner;
            var facing=Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
                .Where(r=>r.GetComponentInParent<CharacterView>()==null && Quaternion.Angle(r.transform.rotation,Q1ProductionView.FocusAngle)<.1f)
                .Select(r=>r.transform).ToArray();
            old.Select(1); old.Dressing.name="Q1 winter world";
            var go=old.gameObject; go.name="Active Q1 Production presentation";
            Object.DestroyImmediate(go.GetComponent<QuarterViewCapture>()); Object.DestroyImmediate(old);
            var view=go.AddComponent<Q1ProductionView>(); view.Owner=c;
            view.PrinceSideQuarter=Sprite("PrinceSideQuarter",Root+"/PrinceSideQuarter.png");
            view.OrdinaryBackQuarter=AssetDatabase.LoadAssetAtPath<Material>("Assets/Surlhwa/QuarterView/OrdinaryBackQuarter.mat");
            dressing=new GameObject("Q1 bounded polish details").transform;
            // A spare bottle pair and small jigger live on a return, away from serving/input space.
            Plane("Return bottle",Art+"/ToolBottle.png",new Vector3(-3.48f,1.01f,.25f),.18f,.38f);
            Plane("Return spare bottle",Art+"/ToolBottle.png",new Vector3(-3.27f,1.01f,.39f),.15f,.32f);
            Plane("Resting jigger",Art+"/ToolJigger.png",new Vector3(-3.40f,1.01f,-.02f),.14f,.19f);
            Plane("Folded service cloth",Art+"/ClothTray.png",new Vector3(3.45f,1.01f,-.12f),.59f,.28f);
            Plane("Quiet shelf notebook",Art+"/NotebookCandle.png",new Vector3(1.95f,.55f,2.89f),.43f,.28f);
            // Only two low silhouettes supplement the broad snow; no busy world perimeter.
            Plane("Low west accumulation",Art+"/SnowBank.png",new Vector3(-6.55f,-.12f,-2.4f),1.45f,.35f);
            Plane("Quiet east stones",Art+"/SnowRocks.png",new Vector3(6.6f,-.12f,6.5f),1.12f,.48f);
            var path=AssetDatabase.LoadAssetAtPath<Material>("Assets/Surlhwa/QuarterView/SnowPath.mat");
            for(int i=0;i<10;i++) {
                Box("Worn approach print",new Vector3(-4.25f-.05f*i+(i%2==0 ? -.08f:.08f),-.113f,-2.72f-.09f*i),new Vector3(.085f,.003f,.14f),path);
            }
            GroundContacts(c);
            view.FacingProps=facing.Concat(dressing.GetComponentsInChildren<MeshRenderer>().Where(r=>r.GetComponent<MeshFilter>().sharedMesh.name.StartsWith("Q1_")).Select(r=>r.transform)).ToArray();
            view.ApplyView(false); go.AddComponent<Q1PolishCapture>();
            EditorSceneManager.SaveScene(scene,ScenePath);
            // Ordinary Unity Build/Build And Run uses the same owner-locked Production scene.
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            string output=Path.GetFullPath("../Validation/Builds/Q1Production/SurlhwaQ1.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            File.WriteAllText("../Validation/Manifests/q1-polish-build.json",JsonUtility.ToJson(new BuildRecord{result=report.summary.result.ToString(),errors=report.summary.totalErrors,warnings=report.summary.totalWarnings,bytes=report.summary.totalSize,seconds=report.summary.totalTime.TotalSeconds,scene=ScenePath},true));
            File.Copy("Assets/Surlhwa/SliceAssets/Noto-OFL.txt",Path.Combine(Path.GetDirectoryName(output),"Noto-OFL.txt"),true);
            if(report.summary.result!=BuildResult.Succeeded) throw new InvalidOperationException("Q1 build failed");
            Debug.Log("Q1_PRODUCTION_BUILD_SUCCEEDED");
        }
        private static Material Sprite(string name,string texturePath)
        {
            if(texturePath.StartsWith(Root)) {
                var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);
                importer.filterMode=FilterMode.Point; importer.mipmapEnabled=false; importer.npotScale=TextureImporterNPOTScale.None;
                importer.textureCompression=TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
            }
            var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"/"+name+".mat");
            if(material==null) { material=new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(material,Root+"/"+name+".mat"); }
            material.SetColor("_BaseColor",Color.white); material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            if(texturePath.EndsWith("SnowBank.png") || texturePath.EndsWith("SnowRocks.png")) material.SetColor("_BaseColor",new Color(.70f,.78f,.84f));
            material.SetFloat("_AlphaClip",1); material.SetFloat("_Cutoff",.4f); material.SetFloat("_Cull",0); material.SetFloat("_ZWrite",1);
            material.EnableKeyword("_ALPHATEST_ON"); material.renderQueue=2450; AssetDatabase.SaveAssetIfDirty(material); return material;
        }
        private static void Plane(string name,string texture,Vector3 position,float width,float height)
        {
            string key=name.Replace(' ','_'); string path=Root+"/"+key+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null) { mesh=new Mesh(); AssetDatabase.CreateAsset(mesh,path); }
            mesh.name="Q1_"+key; mesh.vertices=new[]{new Vector3(-width/2,0,0),new Vector3(-width/2,height,0),new Vector3(width/2,height,0),new Vector3(width/2,0,0)};
            mesh.uv=new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right}; mesh.triangles=new[]{0,1,2,0,2,3}; mesh.RecalculateNormals(); mesh.RecalculateBounds(); AssetDatabase.SaveAssetIfDirty(mesh);
            var go=new GameObject(name); go.transform.SetParent(dressing); go.transform.position=position; go.transform.rotation=Q1ProductionView.WorldAngle;
            go.AddComponent<MeshFilter>().sharedMesh=mesh; var r=go.AddComponent<MeshRenderer>(); r.sharedMaterial=Sprite(key,texture);
            r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows=false;
        }
        private static void Box(string name,Vector3 position,Vector3 size,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name; go.transform.SetParent(dressing); go.transform.position=position; go.transform.localScale=size;
            Object.DestroyImmediate(go.GetComponent<Collider>()); var r=go.GetComponent<Renderer>(); r.sharedMaterial=material;
            r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows=false;
        }
        private static void GroundContacts(CampaignController owner)
        {
            // A small stepped footprint on the floor; it does not cast a sprite-card shadow.
            var points=new[]{new Vector2(-.5f,-.20f),new Vector2(-.30f,-.20f),new Vector2(-.30f,-.4f),new Vector2(.30f,-.4f),new Vector2(.30f,-.20f),new Vector2(.5f,-.20f),new Vector2(.5f,.20f),new Vector2(.30f,.20f),new Vector2(.30f,.4f),new Vector2(-.30f,.4f),new Vector2(-.30f,.20f),new Vector2(-.5f,.20f)};
            var vertices=new System.Collections.Generic.List<Vector3>{new Vector3(0,.5f,0)};
            vertices.AddRange(points.Select(p=>new Vector3(p.x,.5f,p.y)));
            var triangles=new System.Collections.Generic.List<int>();
            for(int i=0;i<points.Length;i++) { triangles.Add(0); triangles.Add((i+1)%points.Length+1); triangles.Add(i+1); }
            string path=Root+"/GroundContact.asset"; var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null) { mesh=new Mesh(); AssetDatabase.CreateAsset(mesh,path); }
            mesh.vertices=vertices.ToArray(); mesh.triangles=triangles.ToArray(); mesh.RecalculateNormals(); mesh.RecalculateBounds(); AssetDatabase.SaveAssetIfDirty(mesh);
            string matPath=Root+"/GroundContact.mat"; var material=AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if(material==null) { material=new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(material,matPath); }
            material.SetColor("_BaseColor",new Color(.16f,.12f,.10f)); material.SetFloat("_Cull",0); AssetDatabase.SaveAssetIfDirty(material);
            foreach(var actor in new[]{owner.Bartender,owner.Ordinary,owner.Prince}) {
                actor.Contact.GetComponent<MeshFilter>().sharedMesh=mesh;
                actor.Contact.sharedMaterials=new[]{material};
                actor.Contact.transform.localScale=new Vector3(.32f,.008f,.17f);
            }
        }
        [Serializable] private sealed class BuildRecord { public string result,scene; public int errors,warnings; public ulong bytes; public double seconds; }
    }
    // Ordinary Unity Build/Build And Run also validates current authored ACT 1 data.
    public sealed class Q1CampaignBuildPreprocessor : UnityEditor.Build.IPreprocessBuildWithReport
    {
        public int callbackOrder => 1;
        public void OnPreprocessBuild(BuildReport report)
        {
            try { CampaignBuilder.Sync(); }
            catch(Exception error) { throw new UnityEditor.Build.BuildFailedException("Authored ACT 1 content is invalid: "+error.Message); }
        }
    }
}
