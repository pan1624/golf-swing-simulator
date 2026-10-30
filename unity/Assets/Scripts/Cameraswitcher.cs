using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    public Camera mainCamera;      // 主相機
    public Camera characterCamera; // 角色相機
    public SocketReceiver socketeceiver;

    void Start()
    {
        // 確保主相機啟動時關閉角色相機
        mainCamera.enabled = true;
        characterCamera.enabled = false;
    }

    void Update()
    {
        // 根據 canchangecam 切換相機
        if(socketeceiver.canchangecam == true)
        {
            mainCamera.enabled = !mainCamera.enabled;
            characterCamera.enabled = !characterCamera.enabled;
           // socketeceiver.canchangecam = false;
        }
    }

    public void ChangeCamera()
    {
        mainCamera.enabled = !mainCamera.enabled;
        characterCamera.enabled = !characterCamera.enabled;
    }

}
