using UnityEngine;

public class ScreenProtector : MonoBehaviour
{
    public float speed = 5f; // 移動速度
    public float rotationSpeed = 360f; // 旋轉速度（每秒度數）
    private Vector2 direction; // 移動方向

    private float minX, maxX, minY, maxY; // 螢幕邊界
    private float objectWidth, objectHeight;

    private void Start()
    {
        // 初始化移動方向（隨機角度）
        var angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized;

        // 獲取主攝影機
        var cam = Camera.main;

        // 計算攝影機的邊界（世界座標）
        var camHeight = 2f * cam.orthographicSize;
        var camWidth = camHeight * cam.aspect;
        minX = cam.transform.position.x - camWidth / 2f;
        maxX = cam.transform.position.x + camWidth / 2f;
        minY = cam.transform.position.y - camHeight / 2f;
        maxY = cam.transform.position.y + camHeight / 2f;

        // 獲取物件尺寸（假設有 SpriteRenderer）
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            objectWidth = sr.bounds.extents.x;
            objectHeight = sr.bounds.extents.y;
        }
        else
        {
            objectWidth = objectHeight = 0f;
        }
    }

    private void Update()
    {
        // 移動物件
        transform.Translate(direction * speed * Time.deltaTime, Space.World);

        // 旋轉物件
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);

        // 獲取當前位置
        var pos = transform.position;

        // 檢查水平邊界
        if (pos.x - objectWidth < minX || pos.x + objectWidth > maxX)
        {
            // 反射 X 方向
            direction.x = -direction.x;
            pos.x = Mathf.Clamp(pos.x, minX + objectWidth, maxX - objectWidth);
            transform.position = pos;
        }

        // 檢查垂直邊界
        if (pos.y - objectHeight < minY || pos.y + objectHeight > maxY)
        {
            // 反射 Y 方向
            direction.y = -direction.y;
            pos.y = Mathf.Clamp(pos.y, minY + objectHeight, maxY - objectHeight);
            transform.position = pos;
        }
    }
}