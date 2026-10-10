using System.IO;
using System.Text;
using UnityEditor;
using Surlhwa.ContentIO;
namespace Surlhwa.Editor
{
    public static class ContentBundleGenerator
    {
        [InitializeOnLoadMethod]
        private static void OnEditorLoad() => EditorApplication.delayCall += Sync;
        [MenuItem("Surlhwa/Validate and regenerate content bundle")]
        public static void Sync()
        {
            string source = File.ReadAllText(Path.GetFullPath("../Content/spatial-proof.json"), Encoding.UTF8);
            SpatialProofLoader.Load(source);
            const string destination = "Assets/GeneratedContent/spatial-proof.json";
            Directory.CreateDirectory("Assets/GeneratedContent");
            if (!File.Exists(destination) || File.ReadAllText(destination, Encoding.UTF8) != source)
                File.WriteAllText(destination, source, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceSynchronousImport);
        }
    }
}
