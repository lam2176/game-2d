using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Màn hình bắt đầu: nút Bắt đầu vào màn chơi, nút Thoát.</summary>
public class ZodiacStartMenu : MonoBehaviour
{
    public string levelSceneName = "Zodiac_Level01";

    public void Play() => SceneManager.LoadScene(levelSceneName);

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
