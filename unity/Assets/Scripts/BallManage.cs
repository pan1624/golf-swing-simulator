using UnityEngine;
using TMPro;

public class GolfBallSwitcher : MonoBehaviour
{
    public GameObject[] golfBalls; // 所有高爾夫球的 Prefab
    public TMP_Dropdown ballDropdown; // UI 下拉選單
    public TMP_Text ballInfoText; // 顯示當前高爾夫球資訊

    private GameObject currentBallInstance; // 當前選擇的高爾夫球實例
    private int currentBallType = 0; // 球類型

    void Start()
    {
        // **檢查設定**
        if (golfBalls == null || golfBalls.Length == 0)
        {
            Debug.LogError("GolfBallSwitcher: golfBalls 陣列為空，請在 Inspector 設定高爾夫球！");
            return;
        }

        if (ballDropdown == null)
        {
            Debug.LogError("GolfBallSwitcher: BallDropdown 未設定！");
            return;
        }

        // **初始化 Dropdown**
        SetupDropdown();

        // **顯示第一顆高爾夫球**
        SwitchBall(0);
    }

    // **設定 UI 下拉選單**
    void SetupDropdown()
    {
        ballDropdown.options.Clear(); // **清空舊選項**
        ballDropdown.onValueChanged.AddListener(SwitchBall); // **綁定選擇事件**

        // **將所有高爾夫球加入選單**
        foreach (GameObject ball in golfBalls)
        {
            ballDropdown.options.Add(new TMP_Dropdown.OptionData(ball.name));
        }

        ballDropdown.value = 0; // **設定預設值**
        ballDropdown.RefreshShownValue(); // **更新 UI**
    }

    // **切換高爾夫球**
    public void SwitchBall(int index)
    {
        if (index < 0 || index >= golfBalls.Length) return;

        // **刪除舊球**
        if (currentBallInstance != null)
        {
            Destroy(currentBallInstance);
        }

        // **生成新球並放置於場地上**
        if (golfBalls[index] != null)
        {
            currentBallInstance = Instantiate(golfBalls[index], Vector3.zero, Quaternion.identity);
        }

        // **更新 UI 顯示目前高爾夫球資訊**
        string ballName = golfBalls[index].name.ToLower();

        if (ballName.Contains("Practice"))
        {
            currentBallType = 1;
        }
        else if (ballName.Contains("taylormade"))
        {
            currentBallType = 2;
        }
        else if (ballName.Contains("prov1x"))
        {
            currentBallType = 3;
        }
        else if (ballName.Contains("rzn"))
        {
            currentBallType = 4;
        }
        else
        {
            currentBallType = 0;
        }

        // **顯示球的詳細資訊**
        if (ballInfoText != null)
        {
            ballInfoText.text = $"當前高爾夫球: {golfBalls[index].name}\n類型: {currentBallType}";
        }

        Debug.Log(" 已選擇高爾夫球: " + golfBalls[index].name);
    }

    public int GetCurrentBallType()
    {
        return currentBallType;
    }
}
