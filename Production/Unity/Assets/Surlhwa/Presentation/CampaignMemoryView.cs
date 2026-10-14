using System.Linq;
using UnityEngine;
using Surlhwa.Domain;
namespace Surlhwa.Presentation
{
    // The existing empty stool remains the chair; this view only reads the earned receipt.
    public sealed class CampaignMemoryView : MonoBehaviour
    {
        private GameObject star; private Material material; private Mesh mesh;
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
            star.transform.position=chair+new Vector3(.32f,.76f,-.06f); star.transform.rotation=Quaternion.Euler(28,0,0);
            float pulse=Glowing ? .94f+.06f*Mathf.Sin(Time.unscaledTime*1.4f) : 1;
            material.SetColor("_BaseColor",(Glowing ? new Color(1,.87f,.48f) : new Color(.53f,.51f,.42f))*pulse);
        }
        private void OnDestroy() { if(material!=null) Destroy(material); if(mesh!=null) Destroy(mesh); }
    }
}
