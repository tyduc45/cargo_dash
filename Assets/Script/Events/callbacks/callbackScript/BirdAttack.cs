using UnityEngine;
using System.Collections;

public class BirdAttack : GameEventBehaviour
{
    [Header("鸟 Prefab")]
    public GameObject birdPrefab;

    [Header("攻击参数")]
    public float birdSpeed = 4f;            // 鸟移动速度
    public float lifetime = 15f;            // 鸟存在时间
    public float[] angles = { 30f, 60f };   // 可能的发射角度

    [Header("场景边界矩形(以本物体为中心)")]
    public Vector2 sceneSize = new Vector2(20f, 10f); // 宽高
    public Color gizmoColor = new Color(1, 0, 0, 0.3f); // 在 Scene 中显示矩形颜色

    public override void Trigger()
    {
        SpawnBird();
    }

    void SpawnBird()
    {
        if (!birdPrefab)
        {
            Debug.LogWarning("[BirdAttack] birdPrefab 未设置");
            return;
        }

        Vector2 half = sceneSize * 0.5f;
        Vector3 center = transform.position; // 世界中心点
        float xLeft = center.x - half.x;
        float xRight = center.x + half.x;
        float yBottom = center.y - half.y;
        float yTop = center.y + half.y;

        // 随机从左右两边生成
        bool fromLeft = Random.value < 0.5f;
        float x = fromLeft ? xLeft : xRight;

        // Y 在中部或上四分之一随机
        float y = Random.value < 0.5f ?
            Random.Range(center.y - half.y * 0.5f, center.y + half.y * 0.5f) : // 中部
            Random.Range(center.y + half.y * 0.5f, yTop);                      // 上 1/4

        Vector2 pos = new Vector2(x, y);

        var bird = Instantiate(birdPrefab, pos, Quaternion.identity);

        var bCtrl = bird.AddComponent<BirdController>();
        bCtrl.speed = birdSpeed;
        bCtrl.lifetime = lifetime;
        // 使用世界坐标的矩形作为边界
        bCtrl.sceneBounds = new Rect(xLeft, yBottom, sceneSize.x, sceneSize.y);

        // 发射角度
        float angle = angles[Random.Range(0, angles.Length)];
        float dirX = fromLeft ? 1 : -1;
        float dirY = Random.value < 0.5f ? 1 : -1; // 上或下
        Vector2 dir = Quaternion.Euler(0, 0, angle * dirY) * Vector2.right * dirX;
        bCtrl.direction = dir.normalized;
    }

    // 在 Scene 视图中绘制矩形，方便调整
    void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Vector3 center = transform.position;
        Vector3 size = new Vector3(sceneSize.x, sceneSize.y, 0);
        Gizmos.DrawWireCube(center, size);
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.1f);
        Gizmos.DrawCube(center, size);
    }
}
