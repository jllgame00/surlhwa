using System.Linq;
using UnityEngine;
using Surlhwa.Domain;
namespace Surlhwa.Presentation
{
    // The existing empty stool remains the chair; this view only reads the earned receipt.
    public sealed class CampaignMemoryView : MonoBehaviour
    {
        public bool RefineSurfaces;
        private GameObject star; private Material material; private Mesh mesh;
        private GameObject lovers;
        private GameObject windmill;
        private GameObject hamlet;
        private GameObject faust;
        private GameObject simcheong;
        private GameObject odysseus;
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
        public bool SimCheongVisible=>simcheong!=null && simcheong.activeSelf;
        public Transform SimCheongLotus { get; private set; }
        public Transform SimCheongWater { get; private set; }
        public Transform SimCheongRipples { get; private set; }
        public bool OdysseusVisible=>odysseus!=null && odysseus.activeSelf;
        public Transform OdysseusAnchor { get; private set; }
        public Transform OdysseusShipFragment { get; private set; }
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
            var lotus=state?.Memories.FirstOrDefault(x=>x.Id=="mem_06" && x.Variant=="half_submerged_lotus_slow_ripples");
            if(lotus!=null && simcheong==null) CreateSimCheongMemory();
            if(simcheong!=null) { simcheong.SetActive(lotus!=null); if(lotus!=null) AnimateSimCheongRipples(Time.unscaledTime); }
            var voyage=state?.Memories.FirstOrDefault(x=>x.Id=="mem_07" && x.Variant=="anchor_ship_fragment");
            if(voyage!=null && odysseus==null) CreateOdysseusMemory();
            if(odysseus!=null) odysseus.SetActive(voyage!=null);
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
        private void CreateSimCheongMemory()
        {
            // A small remembered waterline rests on the existing counter near her
            // unreserved place. The opaque horizontal surface crosses half the bud;
            // its lower silhouette stays submerged, without making the bar a pool.
            simcheong=new GameObject("MEM-06 half submerged lotus and slow ripples"); simcheong.transform.SetParent(transform,false);
            simcheong.transform.position=new Vector3(-1.65f,.995f,-.65f); simcheong.transform.rotation=Quaternion.identity;
            SimCheongWater=Part(simcheong.transform,"MEM06_","Small supported waterline",Vector3.zero,new Color(.19f,.31f,.32f),new[]{
                new Rect(-.23f,-.19f,.46f,.38f),new Rect(-.30f,-.14f,.60f,.28f),new Rect(-.34f,-.08f,.68f,.16f)});
            SimCheongWater.localRotation=Quaternion.Euler(90,0,0);
            SimCheongLotus=Part(simcheong.transform,"MEM06_","Half submerged lotus",Vector3.zero,new Color(.63f,.48f,.51f),new[]{
                new Rect(-.08f,-.14f,.16f,.14f),new Rect(-.09f,0,.18f,.045f),
                new Rect(-.12f,.03f,.07f,.065f),new Rect(.05f,.03f,.07f,.065f),
                new Rect(-.075f,.06f,.15f,.04f),new Rect(-.035f,.09f,.07f,.055f),new Rect(-.015f,.145f,.03f,.018f)});
            var ripples=new System.Collections.Generic.List<Rect>();
            foreach(float radius in new[]{1f,.72f}) for(int i=0;i<32;i++) {
                float angle=i*Mathf.PI/16;
                ripples.Add(new Rect(Mathf.Cos(angle)*.24f*radius-.013f,Mathf.Sin(angle)*.13f*radius-.006f,.026f,.012f));
            }
            SimCheongRipples=Part(simcheong.transform,"MEM06_","Very slow small ripples",new Vector3(0,.002f,0),new Color(.37f,.47f,.46f),ripples.ToArray());
            SimCheongRipples.localRotation=Quaternion.Euler(90,0,0);
        }
        private void AnimateSimCheongRipples(float time)
        {
            // Absolute presentation time avoids drift. Neither the water support nor
            // lotus moves, and this subtle eighteen-second cycle owns no progress.
            if(SimCheongRipples==null) return;
            float scale=1+.025f*Mathf.Sin(time*.35f);
            SimCheongRipples.localScale=new Vector3(scale,scale,1);
        }
        private void CreateOdysseusMemory()
        {
            // Both fragments rest on the existing floor; fixed world XY keeps their
            // bases supported through focus without completing the future ship.
            odysseus=new GameObject("MEM-07 anchor and ship fragment"); odysseus.transform.SetParent(transform,false);
            odysseus.transform.position=new Vector3(.65f,.005f,3.10f); odysseus.transform.rotation=Quaternion.identity;
            OdysseusAnchor=Part(odysseus.transform,"MEM07_","Small weathered anchor",new Vector3(-.21f,0,0),new Color(.43f,.47f,.44f),new[]{
                new Rect(-.013f,.015f,.026f,.265f),new Rect(-.075f,.23f,.15f,.022f),
                new Rect(-.03f,.28f,.016f,.044f),new Rect(.014f,.28f,.016f,.044f),new Rect(-.03f,.324f,.06f,.016f),new Rect(-.03f,.264f,.06f,.016f),
                new Rect(-.035f,0,.07f,.02f),new Rect(-.07f,.015f,.045f,.02f),new Rect(.025f,.015f,.045f,.02f),
                new Rect(-.10f,.03f,.035f,.03f),new Rect(.065f,.03f,.035f,.03f),new Rect(-.11f,.06f,.025f,.04f),new Rect(.085f,.06f,.025f,.04f),
                new Rect(-.11f,.095f,.05f,.02f),new Rect(.06f,.095f,.05f,.02f)});
            OdysseusShipFragment=Part(odysseus.transform,"MEM07_","Incomplete ship fragment",new Vector3(.145f,0,0),new Color(.56f,.40f,.25f),new[]{
                new Rect(-.11f,0,.19f,.022f),new Rect(-.15f,.022f,.27f,.024f),new Rect(-.175f,.046f,.32f,.026f),
                new Rect(-.175f,.072f,.07f,.025f),new Rect(-.145f,.097f,.035f,.018f),
                new Rect(.11f,.072f,.065f,.028f),new Rect(.14f,.10f,.035f,.04f)});
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
            loversOwned.Add(shape); loversOwned.Add(tint);
            if(RefineSurfaces && !name.ToLowerInvariant().Contains("shadow") && !name.ToLowerInvariant().Contains("water") && !name.ToLowerInvariant().Contains("ripple") && name!="Tiny contained red point")
                SurfaceEdges(go.transform,rectangles,color);
            return go.transform;
        }
        private void SurfaceEdges(Transform parent,Rect[] rectangles,Color color)
        {
            // Small authored edge bands stay inside the original pixel silhouette and support plane.
            for(int side=0;side<2;side++) {
                var points=new System.Collections.Generic.List<Vector3>(); var indices=new System.Collections.Generic.List<int>();
                foreach(var r in rectangles) {
                    float edge=Mathf.Min(.004f,Mathf.Min(r.width,r.height)*.18f); if(edge<.0005f) continue;
                    var band=side==0 ? new Rect(r.xMin,r.yMax-edge,r.width,edge) : new Rect(r.xMin,r.yMin,edge,r.height-edge);
                    int n=points.Count; points.Add(new Vector3(band.xMin,band.yMin,-.001f)); points.Add(new Vector3(band.xMin,band.yMax,-.001f));
                    points.Add(new Vector3(band.xMax,band.yMax,-.001f)); points.Add(new Vector3(band.xMax,band.yMin,-.001f)); indices.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
                }
                var detail=new GameObject(side==0 ? "Worn upper edge" : "Quiet material edge"); detail.transform.SetParent(parent,false);
                var geometry=new Mesh{vertices=points.ToArray(),triangles=indices.ToArray()}; geometry.RecalculateBounds();
                var material=new Material(Shader.Find("Universal Render Pipeline/Unlit")); material.SetColor("_BaseColor",Color.Lerp(color,side==0 ? new Color(.86f,.79f,.65f) : new Color(.16f,.17f,.16f),side==0 ? .22f : .25f)); material.SetFloat("_Cull",0);
                detail.AddComponent<MeshFilter>().sharedMesh=geometry; var surface=detail.AddComponent<MeshRenderer>(); surface.sharedMaterial=material;
                surface.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off; surface.receiveShadows=false; loversOwned.Add(geometry); loversOwned.Add(material);
            }
        }
        private void OnDestroy()
        {
            if(material!=null) Release(material); if(mesh!=null) Release(mesh);
            foreach(var item in loversOwned) if(item!=null) Release(item);
        }
        private static void Release(Object item) { if(UnityEngine.Application.isPlaying) Destroy(item); else DestroyImmediate(item); }
    }
}
