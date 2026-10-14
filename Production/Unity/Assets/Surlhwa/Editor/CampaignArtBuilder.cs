using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Surlhwa.Presentation;
using Object=UnityEngine.Object;
namespace Surlhwa.Editor
{
    // ACT 1 derivatives of the existing code-authored pixel environment.
    // C1 camera, geometry, dimensions, supports and historical source assets stay intact.
    public static class CampaignArtBuilder
    {
        private const string Root="Assets/Surlhwa/Act1Assets";
        public static void Apply(CampaignController c)
        {
            var ground=GameObject.Find("C_SnowGround").GetComponent<Renderer>();
            var material=new Material(ground.sharedMaterials[0]); material.name="ACT 1 winter ground";
            var texture=new Texture2D(512,384,TextureFormat.RGB24,false); var colors=new Color[512*384];
            Color dark=Hex("28343e"),shade=Hex("3f505c"),bank=Hex("647984"),light=Hex("82949b");
            for(int y=0;y<384;y++) for(int x=0;x<512;x++)
            {
                float patch=Noise(x,y,31)*.72f+Noise(x+43,y+97,11)*.28f;
                Color color=patch>.66f ? light : patch>.51f ? bank : patch>.36f ? shade : dark;
                // Sparse clumps and lee shadows, never concentric bands around the bar.
                if(patch>.51f && (x/3+y/2*7)%31==0) color=Color.Lerp(color,light,.32f);
                colors[y*512+x]=color;
            }
            texture.SetPixels(colors); texture.Apply(); string path=Root+"/WinterGround.png";
            byte[] bytes=texture.EncodeToPNG(); if(!File.Exists(path) || !File.ReadAllBytes(path).SequenceEqual(bytes)) File.WriteAllBytes(path,bytes); Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport); var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.filterMode=FilterMode.Point; importer.mipmapEnabled=false; importer.npotScale=TextureImporterNPOTScale.None; importer.textureCompression=TextureImporterCompression.Uncompressed; importer.wrapMode=TextureWrapMode.Clamp; importer.SaveAndReimport();
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path)); material=Save(material,"WinterGround.mat");
            ground.sharedMaterials=ground.sharedMaterials.Select(_=>material).ToArray();
            var props=new GameObject("ACT 1 restrained counter detail").transform;
            var linen=Save(NewMaterial(Hex("8f9080")),"Linen.mat"); var seam=Save(NewMaterial(Hex("616e68")),"LinenSeam.mat"); var brass=Save(NewMaterial(Hex("8c6d42")),"SoftBrass.mat");
            Box(props,"Folded bar cloth",new Vector3(-2.65f,1.0025f,-.63f),new Vector3(.40f,.025f,.22f),linen);
            Box(props,"Cloth fold",new Vector3(-2.65f,1.019f,-.65f),new Vector3(.38f,.008f,.018f),seam);
            Box(props,"Small resting tray",new Vector3(3.3f,1.0025f,-.6f),new Vector3(.48f,.025f,.24f),brass);
            // Lift the ordinary coat out of pure silhouette without replacing identity or pixel source.
            var tint=new Color(1.16f,1.12f,1.04f);
            c.Ordinary.StandingMaterial=ActorMaterial(c.Ordinary.StandingMaterial,"OrdinaryStanding",tint);
            c.Ordinary.SeatedMaterial=ActorMaterial(c.Ordinary.SeatedMaterial,"OrdinarySeated",tint);
            for(int i=0;i<c.Ordinary.WalkMaterials.Length;i++) c.Ordinary.WalkMaterials[i]=ActorMaterial(c.Ordinary.WalkMaterials[i],"OrdinaryWalk"+i,tint);
            c.gameObject.AddComponent<CampaignSnowView>();
        }
        private static Material ActorMaterial(Material source,string name,Color tint) { var m=new Material(source); m.SetColor("_BaseColor",tint); return Save(m,name+".mat"); }
        private static Material NewMaterial(Color color) { var m=new Material(Shader.Find("Universal Render Pipeline/Unlit")); m.SetColor("_BaseColor",color); return m; }
        private static Color Hex(string value) { ColorUtility.TryParseHtmlString("#"+value,out Color c); return c; }
        private static Material Save(Material m,string file)
        { string path=Root+"/"+file; m.name=Path.GetFileNameWithoutExtension(file); var old=AssetDatabase.LoadAssetAtPath<Material>(path); if(old!=null) { EditorUtility.CopySerialized(m,old); Object.DestroyImmediate(m); AssetDatabase.SaveAssetIfDirty(old); return old; } AssetDatabase.CreateAsset(m,path); return m; }
        private static float Noise(int x,int y,int size)
        {
            int cx=x/size,cy=y/size; float fx=Mathf.SmoothStep(0,1,x%size/(float)size),fy=Mathf.SmoothStep(0,1,y%size/(float)size);
            float Sample(int a,int b)=>(((a*73856093)^(b*19349663)^1401)&0x7fffffff)%1000/999f;
            return Mathf.Lerp(Mathf.Lerp(Sample(cx,cy),Sample(cx+1,cy),fx),Mathf.Lerp(Sample(cx,cy+1),Sample(cx+1,cy+1),fx),fy);
        }
        private static void Box(Transform parent,string name,Vector3 position,Vector3 scale,Material material)
        { var g=GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=name; g.transform.SetParent(parent,false); g.transform.position=position; g.transform.localScale=scale; g.GetComponent<Renderer>().sharedMaterial=material; Object.DestroyImmediate(g.GetComponent<Collider>()); }
    }
}
