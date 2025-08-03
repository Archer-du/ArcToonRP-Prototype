using UnityEngine;

public class Rotator : MonoBehaviour
{
    [Header("旋转设置")]
    [Tooltip("旋转速度（度/秒）")]
    public Vector3 rotationSpeed = new Vector3(0, 100, 0);

    [Tooltip("是否开始旋转")]
    public bool isRotating = true;

    void Update()
    {
        if (isRotating)
        {
            // 匀速旋转
            transform.Rotate(rotationSpeed * Time.deltaTime, Space.Self);
        }
    }

    /// <summary>
    /// 开始旋转
    /// </summary>
    public void StartRotation()
    {
        isRotating = true;
    }

    /// <summary>
    /// 停止旋转
    /// </summary>
    public void StopRotation()
    {
        isRotating = false;
    }

    /// <summary>
    /// 设置新的旋转速度
    /// </summary>
    /// <param name="newSpeed">新的旋转速度</param>
    public void SetRotationSpeed(Vector3 newSpeed)
    {
        rotationSpeed = newSpeed;
    }
}
