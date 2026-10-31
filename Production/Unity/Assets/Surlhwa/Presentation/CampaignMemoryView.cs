using System.Linq;
using UnityEngine;
using Surlhwa.Domain;
namespace Surlhwa.Presentation
{
    // The existing empty stool remains the chair; this view only reads the earned receipt.
    public sealed class CampaignMemoryView : MonoBehaviour
    {
        private GameObject star; private Material material; private Mesh mesh;
        private GameObject lovers;
        private GameObject windmill;
        private GameObject hamlet;
        private GameObject faust;
        private readonly System.Collections.Generic.List<Object> loversOwned=new System.Collections.Generic.List<Object>();
        public bool LoversVisible=>lovers!=null && lovers.activeSelf;
        public Transform LoversSwing { get; private set; }
        public Transform LoversEmptyPlace { get; private set; }
        public bool WindmillVisible=>windmill!=null && windmill.activeSelf;
        public Transform WindmillFragment { get; private set; }
        public Transform WindmillWing { get; private set; }
        public bool HamletVisible=>hamlet!=null && hamlet.activeSelf;
        public Transform HamletStage { get; private set; }
        public Transform HamletSkull { get; private set; }
        public Transform HamletShadow { get; private set; }
        public bool FaustVisible=>faust!=null && faust.activeSelf;
        public Transform FaustCircle { get; private set; }
        public Transform FaustGlass { get; private set; }
        public Transform FaustGlow { get; private set; }
        private Quaternion presentationFacing=Quaternion.Euler(28,0,0);
        public void SetPresentationFacing(Quaternion facing)
        {
            presentationFacing=facing;
            if(star!=null) star.transform.rotation=facing;
            if(lovers!=null) lovers.transform.rotation=facing;
            if(windmill!=null) windmill.transform.rotation=facing;
        }
        public bool Visible=>star!=null && star.activeSelf; public bool Glowing { get; private set; }
        public void Refresh(CampaignCheckpoint state,Vector3 chair)
        {
            var receipt=state?.Memories.FirstOrDefault(x=>x.Id=="mem_01");
            if(star==null)
            {
                star=new GameObject("MEM-01 small star"); star.transform.SetParent(transform,false);
                var points=new System.Collections.Generic.List<Vector3>(); var indices=new System.Collections.Generic.List<int>();
                string[] pixels={"00100","01110","11111","01110","01010"};
                for(int y=0;y<5;y++) for(int x=0;x<5;x++) if(pixels[y][x]=='1') { int i=points.Count; float l=(x-2.5f)*.026f,b=(4-y)*.026f;
                    points.Add(new Vector3(l,b,0)); points.Add(new Vector3(l,b+.026f,0)); points.Add(new Vector3(l+.026f,b+.026f,0)); points.Add(new Vector3(l+.026f,b,0)); indices.AddRange(new[]{i,i+1,i+2,i,i+2,i+3}); }
                mesh=new Mesh{vertices=points.ToArray(),triangles=indices.ToArray()}; mesh.RecalculateBounds(); star.AddComponent<MeshFilter>().sharedMesh=mesh;
                material=new Material(Shader.Find("Universal Render Pipeline/Unlit")); material.SetFloat("_Cull",0); star.AddComponent<MeshRenderer>().sharedMaterial=material;
            }
            star.SetActive(receipt!=null); Glowing=receipt?.Variant=="star_glowing";
            star.transform.position=chair+new Vector3(.32f,.76f,-.06f); star.transform.rotation=presentationFacing;
            float pulse=Glowing ? .94f+.06f*Mathf.Sin(Time.unscaledTime*1.4f) : 1;
            material.SetColor("_BaseColor",(Glowing ? new Color(1,.87f,.48f) : new Color(.53f,.51f,.42f))*pulse);
            var shared=state?.Memories.FirstOrDefault(x=>x.Id=="mem_02" && x.Variant=="swing_empty_place");
            if(shared!=null && lovers==null) CreateLoversMemory();
            if(lovers!=null) {
                lovers.SetActive(shared!=null);
                lovers.transform.rotation=presentationFacing;
            }
            var fragment=state?.Memories.FirstOrDefault(x=>x.Id=="mem_03" && x.Variant=="windmill_fragment");
            if(fragment!=null && windmill==null) CreateWindmillMemory();
            if(windmill!=null) { windmill.SetActive(fragment!=null); windmill.transform.rotation=presentationFacing; }
            var stage=state?.Memories.FirstOrDefault(x=>x.Id=="mem_04" && x.Variant=="stage_skull_shadow");
            if(stage!=null && hamlet==null) CreateHamletMemory();
            if(hamlet!=null) hamlet.SetActive(stage!=null);
            var cost=state?.Memories.FirstOrDefault(x=>x.Id=="mem_05" && x.Variant=="faint_circle_broken_glass_red_light");
            if(cost!=null && faust==null) CreateFaustMemory();
            if(faust!=null) faust.SetActive(cost!=null);
        }
        private void CreateLoversMemory()
        {
            // One quiet trace on the existing right counter beside the window. It has no interaction, award,
            // camera override or future-revisit animation; the adjacent place stays empty.
            lovers=new GameObject("MEM-02 swing and empty place"); lovers.transform.SetParent(transform,false);
            lovers.transform.position=new Vector3(3.45f,.995f,2.40f); lovers.transform.rotation=presentationFacing;
            LoversSwing=Part("Swing",new Vector3(-.18f,0,0),new Color(.52f,.34f,.19f),new[]{
                new Rect(-.27f,0,.055f,.72f),new Rect(.215f,0,.055f,.72f),new Rect(-.29f,.68f,.58f,.055f),
                new Rect(-.17f,.24f,.34f,.045f),new Rect(-.15f,.28f,.018f,.39f),new Rect(.132f,.28f,.018f,.39f),
                new Rect(-.31f,0,.62f,.028f)});
            LoversEmptyPlace=Part("Adjacent empty place",new Vector3(.40f,0,0),new Color(.38f,.31f,.23f),new[]{
                new Rect(-.17f,0,.34f,.022f),new Rect(-.17f,.12f,.34f,.022f),
                new Rect(-.17f,.022f,.022f,.10f),new Rect(.148f,.022f,.022f,.10f)});
        }
        private void CreateWindmillMemory()
        {
            // Resting on the existing floor below the left shelf, clear of the opaque
            // shelf picture and wall. The attached tilted wing never awards progression.
            windmill=new GameObject("MEM-03 windmill fragment"); windmill.transform.SetParent(transform,false);
            windmill.transform.position=new Vector3(-2.15f,.005f,2.80f); windmill.transform.rotation=presentationFacing;
            WindmillFragment=Part(windmill.transform,"MEM03_","Resting windmill fragment",Vector3.zero,new Color(.58f,.46f,.31f),new[]{
                new Rect(-.12f,0,.24f,.035f),new Rect(-.035f,.025f,.07f,.25f),new Rect(-.065f,.24f,.13f,.12f),
                new Rect(-.20f,.265f,.15f,.045f)});
            WindmillWing=Part(WindmillFragment,"MEM03_","Tilted attached wing",new Vector3(0,.29f,-.003f),new Color(.68f,.55f,.36f),new[]{
                new Rect(-.025f,0,.05f,.235f),new Rect(-.065f,.07f,.13f,.028f),new Rect(-.065f,.14f,.13f,.028f),new Rect(-.065f,.207f,.13f,.028f)});
            WindmillWing.localRotation=Quaternion.Euler(0,0,-28);
        }
        private void CreateHamletMemory()
        {
            // This small physical shelf piece stays in world XY: rotating its base to
            // face the camera would leave the shelf and put the skull behind the niche.
            hamlet=new GameObject("MEM-04 stage skull and shadow"); hamlet.transform.SetParent(transform,false);
            hamlet.transform.position=new Vector3(0,1.087f,2.92f); hamlet.transform.rotation=Quaternion.identity;
            HamletStage=Part(hamlet.transform,"MEM04_","Small stage fragment",Vector3.zero,new Color(.46f,.34f,.23f),new[]{
                new Rect(-.29f,0,.58f,.028f),new Rect(-.26f,.028f,.52f,.026f),new Rect(-.22f,.054f,.39f,.012f)});
            HamletSkull=Part(hamlet.transform,"MEM04_","Quiet skull",new Vector3(-.10f,.066f,-.008f),new Color(.78f,.74f,.61f),new[]{
                new Rect(-.060f,0,.12f,.035f),new Rect(-.075f,.035f,.15f,.035f),
                new Rect(-.090f,.070f,.035f,.070f),new Rect(-.020f,.070f,.040f,.070f),new Rect(.055f,.070f,.035f,.070f),
                new Rect(-.090f,.140f,.18f,.045f),new Rect(-.060f,.185f,.12f,.025f)});
            HamletShadow=Part(hamlet.transform,"MEM04_","Restrained stage shadow",new Vector3(.07f,.066f,.006f),new Color(.19f,.17f,.18f),new[]{
                new Rect(-.05f,0,.13f,.05f),new Rect(-.025f,.05f,.13f,.06f),new Rect(0,.11f,.13f,.08f),
                new Rect(.025f,.19f,.16f,.12f),new Rect(.05f,.31f,.12f,.035f)});
        }
        private void CreateFaustMemory()
        {
            // Canon MEM-05 rests on the floor. Its circle is physically horizontal;
            // glass and tiny contained red point stay in world XY through focus.
            // These are earned traces only, with no light, collider or interaction.
            faust=new GameObject("MEM-05 faint circle broken glass and red light"); faust.transform.SetParent(transform,false);
            faust.transform.position=new Vector3(1.65f,.005f,2.50f); faust.transform.rotation=Quaternion.identity;
            var circle=new System.Collections.Generic.List<Rect>();
            for(int y=-7;y<=7;y++) for(int x=-7;x<=7;x++) {
                float radius=Mathf.Sqrt(x*x+y*y);
                if(radius>=6.2f && radius<=7.3f) circle.Add(new Rect(x*.035f-.0175f,y*.035f-.0175f,.035f,.035f));
            }
            FaustCircle=Part(faust.transform,"MEM05_","Faint floor circle",Vector3.zero,new Color(.32f,.27f,.24f),circle.ToArray());
            FaustCircle.localRotation=Quaternion.Euler(90,0,0);
            // An interrupted bowl with unequal jagged edges and a fallen shard,
            // rather than an intact cup silhouette with a painted crack.
            FaustGlass=Part(faust.transform,"MEM05_","Broken glass",Vector3.zero,new Color(.52f,.54f,.53f),new[]{
                new Rect(-.070f,0,.140f,.018f),new Rect(-.012f,.018f,.024f,.055f),new Rect(-.048f,.073f,.096f,.017f),
                new Rect(-.065f,.085f,.018f,.074f),new Rect(-.081f,.146f,.018f,.039f),new Rect(-.081f,.185f,.032f,.018f),
                new Rect(.045f,.085f,.018f,.045f),new Rect(.060f,.118f,.018f,.035f),new Rect(.060f,.153f,.029f,.018f),
                new Rect(.115f,0,.058f,.016f),new Rect(.130f,.016f,.030f,.018f)});
            FaustGlow=Part(faust.transform,"MEM05_","Tiny contained red point",new Vector3(.004f,.105f,-.004f),new Color(.57f,.105f,.085f),new[]{
                new Rect(-.014f,-.014f,.028f,.028f)});
        }
        private Transform Part(string name,Vector3 at,Color color,Rect[] rectangles)
        { return Part(lovers.transform,"MEM02_",name,at,color,rectangles); }
        private Transform Part(Transform parent,string prefix,string name,Vector3 at,Color color,Rect[] rectangles)
        {
            var go=new GameObject(name); go.transform.SetParent(parent,false); go.transform.localPosition=at;
            var vertices=new System.Collections.Generic.List<Vector3>(); var triangles=new System.Collections.Generic.List<int>();
            foreach(var rect in rectangles) {
                int n=vertices.Count; vertices.Add(new Vector3(rect.xMin,rect.yMin,0)); vertices.Add(new Vector3(rect.xMin,rect.yMax,0));
                vertices.Add(new Vector3(rect.xMax,rect.yMax,0)); vertices.Add(new Vector3(rect.xMax,rect.yMin,0));
                triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
            }
            var shape=new Mesh{vertices=vertices.ToArray(),triangles=triangles.ToArray(),name=prefix+name}; shape.RecalculateBounds();
            var tint=new Material(Shader.Find("Universal Render Pipeline/Unlit")); tint.SetColor("_BaseColor",color); tint.SetFloat("_Cull",0);
            go.AddComponent<MeshFilter>().sharedMesh=shape; var renderer=go.AddComponent<MeshRenderer>(); renderer.sharedMaterial=tint;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows=false;
            loversOwned.Add(shape); loversOwned.Add(tint); return go.transform;
        }
        private void OnDestroy()
        {
            if(material!=null) Release(material); if(mesh!=null) Release(mesh);
            foreach(var item in loversOwned) if(item!=null) Release(item);
        }
        private static void Release(Object item) { if(UnityEngine.Application.isPlaying) Destroy(item); else DestroyImmediate(item); }
    }
}
