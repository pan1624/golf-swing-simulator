using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // 開始遊戲
    public void StartGame()
    {
        SceneManager.LoadScene("Gamescene"); // 確保 "GameScene" 是您的遊戲場景名稱
    }

    // 退出遊戲
    public void QuitGame()
    {
        Application.Quit(); // 在遊戲打包後才能生效
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // 在 Unity 編輯器內測試時使用
#endif
    }
}

//control shift + b