using UnityEngine;

public class character_move : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private Animator animator;

    [Header("Interaction")]
    [SerializeField] private Transform holdPoint; // 物品抓取点（建议在角色手部位置创建一个空物体）
    [SerializeField] private float pickupRange = 2f; // 拾取范围
    [SerializeField] private LayerMask pickableLayer; // 设置一个 Layer 标记可拾取的物体
    [SerializeField] private Vector2 throwForce = new Vector2(5f, 3f); // x为向前推力，y为向上推力

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource; // 挂载在玩家身上的 AudioSource
    [SerializeField] private AudioClip pickUpSound;   // 拾取音效
    [SerializeField] private AudioClip throwSound;    // 投掷音效

    [Header("UI Reference")]
    [SerializeField] private InGameUIManager uiManager; // 拖入场景中的 Canvas

    private Rigidbody rb;
    private Vector3 moveInput;
    private GameObject heldObject; // 当前抓持的物品
    private Transform mainCameraTransform; // 【新增】缓存相机 Transform

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    void Start()
    {
        // 【新增】获取主相机
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        HandleMovementInput();
        
        // 检测空格键点击
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (heldObject == null)
            {
                TryPickUp();
            }
            else
            {
                ThrowHeldObject();
            }
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            // 优先尝试通用交互（捡钱等）
            if (!TryGeneralInteraction())
            {
                // 如果没有交互物体，再检查是不是打开厨具
                CheckKitchenInteraction();
            }
        }
    }

    private void HandleMovementInput()
    {
        // 【修改】将 x, z 改名为 h, v 更符合一般习惯 (Horizontal/Vertical)
        float h = 0f; 
        float v = 0f;
        if (Input.GetKey(KeyCode.W)) v += 1f;
        if (Input.GetKey(KeyCode.S)) v -= 1f;
        if (Input.GetKey(KeyCode.D)) h += 1f;
        if (Input.GetKey(KeyCode.A)) h -= 1f;

        Vector3 targetDirection;

        // 【修改】基于相机方向计算移动向量
        if (mainCameraTransform != null)
        {
            // 获取相机的前方和右方向量
            Vector3 cameraForward = mainCameraTransform.forward;
            Vector3 cameraRight = mainCameraTransform.right;

            // 抹平 Y 轴分量，防止角色往天上飞或钻地
            cameraForward.y = 0f;
            cameraRight.y = 0f;
            
            // 重新归一化，确保方向准确
            cameraForward.Normalize();
            cameraRight.Normalize();

            // 混合输入与方向
            targetDirection = cameraForward * v + cameraRight * h;
        }
        else
        {
            // 如果没找到相机，回退到世界坐标
            targetDirection = new Vector3(h, 0f, v);
        }

        // 限制最大速度幅度为 1 (也就是防止斜向移动速度变快)
        moveInput = targetDirection.sqrMagnitude > 1f ? targetDirection.normalized : targetDirection;

        if (animator != null)
        {
            animator.SetFloat("Speed", moveInput.magnitude, 0.1f, Time.deltaTime);
        }
    }

    private void TryPickUp()
    {
        // 搜索周围的可拾取物体
        Collider[] hitColliders = Physics.OverlapSphere(transform.position + transform.forward, pickupRange, pickableLayer);
        
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.GetComponent<Rigidbody>() != null)
            {
                heldObject = hitCollider.gameObject;
                
                // 禁用物理特性，使其跟随玩家
                Rigidbody objRb = heldObject.GetComponent<Rigidbody>();
                objRb.isKinematic = true;
                objRb.useGravity = false;

                // 禁用碰撞体以避免与玩家碰撞
                Collider objCollider = heldObject.GetComponent<Collider>();
                if (objCollider != null)
                {
                    objCollider.enabled = false;
                }

                // 设置父级到 holdPoint
                heldObject.transform.SetParent(holdPoint);
                heldObject.transform.localPosition = Vector3.zero;
                heldObject.transform.localRotation = Quaternion.identity;

                // 【新增】播放拾取音效
                PlaySound(pickUpSound);
                
                break;
            }
        }
    }

    private void ThrowHeldObject()
    {
        if (heldObject == null) return;

        // 脱离父级
        Rigidbody objRb = heldObject.GetComponent<Rigidbody>();
        heldObject.transform.SetParent(null);
        
        // 恢复物理特性
        objRb.isKinematic = false;
        objRb.useGravity = true;

        // 重新启用碰撞体
        Collider objCollider = heldObject.GetComponent<Collider>();
        if (objCollider != null)
        {
            objCollider.enabled = true;
        }

        // 计算投掷力：角色前方 + 向上的斜上方冲力
        Vector3 force = transform.forward * throwForce.x + Vector3.up * throwForce.y;
        objRb.AddForce(force, ForceMode.Impulse);

        // 【新增】播放投掷音效
        PlaySound(throwSound);

        heldObject = null;
    }
    
    // 辅助方法，避免空引用报错
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void CheckKitchenInteraction()
    {
        // 检测逻辑范围内的物体
        Collider[] hits = Physics.OverlapSphere(transform.position, 2f);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Kitchen"))
            {
                KitchenAppliance ka = hit.GetComponent<KitchenAppliance>();
                if (ka != null && uiManager != null)
                {
                    // 【修改】改为 ToggleKitchenUI 以支持再次按 E 关闭
                    uiManager.ToggleKitchenUI(ka); 
                }
                break;
            }
        }
    }

    // 【新增】通用交互方法
    private bool TryGeneralInteraction()
    {
        // 检测半径设小一点，比拾取范围稍微精确些
        Collider[] hits = Physics.OverlapSphere(transform.position, 2f);
        foreach (var hit in hits)
        {
            // 检查是不是铜钱
            Coin coin = hit.GetComponent<Coin>();
            if (coin != null)
            {
                coin.Collect();
                return true; // 成功交互，阻止打开厨具 UI
            }
        }
        return false;
    }

    void FixedUpdate()
    {
        if (rb != null)
        {
            Vector3 delta = moveInput * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + delta);
        }

        Vector3 dir = new Vector3(moveInput.x, 0f, moveInput.z);
        if (dir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
            if (rb != null)
            {
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime));
            }
        }
    }
}
