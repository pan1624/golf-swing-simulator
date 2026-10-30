using UnityEngine;
using TMPro;

public class GolfClubSwitcher : MonoBehaviour
{
    public GameObject[] golfClubs; // 所有球桿的 Prefab
    public TMP_Dropdown clubDropdown; // UI 下拉選單
    public TMP_Text clubNameText; // 顯示當前球桿名稱
    public GameObject characterModel; // 角色模型 (在 Inspector 拖入)

    private Transform golfClubHolder; // 角色的手部骨骼
    private GameObject currentClubInstance; // 當前選擇的球桿實例

    private int currentClubType = 0; // 球桿類型 (1: 一般, 2: 強力擊球)
     
   
    
    void Start()
    {
        // **檢查設定**
        if (golfClubs == null || golfClubs.Length == 0)
        {
            Debug.LogError("GolfClubSwitcher: golfClubs 陣列為空，請在 Inspector 設定球桿！");
            return;
        }

        if (clubDropdown == null)
        {
            Debug.LogError("GolfClubSwitcher: ClubDropdown 未設定！");
            return;
        }

        if (characterModel == null)
        {
            Debug.LogError("GolfClubSwitcher: 未設定角色模型！");
            return;
        }

        // **尋找角色的右手骨骼**
        FindGolfClubHolder();

        // **初始化 Dropdown**
        SetupDropdown();

        // **顯示第一支球桿**
        SwitchClub(0);
    }

    // **自動尋找角色右手骨骼**
    void FindGolfClubHolder()
    {
        // 嘗試直接尋找 (確保這個名稱符合你的 `Hierarchy`)
        golfClubHolder = characterModel.transform.Find("Rig/B-root/B-spine/B-chest/B-shoulder.L/B-upperArm.L/B-forearm.L/B-hand.L");

        // **如果找不到，使用遍歷方式**
        if (golfClubHolder == null)
        {
            Debug.LogWarning("嘗試自動匹配骨骼名稱...");
            foreach (Transform bone in characterModel.GetComponentsInChildren<Transform>())
            {
                if (bone.name.ToLower().Contains("hand.l")) // **自動匹配 `hand.r`**
                {
                    golfClubHolder = bone;
                    Debug.Log("✅ 自動找到右手骨骼：" + bone.name);
                    break;
                }
            }
        }

        // **如果還是找不到，報錯**
        if (golfClubHolder == null)
        {
            Debug.LogError("❌ GolfClubSwitcher: 無法找到角色的右手骨骼，請確認骨架名稱是否正確！");
        }
    }

    // **設定 UI 下拉選單**
    void SetupDropdown()
    {
        clubDropdown.options.Clear(); // **清空舊選項**
        clubDropdown.onValueChanged.AddListener(SwitchClub); // **綁定選擇事件**

        // **將所有球桿加入選單**
        foreach (GameObject club in golfClubs)
        {
            clubDropdown.options.Add(new TMP_Dropdown.OptionData(club.name));
        }

        clubDropdown.value = 0; // **設定預設值**
        clubDropdown.RefreshShownValue(); // **更新 UI**
    }

    // **切換球桿**
    public void SwitchClub(int index)
    {
        if (index < 0 || index >= golfClubs.Length) return;

        // **確保手部骨骼已找到**
        if (golfClubHolder == null)
        {
            Debug.LogError("GolfClubSwitcher: golfClubHolder 未設定，無法放置球桿！");
            return;
        }

        // **刪除舊球桿**
        if (currentClubInstance != null)
        {
            Destroy(currentClubInstance);
        }

        // **生成新球桿並附加到角色的手**
        if (golfClubs[index] != null)
        {
            currentClubInstance = Instantiate(golfClubs[index], golfClubHolder.position, golfClubHolder.rotation);
            currentClubInstance.transform.SetParent(golfClubHolder); // **設為角色右手的子物件**
            currentClubInstance.transform.localPosition = Vector3.zero; // **重設位置**
            currentClubInstance.transform.localRotation = Quaternion.Euler(-90f, 90f, 0f);// **重設旋轉**
            currentClubInstance.transform.localScale = Vector3.one * 1; // **放大球桿，確保可見**
        }

        // **更新 UI 顯示目前球桿名稱**
        if (clubNameText != null)
        {
            clubNameText.text = "當前球桿: " + golfClubs[index].name;
        }

        Debug.Log("✅ 已選擇球桿：" + golfClubs[index].name);
        Debug.Log("球桿生成於：" + currentClubInstance.transform.position);

        string clubName = golfClubs[index].name.ToLower();

        if (clubName.Contains("7iron"))
        {
            currentClubType = 1; 
        }
        else if (clubName.Contains("8iron"))
        {
            currentClubType = 2; 
        }
        else if (clubName.Contains("9iron"))
        {
            currentClubType = 3; 
        }
        else if (clubName==("Driver"))
        {
            currentClubType = 4; 
        }
        else if (clubName.Contains("putter"))
        {
            currentClubType = 5; 
        }
        else if (clubName==("pw"))
        {
            currentClubType = 6; // **Pitching Wedge**
        }
        else if (clubName==("sw"))
        {
            currentClubType = 7; // **Sand Wedge**
        }
        else
        {
            currentClubType = 0; // **未知類型**
        }

        // **更新 UI 顯示當前球桿名稱**
        if (clubNameText != null)
        {
            clubNameText.text = $"當前球桿: {golfClubs[index].name} | 類型: {currentClubType}";
        }


    }


    public int GetCurrentClubType()
    {
        return currentClubType;
    }

}
