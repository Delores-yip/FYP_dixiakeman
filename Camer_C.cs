using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camer_C : MonoBehaviour
{
    private Camera mainCamera;
    private float moveSpeed = 50;
    private Vector2 borderX = new Vector2(120,300);
    private Vector2 borderZ = new Vector2(-180,50);
    void Start()
    {
        mainCamera = GetComponent<Camera>();
    }


    void Update()
    {
        Move();
    }

    private void Move()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 dir = new Vector3(v, 0, -h);
        transform.position += dir * Time.deltaTime * moveSpeed;
        if(Input.GetKey(KeyCode.LeftShift))
        {
            dir *= 3;
        }
        if(transform.position.x > borderX.y)
        {
            transform.position = new Vector3(borderX.y, transform.position.y, transform.position.z);
        }
        else if(transform.position.x < borderX.x)
        {
            transform.position = new Vector3(borderX.x, transform.position.y, transform.position.z);
        }

        if(transform.position.z > borderZ.y)
        {
            transform.position = new Vector3(transform.position.x, transform.position.y, borderZ.y);
        }
        else if(transform.position.z < borderZ.x)
        {
            transform.position = new Vector3(transform.position.x, transform.position.y, borderZ.x);
        }

        float mouseScrollWheel = Input.GetAxis("Mouse ScrollWheel");
        if(mouseScrollWheel > 0 )
        {
            mainCamera.fieldOfView = Mathf.Clamp(mainCamera.fieldOfView - 10, 20, 100);
        }
        else if(mouseScrollWheel < 0 )
        {
            mainCamera.fieldOfView = Mathf.Clamp(mainCamera.fieldOfView + 10, 20, 100);
        }
// 鼠标中键旋转摄像机
        if (Input.GetMouseButton(2)) // 鼠标中键
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            // 水平旋转摄像机（绕Y轴）
            transform.Rotate(Vector3.up, mouseX * 3f, Space.World);

            // 垂直旋转摄像机（绕X轴，限制角度防止翻转）
            float angle = transform.eulerAngles.x - mouseY * 3f;
            angle = Mathf.Clamp(angle, 10, 80); // 你可以根据需要调整上下限
            Vector3 euler = transform.eulerAngles;
            euler.x = angle;
            transform.eulerAngles = euler;
        }        
      
    }
}
