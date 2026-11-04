using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VirtualOffice.Core
{
    /// Loads each room's sub-scene additively on demand and unloads the previous one,
    /// so the whole facility is never resident in memory at once. 1단계(샘플룸 1개)에서는
    /// 실제 전환을 쓰지 않지만, 다음 단계에서 7개 방을 붙일 때 이 스텁을 그대로 확장한다.
    public class SceneLoader : MonoBehaviour
    {
        [SerializeField] private string bootstrapSceneName = "Bootstrap";

        private readonly HashSet<string> _loadedScenes = new HashSet<string>();

        public void LoadRoom(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName) || _loadedScenes.Contains(sceneName)) return;

            SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
            _loadedScenes.Add(sceneName);
        }

        public void UnloadRoom(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName) || !_loadedScenes.Contains(sceneName)) return;
            if (sceneName == bootstrapSceneName) return;

            SceneManager.UnloadSceneAsync(sceneName);
            _loadedScenes.Remove(sceneName);
        }
    }
}
