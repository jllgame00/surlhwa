using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
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
    public sealed class ContentBundleBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report)
        {
            try { ContentBundleGenerator.Sync(); }
            catch (System.Exception error) {
                throw new BuildFailedException("Authored production content is invalid: " + error.Message);
            }
        }
    }
}
