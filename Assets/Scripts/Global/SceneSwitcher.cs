// SceneSwitcher.cs (No changes needed)
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    public void LoadSpecificScene(int sceneBuildIndex)
    {
        // This function will be called by the buttons.
        if (sceneBuildIndex >= 0 && sceneBuildIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(sceneBuildIndex);
        }
        else
        {
            Debug.LogError("Scene Load Failed...");
        }
    }
}