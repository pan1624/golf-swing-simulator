using UnityEngine;

public class FaceBall : MonoBehaviour
{
    public Transform ball; // 設定球的位置

    void Update()
    {
        if (ball != null)
        {
            transform.LookAt(ball.position);

        }
    }
}