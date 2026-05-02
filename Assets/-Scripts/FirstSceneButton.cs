using UnityEngine;
using UnityEngine.SceneManagement;

public class FirstSceneButton : MonoBehaviour
{
    [SerializeField] private string targetSceneName = "BackGroundScene";

    public void StartGame()
    {
        SceneManager.LoadScene(targetSceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
