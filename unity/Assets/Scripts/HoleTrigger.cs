
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class HoleTrigger : MonoBehaviour
{
    [Header("🎯 允許觸發的洞口 (在 Inspector 拖入)")]
    public List<GameObject> allowedHoles; // 可觸發的洞口

    [Header("⚽ 拖入高爾夫球 (在 Inspector 拖入)")]
    public GameObject golfBall;

    [Header("🧍‍♂️ 拖入玩家模型 (在 Inspector 拖入)")]
    public GameObject playerObject;

    [Header("🔄 每個洞口對應的球移動目標坐標 (手動輸入)")]
    public List<Vector3> ballTargetPositions;

    [Header("🧍‍♂️ 每個洞口對應的玩家移動目標坐標 (手動輸入)")]
    public List<Vector3> playerTargetPositions;

    [Header("📏 UI 顯示高爾夫球與當前目標洞的距離")]
    public Text distanceText;

    [Header("🧍‍♂️ 傳送玩家到球後用")]
    public Transform player;                   // 玩家 Transform（要拖 DummyModel_Male）
    public Rigidbody rb;                       // 球的 Rigidbody（為了計算方向）
    public float playerDistanceBehindBall = 2f; // 玩家離球距離
    public Camera customCamera;               // 攝影機（可以讓玩家面向球）


    private Dictionary<GameObject, Vector3> holeToBallTarget = new Dictionary<GameObject, Vector3>();
    private Dictionary<GameObject, Vector3> holeToPlayerTarget = new Dictionary<GameObject, Vector3>();

    private int currentHoleIndex = 0; // 追蹤目前要打的洞口
    private GameObject currentTargetHole;
    private bool isProcessing = false; // 避免重複觸發
    float distance = 0;

    // 🚀 新增部分：增加 LineRenderer 來顯示洞口的位置
    private LineRenderer holeLineRenderer;

    private void Start()
    {
        Debug.Log($"📌 總共有 {allowedHoles.Count} 個洞");

       

        for (int i = 0; i < allowedHoles.Count; i++)
        {
            holeToBallTarget[allowedHoles[i]] = ballTargetPositions[i];
            holeToPlayerTarget[allowedHoles[i]] = playerTargetPositions[i];
        }

        if (allowedHoles.Count > 0)
        {
            currentTargetHole = allowedHoles[currentHoleIndex];
            Debug.Log($"🎯 當前目標洞: {currentTargetHole.name}");
        }


        // 🚀 新增部分：初始化 LineRenderer
        holeLineRenderer = gameObject.AddComponent<LineRenderer>();
        SetupLineRenderer(holeLineRenderer, Color.red); // 設定為綠色



        UpdateDistanceUI();
    }

    private bool forceStanding = false;

    private void Update()
    {
        
        ResetPlayerSkeleton(playerObject);
      

        if (golfBall == null || currentTargetHole == null)
        {
            Debug.LogWarning("⚠️ [Update] 無法執行，golfBall 或 currentTargetHole 為 null！");
            return;
        }
      //distance = Vector3.Distance(golfBall.transform.position, currentTargetHole.transform.position);

        // 🔥 只允許當前的 `currentTargetHole` 執行 `Update()`，其他洞口不執行
        if (this.gameObject == currentTargetHole)
        {
            UpdateDistanceUI();
        }


    }


    private void UpdateDistanceUI()
    {
        if (golfBall != null && currentTargetHole != null && distanceText != null)
        {
            distance = Vector3.Distance(golfBall.transform.position, currentTargetHole.transform.position);
            distanceText.text = $"與當前洞 {currentHoleIndex} 的距離: {distance:F1} 公尺";
         //   Debug.Log($"🔍 [Update] 來自物件: {this.gameObject.name}, currentHoleIndex={currentHoleIndex}, currentTargetHole={(currentTargetHole != null ? currentTargetHole.name : "null")}");
        }

        // 🚀 新增部分：更新箭頭顯示的位置
        if (currentTargetHole != null)
        {
            Vector3 holePosition = currentTargetHole.transform.position;
            holeLineRenderer.SetPosition(0, holePosition + Vector3.up * 5f); // 起點在洞口
            holeLineRenderer.SetPosition(1, holePosition + Vector3.up * 50f); // 終點在洞口上方 50m
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == golfBall && currentTargetHole != null && !isProcessing)
        {
            Debug.Log($"🏌 高爾夫球進入洞: {currentTargetHole.name}");
            isProcessing = true; // 避免多次觸發

            Rigidbody rb = golfBall.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Debug.Log("⛔ 停止球的移動");
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            StartCoroutine(SinkAndMove());
        }
    }

    private void SetupLineRenderer(LineRenderer lr, Color color)
    {
        // 🚀 新增部分：設定 LineRenderer
        lr.startWidth = 0.1f;
        lr.endWidth = 0.1f;
        lr.positionCount = 2;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = color;
        lr.endColor = color;
    }

    private void ResetPlayerSkeleton(GameObject playerObj)
    {
        // 嘗試找到 playerObj 底下的 hips
        Transform hips = FindChildRecursive(playerObj.transform, "Hips");

        if (hips != null)
        {
            Vector3 hipsEuler = hips.eulerAngles;
            hips.eulerAngles = new Vector3(0f, hipsEuler.y, 0f); // 只保留 Y 軸方向
            Debug.Log("✅ 骨架 hips 已重新站直！");
        }
        else
        {
            Debug.LogWarning("⚠️ 沒找到 hips 骨架！");
        }
    }

    // 輔助函數（用來在子物件裡找名字是 "Hips" 的 Transform）
    private Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;
            Transform found = FindChildRecursive(child, childName);
            if (found != null)
                return found;
        }
        return null;
    }


   

    private IEnumerator SinkAndMove()
    {
        Debug.Log($"🔄 球開始沈入洞: {currentTargetHole.name}");

        float duration = 1.0f;
        Vector3 startPos = golfBall.transform.position;
        Vector3 endPos = startPos + Vector3.down * 0.3f;

        float elapsedTime = 0;
        while (elapsedTime < duration)
        {
            golfBall.transform.position = Vector3.Lerp(startPos, endPos, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(0.5f);

        if (holeToBallTarget.ContainsKey(currentTargetHole))
        {
            golfBall.transform.position = holeToBallTarget[currentTargetHole];
            golfBall.GetComponent<Rigidbody>().isKinematic = false;
        }

        currentHoleIndex++;
        currentTargetHole = allowedHoles[currentHoleIndex];

        yield return new WaitForSeconds(0.3f);

        if (player != null)
        {
            Vector3 moveDirection = (golfBall.transform.position - currentTargetHole.transform.position).normalized;
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



        Vector3 holePosition = currentTargetHole.transform.position;
        holeLineRenderer.SetPosition(0, holePosition + Vector3.up * 0); // 起點在洞口
        holeLineRenderer.SetPosition(1, holePosition + Vector3.up * 0f); // 終點在洞口上方 50m



        Debug.Log($"🔄 變更前 currentHoleIndex: {currentHoleIndex}");
        currentHoleIndex++;
        if (currentHoleIndex < allowedHoles.Count)
        {
            currentTargetHole = allowedHoles[currentHoleIndex];

            



            Debug.Log($"🎯 成功切換到新目標洞: {currentTargetHole.name}");

            // ✅ 先更新全域 UI
            UpdateDistanceUI();

            // ✅ 手動通知新的 `HoleTrigger` 讓它更新 `UpdateDistanceUI()`，確保 UI 正確顯示
            HoleTrigger newHoleTrigger = currentTargetHole.GetComponent<HoleTrigger>();
            if (newHoleTrigger != null)
            {
                Debug.Log($"🚀 手動觸發新目標洞 ({currentTargetHole.name}) 執行 `UpdateDistanceUI()`");

               
                newHoleTrigger.currentHoleIndex = currentHoleIndex;
                newHoleTrigger.currentTargetHole = currentTargetHole;
                newHoleTrigger.UpdateDistanceUI();
                newHoleTrigger.Update();
            }
        }

        else
        {
            Debug.Log("🎉 所有洞都完成了！");
            currentTargetHole = null;
        }

        isProcessing = false; // 允許下一次觸發
    }
}
