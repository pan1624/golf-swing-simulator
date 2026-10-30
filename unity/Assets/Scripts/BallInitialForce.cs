using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEditorInternal;
using Unity.Burst.CompilerServices;

public class BallInitialForce : MonoBehaviour
{
    private Rigidbody rb;

    public Transform player; // 玩家 Transform
    private LineRenderer ballLineRenderer;  // 球速度向量
    private LineRenderer playerLineRenderer; // 玩家方向向量

    private int shotCount; // 記錄打了幾杆
    public Text shotCounterText; // UI 文字顯示桿數
    public Text speedText; // UI 文字顯示速度
    public Text distanceText; //  這是 **原本的** UI，提供距離資訊（需要手動拖曳）
    public Text distancePopupText; //  這是 **新的** UI，球停止時才會顯示（需要手動拖曳）

    public float minSpeedThreshold = 0.2f; // 最小速度閾值
    private bool ballStopped = false; // 確保只計算一次球停止的狀態
    public bool hasLaunched = false; // **只有當球發射後，才會判斷它是否停止**
    public Camera customCamera; //  讓你手動設定自創的攝影機

    public float playerDistanceBehindBall = 15f; //  玩家與球之間的距離

    public GolfClubSwitcher clubSwitcher; // 指定 GolfClubSwitcher 的引用
    public GolfBallSwitcher BallSwitcher; // 指定BallSwitcher 的引用
    private float launchTime; //  記錄球發射的時間


    // **高爾夫球數據**
    private float[] frictionValues = { 0.3f, 0.4f, 0.7f, 0.4f }; // 摩擦力
    private float[] restitutionValues = { 0.5f, 0.75f, 0.65f, 0.7f }; // 恢復係數
    private float[] spinResistanceValues = { 0.01f, 0.02f, 0.05f, 0.02f }; // 旋轉阻力

    public SocketReceiver Socketreceiver;







    void Start()
    {

        PlayerPrefs.SetInt("ShotCount", 0);
        rb = GetComponent<Rigidbody>();

        Physics.gravity = new Vector3(0, -30f, 0); // 預設為地球重力
        if (rb == null)
        {
            Debug.LogError("Rigidbody not found on the object.");
        }

        // 🏌️ **判斷是否需要重置 `shotCount`**
        if (PlayerPrefs.GetInt("GameStarted", 0) == 0) // **第一次進入遊戲**
        {
            shotCount = 0;
            PlayerPrefs.Save();
            // ResetGame(); // 重置遊戲
        }
        else
        {
            shotCount = PlayerPrefs.GetInt("ShotCount", 0); // 讀取已存的擊球數
        }

        // 🔴 初始化球速度向量的 LineRenderer
        ballLineRenderer = gameObject.AddComponent<LineRenderer>();
        SetupLineRenderer(ballLineRenderer, Color.red);

        Debug.Log("🚀 BallInitialForce Start() 被執行！");
        if (player == null)
        {
            Debug.LogError("❌ 錯誤: Player transform 未被指定！請在 Inspector 手動分配。");
            return;
        }

        // 🟢 初始化玩家朝向向量的 LineRenderer
        if (player != null)
        {
            playerLineRenderer = player.gameObject.AddComponent<LineRenderer>();
            SetupLineRenderer(playerLineRenderer, Color.green);
        }
        else
        {
            Debug.LogError("Player transform is not assigned!");
        }

        // **隱藏距離 UI**
        if (distancePopupText != null)
        {
            distancePopupText.gameObject.SetActive(false);
        }

        // **經過 2 秒自動發射球**
        // StartCoroutine(LaunchBall());
    }

    private void SetupLineRenderer(LineRenderer lr, Color color)
    {
        lr.startWidth = 0.1f;
        lr.endWidth = 0.1f;
        lr.positionCount = 2;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = color;
        lr.endColor = color;
    }

    public IEnumerator LaunchBall(float force)
    {
        hasLaunched = true; // **球真正被打出去，才開始判斷是否停止**
        Debug.Log("等待 1 秒後發射球...");
        yield return new WaitForSeconds(1f); // ✅ **等待 1 秒後自動發射**

        int clubType = clubSwitcher.GetCurrentClubType();
        Debug.Log($"Golf club type: {clubType}");

        // 🔥 計算 `launchDirection`
        Vector3 launchDirection = player.forward * force;
        if (clubType == 1) launchDirection.y = 10;
        else if (clubType == 2) launchDirection.y = 12;
        else if (clubType == 3) launchDirection.y = 15;
        else if (clubType == 4) launchDirection.y = 18;
        else if (clubType == 5) launchDirection.y = 0;
        else if (clubType == 6) launchDirection.y = 20;  ///Club choose
        else if (clubType == 7) launchDirection.y = 30;  ///Club choose



        int BallType = BallSwitcher.GetCurrentBallType();


        // 設定初速度
        rb.AddForce(launchDirection, ForceMode.Impulse);
        rb.angularDamping = spinResistanceValues[BallType]; // 設置旋轉阻力

        // 設定物理材質
        PhysicsMaterial ballMaterial = new PhysicsMaterial();
        ballMaterial.dynamicFriction = frictionValues[BallType];
        ballMaterial.staticFriction = frictionValues[BallType];
        ballMaterial.bounciness = restitutionValues[BallType];
        ballMaterial.frictionCombine = PhysicsMaterialCombine.Average;
        ballMaterial.bounceCombine = PhysicsMaterialCombine.Average;

        Collider ballCollider = GetComponent<Collider>();
        if (ballCollider != null)
        {
            ballCollider.material = ballMaterial;
        }



        ballStopped = false; // **重置球停止狀態**
        shotCount++; // 增加桿數
        PlayerPrefs.SetInt("ShotCount", shotCount); //  **保存桿數**
        PlayerPrefs.Save();

        //  **記錄發射時間**
        launchTime = Time.time;
        UpdateShotCounter(); // 更新 UI 顯示

        // **隱藏距離 UI**
        if (distancePopupText != null)
        {
            distancePopupText.gameObject.SetActive(false);
        }

        Debug.Log("球已發射！速度: " + launchDirection);
    }

    void Update()
    {
        if (rb != null)
        {
            // 🔴 設定第一條線（紅色）表示高爾夫球的速度向量
            ballLineRenderer.SetPosition(0, transform.position);
            ballLineRenderer.SetPosition(1, transform.position + rb.linearVelocity);
        }

        if (player != null && playerLineRenderer != null)
        {
            // 🟢 設定第二條線（綠色）表示玩家的朝向方向
            playerLineRenderer.SetPosition(0, player.position);
            playerLineRenderer.SetPosition(1, player.position + player.forward * 25);
        }

        // 更新速度顯示
        speedUpdate();

        // ⚠️ **只有當球發射過後 (`hasLaunched == true`)，才會檢查它是否停止**
        if (hasLaunched && rb != null && rb.linearVelocity.magnitude < minSpeedThreshold && !ballStopped && (Time.time - launchTime > 1.0f))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            ballStopped = true; // 確保只執行一次
            Debug.Log("球速度過慢，已停止。");

            // **移動玩家到球的正後方**
            MovePlayerBehindBall();



            // **顯示新的距離 UI**
            ShowDistancePopup();


        }

        // 按下 "N" 鍵重新開始下一桿
        if (ballStopped && Input.GetKeyDown(KeyCode.N))
        {
            Socketreceiver.canstart = false;
            hasLaunched = false; // **重置發射狀態**
            distancePopupText.text = "請移動到綠色區域發球...";



        }
    }

    public void Checkballstop()
    {
        if(ballStopped)
        {
            Socketreceiver.canstart = false;
            hasLaunched = false; // **重置發射狀態**
            distancePopupText.text = "請移動到綠色區域發球...";

        }

    }



    private void MovePlayerBehindBall()
    {
        if (player != null)
        {
            Vector3 moveDirection = -rb.linearVelocity.normalized;
            if (moveDirection == Vector3.zero)
            {
                moveDirection = -player.forward;
            }

            float newDistance = playerDistanceBehindBall + 2f;
            Vector3 newPosition = rb.position + moveDirection * newDistance;
            player.position = newPosition;

            // ✅ 讓角色面對球
            player.LookAt(rb.position);

            // ✅ 攝影機調整
            if (customCamera != null)
            {
                customCamera.transform.LookAt(rb.position + Vector3.down * 0.12f);
            }

            Debug.Log("✅ 玩家已到球後並站直");
        }
    }



    private void UpdateShotCounter()
    {
        if (shotCounterText != null)
        {
            shotCounterText.text = "Shots: " + shotCount;
        }
    }

    private void speedUpdate()
    {
        if (speedText != null)
        {
            speedText.text = "Speed: " + rb.linearVelocity.magnitude.ToString("F2") + "m/s";
        }
    }

    private void ShowDistancePopup()
    {
        if (distanceText != null && distancePopupText != null)
        {
            string originalText = distanceText.text; // 取得原本的文字
            if (originalText.Length > 8)
            {
                distancePopupText.text = originalText.Substring(8); // 刪除前8個字
            }
            else
            {
                distancePopupText.text = originalText; // 避免錯誤
            }
            distancePopupText.text += " 接觸灰色區域開啓下一杆";

            distancePopupText.gameObject.SetActive(true);
        }
    }
}
