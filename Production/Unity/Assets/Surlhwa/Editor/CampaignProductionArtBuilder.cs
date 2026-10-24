using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Surlhwa.Presentation;
using Object=UnityEngine.Object;

namespace Surlhwa.Editor
{
    // A bounded upgrade of the existing ACT 1 scene. No historical scene regeneration.
    public static class CampaignProductionArtBuilder
    {
        private const string Root="Assets/Surlhwa/Act1Assets/ProductionArt";
        private static Transform dressing;
        private static readonly Quaternion View=Quaternion.Euler(28,0,0);

        [MenuItem("Surlhwa/Apply ACT 1 production art to existing scene")]
        public static void UpgradeScene()
        {
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save or close modified Editor scenes before applying ACT 1 art. No scene was replaced.");
            var scene=EditorSceneManager.OpenScene(CampaignBuilder.ScenePath,OpenSceneMode.Single);
            var controller=Object.FindFirstObjectByType<CampaignController>();
            if(controller==null) throw new InvalidOperationException("ACT 1 controller missing; no scene replacement is permitted.");
            Apply(controller);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log("ACT1_PRODUCTION_ART_APPLIED: C1 camera, geometry and contact anchors retained.");
        }
        public static void UpgradeAndBuildPlayer() { UpgradeScene(); CampaignBuilder.BuildExistingPlayer(); }

        public static void Apply(CampaignController controller)
        {
            if(!File.Exists(Root+"/RearWood.png")) throw new InvalidOperationException("Run Production/Tools/prepare_act1_art.py first.");
            AssetDatabase.Refresh();
            var previous=GameObject.Find("ACT 1 production art"); if(previous!=null) Object.DestroyImmediate(previous);
            dressing=new GameObject("ACT 1 production art").transform;
            Surfaces(); Exterior(); Interior(); Characters(controller);
            controller.Foot=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/FootstepFeltWood.wav") ?? controller.Foot;
            foreach(string guid in AssetDatabase.FindAssets("",new[]{Root})) AssetDatabase.SaveAssetIfDirty(new GUID(guid));
            EditorUtility.SetDirty(controller);
        }

        private static void Surfaces()
        {
            Material top=Material("CounterWood",Color.white,false),front=Material("CounterPanels",Color.white,false);
            Material wood=Material("RearWood",Color.white,false),floor=Material("FloorWood",new Color(.82f,.82f,.85f),false);
            foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)) {
                if(r.name=="C_Floor") ReplaceSurface(r,4,floor);
                else if(r.name=="C_RearPlane") ReplaceSurface(r,0,wood);
                else if(r.name=="C_CounterTop" || r.name=="C_CounterReturnTop") ReplaceSurface(r,4,top);
                else if(r.name=="C_CounterFront" || r.name=="C_CounterReturn") ReplaceSurface(r,0,front);
            }
            // Front panel moulding is now in the texture; preserve geometry/colliders.
            Hide("C_CounterPanelTrim");
            Hide("C_SnowAccumulation");
        }
        private static void ReplaceSurface(Renderer renderer,int index,Material value)
        { var materials=renderer.sharedMaterials; if(index>=materials.Length) throw new InvalidOperationException(renderer.name+" face layout changed"); materials[index]=value; renderer.sharedMaterials=materials; }

        private static void Interior()
        {
            foreach(string name in new[]{"C_Bottle","C_BottleNeck","C_BottleCork","C_BottleLabel","C_ShelfBacking","C_Shelf","C_LampBase","C_LampGlass","C_LampFrame","C_LampCap","C_LampSnow","C_EmptyMemoryRecess"}) Hide(name);
            // C_Shelf retains its support/collider; the authored plane supplies the visible shelf.
            foreach(float x in new[]{-2.45f,2.45f}) {
                Plane("Bottle shelf upper","ShelfBottles",new Vector3(x,1.20f,3.10f),2.48f,.75f,Quaternion.identity);
                Plane("Glass shelf lower","ShelfGlasses",new Vector3(x,.50f,3.08f),2.35f,.80f,Quaternion.identity);
            }
            Plane("Empty memory niche","EmptyNiche",new Vector3(0,1.04f,3.05f),1.25f,.98f,Quaternion.identity);
            foreach(float x in new[]{-4.15f,4.15f}) Plane("Warm rear lantern","Lantern",new Vector3(x,1.36f,3.02f),.32f,.60f,Quaternion.identity);
            foreach(float x in new[]{-5.25f,5.25f}) Plane("Warm snow lantern","Lantern",new Vector3(x,-.10f,-2.51f),.30f,.69f,View);
            foreach(var stool in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name=="C_Stool" || t.name=="C_ExtraStool").ToArray()) {
                // Leather is a surface on the accepted seat, not a second elevated seat.
                var renderer=stool.GetComponent<Renderer>(); var materials=renderer.sharedMaterials;
                materials[4]=Material("StoolLeather",Color.white,false); materials[0]=materials[4]; renderer.sharedMaterials=materials;
            }
            // Small life props stay on the returns / edges, clear of active tools and bartender.
            Plane("Clean cloth and glasses","ClothTray",new Vector3(-2.75f,.991f,-.69f),.85f,.43f,View);
            Plane("Ceramic pitcher and cups","CeramicSet",new Vector3(3.43f,.991f,1.22f),.62f,.49f,View);
            Plane("Notebook and candle","NotebookCandle",new Vector3(-3.43f,.991f,1.30f),.67f,.43f,View);
            // Keep the accepted support bounds, but remove visible blockout duplicates.
            Hide("Folded bar cloth"); Hide("Cloth fold"); Hide("Small resting tray");
        }

        private static void Exterior()
        {
            const int width=512,height=384;
            var texture=new Texture2D(width,height,TextureFormat.RGB24,false); var pixels=new Color[width*height];
            Color shadow=Hex("526772"),snow=Hex("73858d"),crest=Hex("99a9ae"),warm=Hex("a08c67");
            for(int y=0;y<height;y++) for(int x=0;x<width;x++) {
                float wx=(x/(float)(width-1)-.5f)*24,wz=(y/(float)(height-1)-.5f)*24+.8f;
                // Broad wind-carved forms with a quiet five-step value ramp. No mottled islands.
                float drift=.48f+.20f*Mathf.Sin(wz*2.1f+wx*.24f+.16f*Mathf.Sin(wx*.55f));
                float q=Mathf.Round(drift*5)/5;
                Color c=Color.Lerp(shadow,snow,q);
                if(Mathf.Sin(wz*3.2f+Mathf.Sin(wx*.65f)*.6f)>.94f) c=Color.Lerp(c,crest,.18f);
                float spill=0;
                foreach(float lx in new[]{-5.25f,5.25f}) {
                    float dx=(wx-lx)/1.05f,dz=(wz+2.5f)/1.35f;
                    spill=Mathf.Max(spill,Mathf.Clamp01(1-dx*dx-dz*dz));
                }
                // Warmth is baked in actual ground pixels, not a bloom/filter over characters.
                c=Color.Lerp(c,warm,Mathf.Round(spill*5)/5*.55f);
                pixels[y*width+x]=c;
            }
            texture.SetPixels(pixels); texture.Apply(); string path=Root+"/SnowGround.png";
            WriteTexture(path,texture); Object.DestroyImmediate(texture);
            var ground=GameObject.Find("C_SnowGround").GetComponent<Renderer>();
            var material=Material("SnowGround",Color.white,false); ground.sharedMaterials=ground.sharedMaterials.Select(_=>material).ToArray();
            // These silhouettes remain beyond the existing floor footprint and walking corridor.
            Plane("Distant west pine","DistantPines",new Vector3(-6.0f,-.12f,6.9f),2.6f,2.3f,View,new Color(.53f,.63f,.68f));
            Plane("Distant east pine","DistantPines",new Vector3(5.8f,-.12f,7.8f),2.2f,2.0f,View,new Color(.49f,.59f,.65f));
            Plane("West winter pine","SnowPine",new Vector3(-6.25f,-.12f,2.9f),1.35f,2.45f,View,new Color(.70f,.78f,.82f));
            Plane("East winter pine","SnowPine",new Vector3(6.12f,-.12f,2.0f),1.2f,2.2f,View,new Color(.65f,.74f,.80f));
            Plane("West snow rocks","SnowRocks",new Vector3(-5.85f,-.12f,-.7f),1.26f,.68f,View,new Color(.74f,.82f,.86f));
            Plane("East snow rocks","SnowRocks",new Vector3(5.8f,-.12f,-1.3f),1.18f,.63f,View,new Color(.74f,.82f,.86f));
            Plane("West snow bank","SnowBank",new Vector3(-5.0f,-.12f,-4.0f),1.55f,.58f,View,new Color(.69f,.78f,.84f));
            Plane("East snow bank","SnowBank",new Vector3(5.15f,-.12f,-3.7f),1.5f,.60f,View,new Color(.69f,.78f,.84f));
        }

        private static void Characters(CampaignController c)
        {
            Bind(c.Bartender,"Bartender",52); Bind(c.Prince,"Prince",70); Bind(c.Ordinary,"OrdinaryCoat",70);
            var art=AssetDatabase.LoadAssetAtPath<CampaignArtSet>(Root+"/Act1Art.asset");
            if(art==null) { art=ScriptableObject.CreateInstance<CampaignArtSet>(); AssetDatabase.CreateAsset(art,Root+"/Act1Art.asset"); }
            art.Bottle=Texture("ToolBottle"); art.Jigger=Texture("ToolJigger"); art.Mixing=Texture("ToolMixing"); art.Rocks=Texture("ToolRocks");
            art.Cocktail=Texture("ToolCocktail"); art.Spoon=Texture("ToolSpoon"); art.FloatSpoon=Texture("ToolFloatSpoon"); art.Strainer=Texture("ToolStrainer"); art.Ice=Texture("ToolIce"); art.Grip=Texture("ToolGrip");
            art.BartenderIdle=new[]{"Standing","Towel","Blink","Listening"}.Select(x=>Material("Bartender"+x,Color.white)).ToArray();
            art.OrdinaryCoat=new[]{"Standing","Seated","WalkA","WalkB"}.Select(x=>Material("OrdinaryCoat"+x,Color.white)).ToArray();
            art.OrdinaryBun=new[]{"Standing","Seated","WalkA","WalkB"}.Select(x=>Material("OrdinaryBun"+x,Color.white)).ToArray();
            c.Art=art; EditorUtility.SetDirty(art); AssetDatabase.SaveAssetIfDirty(art);
        }
        private static void Bind(CharacterView actor,string prefix,float ppu)
        {
            actor.StandingMesh=SpriteMesh(prefix+"Standing",new Vector2(48,0),ppu);
            actor.StandingMaterial=Material(prefix+"Standing",Color.white);
            if(prefix=="Bartender") { actor.SeatedMesh=actor.StandingMesh; actor.SeatedMaterial=actor.StandingMaterial; actor.Show(true,false); }
            else {
                actor.SeatedMesh=SpriteMesh(prefix+"Seated",new Vector2(38,15),ppu); actor.SeatedMaterial=Material(prefix+"Seated",Color.white);
                actor.WalkMeshes=new[]{SpriteMesh(prefix+"WalkA",new Vector2(48,0),ppu),actor.StandingMesh,SpriteMesh(prefix+"WalkB",new Vector2(48,0),ppu),actor.StandingMesh};
                actor.WalkMaterials=new[]{Material(prefix+"WalkA",Color.white),actor.StandingMaterial,Material(prefix+"WalkB",Color.white),actor.StandingMaterial}; actor.Show(false,true);
            }
            EditorUtility.SetDirty(actor);
        }
        private static Mesh SpriteMesh(string name,Vector2 pivot,float ppu)
        {
            var texture=Texture(name); return Save(Quad(-pivot.x/ppu,(texture.width-pivot.x)/ppu,-pivot.y/ppu,(texture.height-pivot.y)/ppu),name+"Mesh.asset");
        }
        private static void Hide(string name) { foreach(var renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(x=>x.name==name)) renderer.enabled=false; }
        private static Texture2D Texture(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/"+name+".png") ?? throw new InvalidOperationException("Missing production art: "+name);
        private static void Plane(string name,string texture,Vector3 at,float width,float height,Quaternion rotation,Color? tint=null)
        {
            var go=new GameObject(name); go.transform.SetParent(dressing,false); go.transform.position=at; go.transform.rotation=rotation;
            go.AddComponent<MeshFilter>().sharedMesh=Save(Quad(-width/2,width/2,0,height),name.Replace(' ','_')+"Mesh.asset");
            var renderer=go.AddComponent<MeshRenderer>(); renderer.sharedMaterial=Material(texture,tint ?? Color.white,true,name.Replace(' ','_')); renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false;
        }
        private static Mesh Quad(float left,float right,float bottom,float top)
        {
            var mesh=new Mesh { vertices=new[]{new Vector3(left,bottom,0),new Vector3(left,top,0),new Vector3(right,top,0),new Vector3(right,bottom,0)},
                uv=new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right},triangles=new[]{0,1,2,0,2,3} };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
        private static Material Material(string texture,Color tint,bool alpha=true,string name=null)
        {
            var value=new Material(Shader.Find("Universal Render Pipeline/Unlit")); value.SetTexture("_BaseMap",Texture(texture)); value.SetColor("_BaseColor",tint); value.SetFloat("_Cull",0);
            if(alpha) { value.SetFloat("_AlphaClip",1); value.SetFloat("_Cutoff",.4f); value.SetFloat("_ZWrite",1); value.EnableKeyword("_ALPHATEST_ON"); value.renderQueue=2450; }
            return Save(value,(name ?? texture)+".mat");
        }
        private static T Save<T>(T value,string name) where T:Object
        {
            string path=Root+"/"+name; value.name=Path.GetFileNameWithoutExtension(name); var existing=AssetDatabase.LoadAssetAtPath<T>(path);
            if(existing!=null) { EditorUtility.CopySerialized(value,existing); Object.DestroyImmediate(value); AssetDatabase.SaveAssetIfDirty(existing); return existing; }
            AssetDatabase.CreateAsset(value,path); return value;
        }
        private static Color Hex(string text) { ColorUtility.TryParseHtmlString("#"+text,out var color); return color; }
        private static void WriteTexture(string path,Texture2D texture)
        {
            var bytes=texture.EncodeToPNG();
            if(!File.Exists(path) || !File.ReadAllBytes(path).SequenceEqual(bytes)) {
                string temp=path+".pending"; File.WriteAllBytes(temp,bytes); if(File.Exists(path)) File.Replace(temp,path,null); else File.Move(temp,path);
            }
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path); importer.filterMode=FilterMode.Point; importer.mipmapEnabled=false;
            importer.npotScale=TextureImporterNPOTScale.None; importer.textureCompression=TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
        }
    }
}
