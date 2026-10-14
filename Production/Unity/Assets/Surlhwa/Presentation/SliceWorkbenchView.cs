using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Surlhwa.Domain;

namespace Surlhwa.Presentation
{
    public sealed class SliceWorkbenchView : MonoBehaviour
    {
        public bool FlatTools; public bool CocktailGlass; public decimal ServingFillMl { get; private set; }
        public bool ExtendedTools; public decimal ShakerFillMl { get; private set; } public int VisibleLayerCount { get; private set; }
        public float ServingStemTop => .17f; public float ServingFillBottom => CocktailGlass ? .19f : .035f;
        public Vector3 MixingCenter => transform.TransformPoint(new Vector3(.75f,.4f,0));
        private readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>(); private readonly List<Transform> depth=new List<Transform>();
        private Transform bottle,jigger,mixing,serving,spoon,hand; private MeshFilter jiggerFill,mixingFill,servingFill;
        private GameObject mixingIce,servingIce,cherry; private LineRenderer servingOutline,servingRim,stream; private Material liquid;
        private bool initialized; private int lastRevision=-1; private bool lastGlass;
        private Transform shaker; private MeshFilter shakerFill; private GameObject shakerIce,shakerLid,orange;
        private readonly List<MeshFilter> layerFills=new List<MeshFilter>();
        public void Initialize(Texture2D grip)
        {
            if(initialized) return; initialized=true; transform.rotation=Quaternion.Euler(28,0,0);
            var edge=Mat(new Color(.75f,.81f,.78f)); liquid=Mat(new Color(.64f,.4f,.19f)); var iron=Mat(new Color(.63f,.65f,.62f));
            bottle=Node("Selected bottle",new Vector3(-.65f,.02f,0));
            Outline(bottle,new[]{new Vector3(-.13f,0,0),new Vector3(-.15f,.4f,0),new Vector3(-.06f,.48f,0),new Vector3(-.05f,.67f,0),new Vector3(.05f,.67f,0),new Vector3(.06f,.48f,0),new Vector3(.15f,.4f,0),new Vector3(.13f,0,0)},edge,true);
            Quad(bottle,"Bottle glass",new Rect(-.12f,.04f,.24f,.35f),Mat(new Color(.19f,.3f,.24f)),.02f);
            Quad(bottle,"Label",new Rect(-.1f,.17f,.2f,.15f),Mat(new Color(.74f,.65f,.45f)),-.02f);
            jigger=Node("Jigger 60 ml",Vector3.zero); mixing=Node("Mixing glass 300 ml",new Vector3(.75f,0,0)); serving=Node("Serving glass 180 ml",new Vector3(1.5f,0,0));
            Outline(jigger,new[]{new Vector3(-.16f,.39f,0),new Vector3(-.07f,.2f,0),new Vector3(-.12f,0,0),new Vector3(.12f,0,0),new Vector3(.07f,.2f,0),new Vector3(.16f,.39f,0)},iron,true);
            Outline(mixing,new[]{new Vector3(-.23f,.55f,0),new Vector3(-.19f,0,0),new Vector3(.19f,0,0),new Vector3(.23f,.55f,0)},edge,false);
            servingOutline=Outline(serving,new Vector3[4],edge,false);
            foreach(var item in new[]{jigger,mixing,serving}) {
                float width=item==mixing ? .23f : .16f, y=item==jigger ? .39f : .55f;
                var rim=Node("Shallow rim",Vector3.zero,item); depth.Add(rim);
                var rimLine=Outline(rim,new[]{new Vector3(-width,y,0),new Vector3(-width,y,.08f),new Vector3(width,y,.08f),new Vector3(width,y,0)},edge,false); if(item==serving) servingRim=rimLine;
            }
            jiggerFill=Quad(jigger,"Actual jigger liquid",new Rect(0,0,0,0),Mat(Color.white),.015f);
            mixingFill=Quad(mixing,"Actual mixing liquid",new Rect(0,0,0,0),Mat(Color.white),.015f);
            servingFill=Quad(serving,"Actual bowl liquid",new Rect(0,0,0,0),Mat(Color.white),.015f);
            mixingIce=Ice(mixing); servingIce=Ice(serving); cherry=Quad(serving,"Cherry in glass",new Rect(.075f,.3f,.07f,.07f),Mat(new Color(.52f,.14f,.15f)),-.02f).gameObject;
            spoon=Node("Spoon grip",new Vector3(.75f,.4f,-.04f)); Outline(spoon,new[]{Vector3.zero,new Vector3(.12f,.32f,0)},iron,false,.018f);
            Quad(spoon,"Spoon bowl",new Rect(-.035f,-.03f,.07f,.09f),iron,0);
            hand=Node("2D grip",Vector3.zero);
            if(grip!=null) { var mat=Mat(Color.white); mat.SetTexture("_BaseMap",grip); mat.SetFloat("_AlphaClip",1); mat.SetFloat("_Cutoff",.4f); mat.EnableKeyword("_ALPHATEST_ON"); mat.renderQueue=2450; Quad(hand,"Pixel hand",new Rect(-.10f,-.07f,.24f,.18f),mat,-.07f); }
            else Debug.LogWarning("Slice hand artwork missing; logical actions remain available.");
            stream=Outline(transform,new[]{Vector3.zero,Vector3.zero},liquid,false,.012f); stream.gameObject.name="Accepted liquid stream";
            hand.gameObject.SetActive(false); stream.enabled=false;
        }
        private Transform Node(string label,Vector3 position,Transform parent=null)
        { var t=new GameObject(label).transform; t.SetParent(parent ?? transform,false); t.localPosition=position; return t; }
        private Material Mat(Color color)
        { var m=new Material(Shader.Find("Universal Render Pipeline/Unlit")); m.SetColor("_BaseColor",color); m.SetFloat("_Cull",0); owned.Add(m); return m; }
        private LineRenderer Outline(Transform parent,Vector3[] points,Material mat,bool loop,float width=.012f)
        {
            var line=Node("Outline",Vector3.zero,parent).gameObject.AddComponent<LineRenderer>(); line.useWorldSpace=false; line.sharedMaterial=mat; line.widthMultiplier=width;
            line.positionCount=points.Length; line.SetPositions(points); line.loop=loop; line.shadowCastingMode=ShadowCastingMode.Off; return line;
        }
        private MeshFilter Quad(Transform parent,string label,Rect rect,Material mat,float z)
        {
            var node=Node(label,Vector3.zero,parent); var f=node.gameObject.AddComponent<MeshFilter>(); f.sharedMesh=new Mesh(); owned.Add(f.sharedMesh);
            var r=node.gameObject.AddComponent<MeshRenderer>(); r.sharedMaterial=mat; r.shadowCastingMode=ShadowCastingMode.Off;
            SetQuad(f,rect.xMin,rect.xMax,rect.yMin,rect.yMax,z); return f;
        }
        private static void SetQuad(MeshFilter f,float left,float right,float bottom,float top,float z)
        { var m=f.sharedMesh; m.Clear(); m.vertices=new[]{new Vector3(left,bottom,z),new Vector3(left,top,z),new Vector3(right,top,z),new Vector3(right,bottom,z)}; m.uv=new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right}; m.triangles=new[]{0,1,2,0,2,3}; m.RecalculateBounds(); }
        private GameObject Ice(Transform parent)
        { var group=Node("Actual ice",Vector3.zero,parent); var mat=Mat(new Color(.61f,.74f,.78f)); for(int n=0;n<3;n++) Quad(group,"Cube",new Rect(-.14f+n*.09f,.16f+n%2*.08f,.085f,.075f),mat,-.025f); return group.gameObject; }
        private Color Tint(ContainerReadModel c)
            => Tint(c.Amounts,c.TotalMl);
        private Color Tint(IReadOnlyDictionary<string,decimal> amounts,decimal total)
        {
            Color sum=Color.black; if(total==0) return new Color(.5f,.7f,.7f);
            foreach(var a in amounts) { Color color=a.Key=="cream" ? new Color(.91f,.86f,.72f) : a.Key=="coffee_liqueur" ? new Color(.22f,.12f,.08f) : a.Key=="campari" ? new Color(.72f,.16f,.08f) : a.Key.Contains("whiskey") ? new Color(.66f,.37f,.13f) : a.Key=="sweet_red_vermouth" || a.Key=="angostura_bitters" ? new Color(.47f,.19f,.14f) : new Color(.65f,.72f,.63f); sum+=color*(float)(a.Value/total); } sum.a=1; return sum;
        }
        private static void FrustumFill(MeshFilter f,float bottomWidth,float topWidth,float bottom,float height,float fraction)
        {
            float width=Mathf.Pow(Mathf.Lerp(bottomWidth*bottomWidth*bottomWidth,topWidth*topWidth*topWidth,fraction),1f/3);
            float top=bottom+height*(width-bottomWidth)/(topWidth-bottomWidth); var m=f.sharedMesh; m.Clear();
            m.vertices=new[]{new Vector3(-bottomWidth,bottom,.015f),new Vector3(-width,top,.015f),new Vector3(width,top,.015f),new Vector3(bottomWidth,bottom,.015f)};
            m.triangles=new[]{0,1,2,0,2,3}; m.RecalculateBounds();
        }
        public void Refresh(BrewingSession brew,SliceInputAdapter adapter,float unused,ServiceSnapshot served=null)
        {
            if(!initialized) return;
            if(ExtendedTools && shaker==null) BuildExtendedTools();
            foreach(var d in depth) d.gameObject.SetActive(!FlatTools);
            var zero=brew==null;
            if(zero || lastRevision!=brew.Revision || lastGlass!=CocktailGlass)
            {
                lastRevision=brew?.Revision ?? -1; lastGlass=CocktailGlass;
                decimal j=brew?.Container(ContainerRole.Jigger).TotalMl ?? 0,m=brew?.Container(ContainerRole.Mixing).TotalMl ?? 0;
                ServingFillMl=brew?.Container(ContainerRole.Serving).TotalMl ?? served?.TotalMl ?? 0;
                FrustumFill(jiggerFill,.065f,.145f,.22f,.15f,(float)(j/60m));
                FrustumFill(mixingFill,.175f,.215f,.025f,.49f,(float)(m/300m));
                jiggerFill.gameObject.SetActive(j>0); mixingFill.gameObject.SetActive(m>0); servingFill.gameObject.SetActive(ServingFillMl>0);
                var outline=CocktailGlass ? new[]{new Vector3(-.17f,0,0),new Vector3(.17f,0,0),Vector3.zero,new Vector3(0,.18f,0),new Vector3(-.25f,.54f,0),new Vector3(.25f,.54f,0),new Vector3(0,.18f,0)} :
                    new[]{new Vector3(-.20f,.5f,0),new Vector3(-.18f,.01f,0),new Vector3(.18f,.01f,0),new Vector3(.20f,.5f,0)};
                servingOutline.positionCount=outline.Length; servingOutline.SetPositions(outline);
                float rimWidth=CocktailGlass ? .25f : .20f,rimHeight=CocktailGlass ? .54f : .5f;
                servingRim.SetPositions(new[]{new Vector3(-rimWidth,rimHeight,0),new Vector3(-rimWidth,rimHeight,.08f),new Vector3(rimWidth,rimHeight,.08f),new Vector3(rimWidth,rimHeight,0)});
                if(CocktailGlass) {
                    float h=Mathf.Pow((float)(ServingFillMl/180m),1f/3),top=.19f+.33f*h,w=.23f*h; var mesh=servingFill.sharedMesh; mesh.Clear();
                    mesh.vertices=new[]{new Vector3(0,.19f,.015f),new Vector3(-w,top,.015f),new Vector3(w,top,.015f)}; mesh.triangles=new[]{0,1,2}; mesh.RecalculateBounds();
                } else FrustumFill(servingFill,.16f,.18f,.035f,.44f,(float)(ServingFillMl/180m));
                mixingIce.SetActive(!zero && brew.Container(ContainerRole.Mixing).HasIce); servingIce.SetActive(brew?.Container(ContainerRole.Serving).HasIce ?? served?.HasIce ?? false); cherry.SetActive((brew?.GarnishId ?? served?.Garnish)=="cherry");
                if(ExtendedTools) RefreshExtended(brew,served);
                if(!zero) { jiggerFill.GetComponent<Renderer>().sharedMaterial.SetColor("_BaseColor",Tint(brew.Container(ContainerRole.Jigger))); mixingFill.GetComponent<Renderer>().sharedMaterial.SetColor("_BaseColor",Tint(brew.Container(ContainerRole.Mixing))); servingFill.GetComponent<Renderer>().sharedMaterial.SetColor("_BaseColor",Tint(brew.Container(ContainerRole.Serving))); }
                else if(served!=null) servingFill.GetComponent<Renderer>().sharedMaterial.SetColor("_BaseColor",Tint(served.Amounts,served.TotalMl));
            }
            string tool=adapter?.ActiveTool; bottle.localRotation=Quaternion.Euler(0,0,tool=="pour" || tool=="splash" ? -45 : 0);
            jigger.localRotation=Quaternion.Euler(0,0,tool=="transfer" || tool=="float" ? -55 : 0); mixing.localRotation=Quaternion.Euler(0,0,tool=="strain" ? -55 : 0);
            if(shaker!=null) { shaker.localRotation=Quaternion.Euler(0,0,tool=="shake" ? adapter.ShakePosition*25 : tool=="shaker_transfer" ? -55 : 0); shaker.localPosition=new Vector3(2.2f+(tool=="shake" ? adapter.ShakePosition*.12f : 0),0,0); }
            spoon.localRotation=Quaternion.Euler(0,0,adapter?.SpoonAngle ?? 0); spoon.gameObject.SetActive(tool=="stir"); hand.gameObject.SetActive(tool!=null);
            Transform grip=tool=="pour" || tool=="splash" ? bottle : tool=="transfer" || tool=="float" ? jigger : tool=="strain" ? mixing : tool=="shake" || tool=="shaker_transfer" ? shaker : spoon;
            if(tool!=null) { hand.position=grip.TransformPoint(tool=="stir" ? new Vector3(.12f,.32f,-.05f) : new Vector3(.08f,.27f,-.05f)); hand.rotation=grip.rotation; }
            stream.enabled=adapter!=null && adapter.AcceptedLiquidThisStep;
            if(stream.enabled) { float mouth=tool=="pour" || tool=="splash" ? .67f : tool=="transfer" || tool=="float" ? .39f : .55f; stream.SetPosition(0,transform.InverseTransformPoint(grip.TransformPoint(new Vector3(0,mouth,-.02f)))); Transform target=tool=="strain" || tool=="float" || tool=="shaker_transfer" ? serving : tool=="transfer" && (!ExtendedTools || adapter.Target==ContainerRole.Jigger) ? mixing : adapter.Target==ContainerRole.Jigger ? jigger : adapter.Target==ContainerRole.Mixing ? mixing : adapter.Target==ContainerRole.Shaker ? shaker : serving; stream.SetPosition(1,target.localPosition+new Vector3(0,.26f,-.02f)); }
        }
        private void BuildExtendedTools()
        {
            var iron=Mat(new Color(.64f,.68f,.65f)); shaker=Node("Shaker 300 ml",new Vector3(2.2f,0,0));
            Outline(shaker,new[]{new Vector3(-.21f,.56f,0),new Vector3(-.16f,0,0),new Vector3(.16f,0,0),new Vector3(.21f,.56f,0)},iron,false);
            shakerFill=Quad(shaker,"Actual shaker liquid",new Rect(0,0,0,0),Mat(Color.white),.015f); shakerIce=Ice(shaker);
            shakerLid=Quad(shaker,"Closed lid",new Rect(-.22f,.56f,.44f,.10f),iron,-.02f).gameObject;
            orange=Quad(serving,"Orange slice at rim",new Rect(.14f,.42f,.12f,.13f),Mat(new Color(.91f,.46f,.13f)),-.035f).gameObject;
        }
        private void RefreshExtended(BrewingSession brew,ServiceSnapshot served)
        {
            ShakerFillMl=brew?.Container(ContainerRole.Shaker).TotalMl ?? 0;
            FrustumFill(shakerFill,.14f,.19f,.025f,.49f,(float)(ShakerFillMl/300m)); shakerFill.gameObject.SetActive(ShakerFillMl>0);
            if(brew!=null) shakerFill.GetComponent<Renderer>().sharedMaterial.SetColor("_BaseColor",Tint(brew.Container(ContainerRole.Shaker)));
            shakerIce.SetActive(brew?.Container(ContainerRole.Shaker).HasIce ?? false); shakerLid.SetActive(brew?.ShakerClosed ?? false);
            orange.SetActive((brew?.GarnishId ?? served?.Garnish)=="orange_slice");
            var layers=(brew?.Freeze() ?? served)?.Layers; VisibleLayerCount=layers?.Count ?? 0;
            for(int i=0;i<VisibleLayerCount;i++) if(layerFills.Count<=i) layerFills.Add(Quad(serving,"Actual Float layer "+i,new Rect(0,0,0,0),Mat(Color.white),-.005f));
            decimal accumulated=0; for(int i=0;i<layerFills.Count;i++)
            {
                var f=layerFills[i]; f.gameObject.SetActive(i<VisibleLayerCount); if(i>=VisibleLayerCount) continue;
                decimal amount=layers[i].Amounts.Values.Sum(); float low=(float)(accumulated/180m),high=(float)((accumulated+amount)/180m); accumulated+=amount;
                float w0,w1,y0,y1;
                if(CocktailGlass) { float a=Mathf.Pow(low,1f/3),b=Mathf.Pow(high,1f/3); w0=.23f*a; w1=.23f*b; y0=.19f+.33f*a; y1=.19f+.33f*b; }
                else { w0=Mathf.Pow(Mathf.Lerp(.16f*.16f*.16f,.18f*.18f*.18f,low),1f/3); w1=Mathf.Pow(Mathf.Lerp(.16f*.16f*.16f,.18f*.18f*.18f,high),1f/3); y0=.035f+.44f*(w0-.16f)/.02f; y1=.035f+.44f*(w1-.16f)/.02f; }
                var mesh=f.sharedMesh; mesh.Clear(); mesh.vertices=new[]{new Vector3(-w0,y0,-.005f),new Vector3(-w1,y1,-.005f),new Vector3(w1,y1,-.005f),new Vector3(w0,y0,-.005f)}; mesh.triangles=new[]{0,1,2,0,2,3}; mesh.RecalculateBounds();
                f.GetComponent<Renderer>().sharedMaterial.SetColor("_BaseColor",Tint(layers[i].Amounts,amount));
            }
        }
        private void OnDestroy() { foreach(var o in owned) if(o!=null) { if(UnityEngine.Application.isPlaying) Destroy(o); else DestroyImmediate(o); } }
    }
}
