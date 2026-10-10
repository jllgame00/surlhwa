using UnityEngine;
using UnityEngine.SceneManagement;
namespace Surlhwa.Presentation
{
    public sealed class Bootstrap : MonoBehaviour
    {
        private void Start() => SceneManager.LoadScene("Bar", LoadSceneMode.Single);
    }
}
