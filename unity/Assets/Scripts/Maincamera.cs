using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform ball; // 拖入球的 Transform
    public Vector3 offset = new Vector3(0f, 5f, -10f); // 相機與球的相對位置

    void LateUpdate()
    {
        if (ball != null)
        {
            transform.position = ball.position + offset;
        }
    }
}
