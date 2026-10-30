using UnityEngine;

public class SmoothGolfBallCameraFollow : MonoBehaviour
{
    public Transform golfBall; // 連結高爾夫球
    public Vector3 offset = new Vector3(0, 5, 20); // 相機相對於球的位置
    public float smoothSpeed = 5f; // 平滑移動速度

    void LateUpdate()
    {
        if (golfBall != null)
        {
            Vector3 desiredPosition = golfBall.position + offset;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
            transform.LookAt(golfBall.position); // 讓相機朝向球
        }
    }
}