using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Unity.Content;
using UnityEngine;



public class SocketReceiver : MonoBehaviour
{
    public Transform[] bodyJoints;  // 骨架節點


    public Rigidbody golfBall;      // 高爾夫球的 Rigidbody

    public float rotationSpeed = 50f; // 旋轉速度

    public Transform rotationCenter;

    private TcpListener server;
    private TcpClient client;
    private NetworkStream stream;
    private bool isClientConnected = false; // 標誌是否有客戶端連接
    private static bool serverStarted = false; // 標誌伺服器是否已啟動

    public bool forceApplied = false; // 檢查是否已施加力


    private Vector3 previousJointPositions; // 上一幀的關節位置[3]
    private Vector3 previousJointPositions1; // 上一幀的關節位置[5]
    private float previousTime = 0f;
    public Transform player;


    private float lastCameraChangeTime = 0f; // 上次切換攝影機的時間

    public BallInitialForce ballInitialForce; // 指定 BallInitialForce 的引用    
    public CameraSwitcher cameraSwitcher; // 指定 CameraSwitcher 的引用
    public RotateAroundBall rotateAroundBall; // 指定 RotateAroundBall 的引用    


    public GolfClubSwitcher clubSwitcher; // 指定 GolfClubSwitcher 的引用
    public bool canstart = true;
    public bool canchangecam = false;
    private bool hasSwung = false;




    void Start()
    {


        if (bodyJoints == null || bodyJoints.Length == 0)
        {
            Debug.LogError("Body Joints array is not set or empty. Please assign bone Transforms in the Inspector.");
            return;
        }


        if (!serverStarted)
        {
            try
            {
                server = new TcpListener(IPAddress.Parse("127.0.0.1"), 6001);
                server.Start();
                serverStarted = true;
                Debug.Log("Server started. Waiting for connection...");
                server.BeginAcceptTcpClient(OnClientConnected, null);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to start the server: {e.Message}");


            }
        }


    }



    void Update()
    {


        HandleInput();



        if (!isClientConnected)
        {
            Debug.Log("No client connected yet.");
            return;
        }

        if (stream != null)
        {
            Debug.Log("Stream is valid...");
            try
            {
                if (stream.DataAvailable)
                {
                    // Debug.Log("Data available in stream...");
                    byte[] buffer = new byte[2048];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);

                    if (bytesRead > 0)
                    {
                        string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        //   Debug.Log($"Raw Data Received:\n{data}");
                        StartCoroutine(ProcessData(data));
                    }
                }
                else
                {
                    Debug.Log("No data available in stream yet.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error reading stream: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("Stream is null. Unable to process data.");
        }
    }

    void OnClientConnected(IAsyncResult result)
    {
        try
        {
            client = server.EndAcceptTcpClient(result);
            stream = client.GetStream();
            isClientConnected = true; // 更新連接狀態
            Debug.Log("Client connected!");

            // 繼續接受新的客戶端連接
            server.BeginAcceptTcpClient(OnClientConnected, null);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error while accepting client: {e.Message}");
        }
    }



    void ResetPlayerRotation()
    {
        Transform hips = bodyJoints[0];
        Vector3 euler = hips.eulerAngles;
        hips.eulerAngles = new Vector3(euler.x, euler.y - 90f, euler.z);
        hasSwung = false;
        Debug.Log("🔄 hips 已回轉 -90 度，當前角度：" + hips.eulerAngles.y);
    }




    public IEnumerator ProcessData(string data)
    {

        

        if (data.Contains("start_game"))
        {
            Debug.Log("Received 'start_game'.");
            canstart = true;
            
            if (!hasSwung)
            {
                hasSwung = true;

                // ✅ 關閉 Animator 避免動畫覆蓋旋轉（可選）
                Animator animator = player.GetComponent<Animator>();
                if (animator != null) animator.enabled = false;

                // ✅ 在目前角度上 +90 度（直接設定）
                Transform hips = bodyJoints[0];
                Vector3 hipsEuler = hips.eulerAngles;
                hips.eulerAngles = new Vector3(hipsEuler.x, hipsEuler.y + 90f, hipsEuler.z);

                Debug.Log("✅ hips 已轉 90 度，新角度：" + hips.eulerAngles.y);

                Debug.Log("Player name: " + player.name);
                Debug.DrawRay(player.position, player.forward * 2f, Color.red, 5f);
                Debug.Log("✅ 原地轉身完成！角度：" + player.eulerAngles.y + "，位置：" + player.position);
            }
            

        }




        if (data.Contains("camera_change"))
        {
            Debug.Log(" Received 'camera_change'. ");



            if (Time.time - lastCameraChangeTime >= 2f)
            {
                lastCameraChangeTime = Time.time; // 更新上次切換時間
                canchangecam = true;

                // 切換攝影機
                cameraSwitcher.mainCamera.enabled = !cameraSwitcher.mainCamera.enabled;
                cameraSwitcher.characterCamera.enabled = !cameraSwitcher.characterCamera.enabled;
            }


        }



        if (data.Contains("move_right"))
        {
            Debug.Log(" Received 'move_right'. ");
            rotateAroundBall.Turnright();

        }

        if (data.Contains("move_left"))
        {
            Debug.Log(" Received 'move_left'. ");
            rotateAroundBall.Turnleft();
        }


        if (data.Contains("next"))
        {
            Debug.Log(" Received 'next'. ");
            ballInitialForce.Checkballstop();
        }





        // 確保 bodyJoints 陣列已經被設置，否則記錄錯誤並返回
        if (bodyJoints == null || bodyJoints.Length == 0)
        {
            Debug.LogError("Body Joints array is not set or empty.");
            yield break;
        }

        // 解析從 TCP 傳輸過來的數據，以換行符進行拆分
        string[] lines = data.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        lines = lines.Where(line => !line.Contains("start_game")).ToArray();
        lines = lines.Where(line => !line.Contains("camera_change")).ToArray();
        lines = lines.Where(line => !line.Contains("move_right")).ToArray();
        lines = lines.Where(line => !line.Contains("move_left")).ToArray();
        // 延遲 0.2 秒
        yield return new WaitForSeconds(0.3f);




        // 確保接收到的關節數據與預期的數量相符
        if (lines.Length != bodyJoints.Length)
        {
            Debug.LogWarning($"Received {lines.Length} joints, but expected {bodyJoints.Length}.");
            yield break;
        }

        // 初始化上一幀的關節位置，如果還未初始化
        if (previousJointPositions == null)
        {
            previousJointPositions = Vector3.zero;
        }

        if (previousJointPositions1 == null)
        {
            previousJointPositions1 = Vector3.zero;
        }

        // Step 1: 更新髖關節（hips）的世界坐標
        if (bodyJoints[0] == null)
        {
            Debug.LogError("Hips joint (bodyJoints[0]) is not assigned.");
            yield break;
        }

        // 如果高爾夫球存在並且還未施加力，則嘗試施加推力
        if (golfBall != null && !forceApplied && canstart == true)
        {
            
           /* if (ballInitialForce.hasLaunched == false)
                StartCoroutine(ballInitialForce.LaunchBall(2f));
                canstart = false;
             */
            
            if (ApplyGolfBallForce(bodyJoints[5].position, bodyJoints[6].position, previousJointPositions, previousJointPositions1, Time.time - previousTime, 2))
            {
               
             
            }
           
           
        }

        // 記錄當前關節數據作為上一幀的數據
        previousJointPositions = bodyJoints[5].position;
        previousJointPositions1 = bodyJoints[6].position;
        previousTime = Time.time;

        // 解析 hips (身體中心) 的位置數據
        string[] hipsParts = lines[0].Split(',');
        if (!ParseJointData(hipsParts, 0, out Vector3 hipsWorldPosition))
        {
            Debug.Log("Invalid data for hips joint.");
            // Replace the line causing the error in ProcessData method
            hipsWorldPosition = new Vector3(0, 0, 0);
            yield break;
        }

        // 直接更新 hips (身體中心) 的位置
        //bodyJoints[0].position = hipsWorldPosition;

        // Step 2: 處理所有關節的位置
        //bodyJoints[0].position = hipsWorldPosition;

        // Step 2: 處理所有關節的位置
        // 取得 hips 原本在場景中的世界位置（不要被覆蓋）
        hipsWorldPosition = bodyJoints[0].position;

        // ✅ 只控制手部關節（假設 index 3~10 是手）
        for (int i = 3; i <= 10 && i < bodyJoints.Length; i++)
        {
            if (bodyJoints[i] == null) continue;

            // 解析當前關節的位置資料
            string[] parts = lines[i].Split(',');

        


            if (!ParseJointData(parts, i, out Vector3 jointWorldPosition))
                continue;


            
           
            // 計算方向向量（從目前關節指向目標位置）
            Vector3 direction = jointWorldPosition - bodyJoints[i].position;

            // 使用方向向量計算旋轉
            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(Vector3.forward, direction.normalized);
                bodyJoints[i].rotation = Quaternion.Slerp(bodyJoints[i].rotation, targetRotation, Time.deltaTime * 10f);
            }
        }


    }



    void HandleInput()
    {
        // *左右旋轉 (XZ 平面)*
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            transform.Rotate(Vector3.up, -rotationSpeed * Time.deltaTime);
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
    }



    bool ParseJointData(string[] parts, int index, out Vector3 position)
    {
        position = Vector3.zero;

        if (parts.Length >= 3 &&
            float.TryParse(parts[0], out float x) &&
            float.TryParse(parts[1], out float y) &&
            float.TryParse(parts[2], out float visibility) &&
            visibility > 0.00f)
        {
            float scaleX = (index >= 3 && index <= 10) ? 30.0f : 50.0f;
            float scaleY = (index >= 3 && index <= 10) ? 30.0f : 50.0f;

            float baseY = 1.0f; // ✅ 設定地面高度為角色中心

            float mappedX = (x-0.5f ) * scaleX;
            float mappedY = baseY + ((0.5f - y) * scaleY); // ✅ 加上偏移
            float mappedZ = 0f;

            if(index == 7)
            {
                mappedX = (x+1f) * scaleX;
                mappedY = baseY + ((-y) * 70f); // ✅ 加上偏移
            }

            // 最終位置（根據 player 為中心點）
            position = player.position + new Vector3(mappedX, mappedY - player.position.y, mappedZ);
            return true;
        }

        return false;
    }





    void OnApplicationQuit()
    {
        try
        {
            stream?.Close();
            client?.Close();
            if (server != null)
            {
                server.Stop();
                server = null;
            }
            Debug.Log("Server stopped.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error while stopping the server: {e.Message}");
        }
    }

    bool ApplyGolfBallForce(Vector3 joint1Position, Vector3 joint2Position, Vector3 previousJoint1Position, Vector3 previousJoint2Position, float deltaTime, int type)
    {
        // 确保 deltaTime 有效
        if (deltaTime <= 0)
        {
            Debug.LogWarning("Delta time is zero or negative. Skipping force application.");
            return false;
        }


        int clubType = clubSwitcher.GetCurrentClubType();
        Debug.Log($"Golf club type: {clubType}");



        // 计算 joint1,2 的速度向量
        Vector3 joint1Velocity = (joint1Position - previousJoint1Position) / deltaTime;
        Vector3 joint2Velocity = (joint2Position - previousJoint2Position) / deltaTime;

        // 计算 joint1Velocity 的模长（速度的纯量）
        float joint1Speed = joint1Velocity.magnitude * 30;
        float joint2Speed = joint2Velocity.magnitude * 30;


        Vector3 force;
        // float joint1Speed = 5;
        // 计算 joint2 和 joint1 的位置差
        Vector3 delta = joint2Position - joint1Position;
        if (joint1Speed != 0)
            force = player.parent.forward * joint1Speed;
        else
            force = player.parent.forward * joint2Speed;

        joint1Speed = 5f;

        // 判断条件
        if (delta.x > 0 && delta.y * 10 < -5 && delta.x * 10 > 1)
        {
            //ResetPlayerRotation();
            force.y = 0f;
            float forcemagnitude = joint1Speed;
            StartCoroutine(ballInitialForce.LaunchBall(forcemagnitude));
            canstart = false;
            Debug.Log("Force applied successfully.");

            return true;
        }
        else
        {
            // 条件不满足，跳过处理
        //  Debug.Log($"Conditions not met: delta.x = {delta.x * 10}, delta.y = {delta.y * 10}");
            return true;
        }
    }

}
