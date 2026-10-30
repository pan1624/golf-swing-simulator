using UnityEngine;

[ExecuteInEditMode] // ????♂? **讓腳本在編輯模式下運行**
public class MeasureDistance : MonoBehaviour
{
    public Transform pointA; // 第一個點
    public Transform pointB; // 第二個點
    public float distance; // 量測結果

    void Update()
    {
        if (pointA != null && pointB != null)
        {
            distance = Vector3.Distance(pointA.position, pointB.position) *3;
        }
    }
}
