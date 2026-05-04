using UnityEngine;
using UnityEngine.SceneManagement;

namespace MediProfen.Core
{
    public class SceneLoader : MonoBehaviour
    {
        public void LoadScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogWarning("Scene name is empty.");
                return;
            }

            SceneManager.LoadSceneAsync(sceneName);
        }
    }
}
