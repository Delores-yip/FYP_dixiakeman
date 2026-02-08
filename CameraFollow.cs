using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("拖入玩家对象。如果不填，代码会自动查找 Tag 为 Player 的物体")]
    [SerializeField] private Transform target; 
    [Tooltip("相机看向玩家的高度偏移，建议设置在头部或胸口位置 (例如 1.5 - 1.7)")]
    [SerializeField] private float heightOffset = 1.6f; 

    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 4f;       // 滚轮缩放速度
    [SerializeField] private float minDistance = 0.5f;   // 最近距离（防止穿过头部）
    [SerializeField] private float maxDistance = 10f;    // 最远距离（可自行调整）

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 3f;   // 右键旋转灵敏度
    [SerializeField] private float yMinLimit = -10f;     // 俯仰角限制（防止看地底）
    [SerializeField] private float yMaxLimit = 80f;      // 俯仰角限制

    private float currentDistance = 5f;  // 当前距离
    private float currentYaw = 0f;       // 水平角度
    private float currentPitch = 20f;    // 垂直角度

    void Start()
    {
        // 自动查找玩家（如果没有手动赋值）
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }

        // 初始化角度（基于当前相机角度，防止一开始跳变）
        Vector3 angles = transform.eulerAngles;
        currentYaw = angles.y;
        currentPitch = angles.x;
        
        // 确保距离在范围内
        currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
    }

    void LateUpdate()
    {
        if (target == null) return;

        // --- 1. 处理鼠标滚轮缩放 ---
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            // 滚轮向前(正) -> 距离减小
            currentDistance -= scroll * zoomSpeed;
            currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
        }

        // --- 2. 处理鼠标右键长按旋转 ---
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
            float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed;

            currentYaw += mouseX;
            currentPitch -= mouseY; // 鼠标上移需减小X轴旋转角度（抬头）

            // 限制抬头和低头的角度
            currentPitch = Mathf.Clamp(currentPitch, yMinLimit, yMaxLimit);
        }

        // --- 3. 计算相机位置 ---
        // 目标焦点位置 = 玩家坐标 + 高度偏移
        Vector3 targetPosition = target.position + Vector3.up * heightOffset;
        
        // 计算旋转四元数
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);

        // 计算最终位置：从目标点沿视线反向延伸
        Vector3 negDistance = new Vector3(0.0f, 0.0f, -currentDistance);
        Vector3 position = rotation * negDistance + targetPosition;

        // 应用变换
        transform.rotation = rotation;
        transform.position = position;
    }
}