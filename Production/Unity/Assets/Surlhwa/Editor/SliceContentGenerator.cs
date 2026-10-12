using System.IO;
using System.Text;
using Surlhwa.ContentIO;
using UnityEditor;
using UnityEngine;

namespace Surlhwa.Editor
{
    public static class SliceContentGenerator
    {
        public const string Destination="Assets/GeneratedContent/slice-content.json";
        [MenuItem("Surlhwa/Validate and regenerate Slice content")]
        public static void Sync()
        {
            string bundle=SliceContentLoader.BundleFromDirectory(Path.GetFullPath("../Content"));
            Directory.CreateDirectory(Path.GetDirectoryName(Destination));
            if(!File.Exists(Destination) || File.ReadAllText(Destination,Encoding.UTF8)!=bundle)
                File.WriteAllText(Destination,bundle,new UTF8Encoding(false));
            AssetDatabase.ImportAsset(Destination,ImportAssetOptions.ForceSynchronousImport);
            if(AssetDatabase.LoadAssetAtPath<TextAsset>(Destination)==null) throw new System.InvalidOperationException("Slice content import failed.");
        }
    }
}
