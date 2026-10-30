using UnityEngine;

public class RotateAroundBall : MonoBehaviour
{
    public Transform ball; // 設定旋轉中心（球）
    public float rotationSpeed = 50f; // 旋轉速度
    public Transform tranform;
    void Update()
    {
        if (Input.GetKey(KeyCode.A)) // 按 A 鍵向左旋轉
        {
            transform.RotateAround(ball.position, Vector3.up, rotationSpeed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.D)) // 按 D 鍵向右旋轉
        {
            transform.RotateAround(ball.position, Vector3.up, -rotationSpeed * Time.deltaTime);
        }
    }

    public void Turnleft()
    {
        transform.RotateAround(ball.position, Vector3.up, rotationSpeed * Time.deltaTime);
    }
    public void Turnright()
    {
        transform.RotateAround(ball.position, Vector3.up, -rotationSpeed * Time.deltaTime);
    }
}