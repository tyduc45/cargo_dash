using UnityEngine;
using System.Collections;
using TMPro;

public class WaterLeakage : GameEventBehaviour
{
    [Header("Spawn Area")]
    public SpriteRenderer spawnAreaSprite;  // 用 SpriteRenderer 来定义范围
    public GameObject dropletPrefab;       // 雨滴Prefab

    [Header("Danger Zone")]
    public GameObject dangerZonePrefab;    // 危险区Prefab（内部已挂 SpriteRenderer + BoxCollider2D + DangerArea）
    public float dropletFallSpeed = 10f;   // 雨滴下落速度
    public int droplets = 1;               // 一次生成多少滴水

    [Header("Landing Position")]
    public float dangerZoneHeightOffset = 0.3f;

    public override void Trigger()
    {
        StartCoroutine(SpawnDroplets(droplets));
    }

    private IEnumerator SpawnDroplets(int count)
    {
        if (!spawnAreaSprite)
        {
            Debug.LogWarning("[WaterLeakage] spawnAreaSprite 未设置");
            yield break;
        }

        // 使用 sprite.bounds 得到局部尺寸（相对于自身坐标系）
        Bounds localBounds = spawnAreaSprite.sprite.bounds;

        for (int i = 0; i < count; i++)
        {
            // 在局部空间随机
            float randX = Random.Range(localBounds.min.x, localBounds.max.x);
            float startY = localBounds.max.y + 2f;   // 在顶部再加2单位
            Vector3 localPos = new Vector3(randX, startY, 0f);

            // 转成世界坐标
            Vector3 worldPos = spawnAreaSprite.transform.TransformPoint(localPos);

            var d = Instantiate(dropletPrefab, worldPos, Quaternion.identity);
            var rb = d.GetComponent<Rigidbody2D>();
            if (rb) rb.linearVelocity = Vector2.down * dropletFallSpeed;

            var droplet = d.AddComponent<Droplet>();
            droplet.Init(this, dangerZonePrefab);

            SoundManager.Instance.PlaySound(SoundType.WaterDropletSpawn, null, 0.2f);

            yield return new WaitForSeconds(0.2f);
        }
    }

    /// <summary> 单个水滴的碰撞逻辑 </summary>
    class Droplet : MonoBehaviour
    {
        private GameObject dangerPrefab;
        private WaterLeakage parentLeakage;
        //public void Init(WaterLeakage p, GameObject dpf) => dangerPrefab = dpf;

        [Header("Hit Text")]
        [SerializeField]
        private TMP_FontAsset customFont;

        public void Init(WaterLeakage parent, GameObject dpf)
        {
            parentLeakage = parent;
            dangerPrefab = dpf;
        }

        void OnCollisionEnter2D(Collision2D col)
        {
            if (col.collider.CompareTag("player"))
            {
                var pc = col.collider.GetComponent<controller>();
                if (pc != null && pc.IsStrong)
                {
                    // 强壮：无视雨滴减速
                    Destroy(gameObject);
                    return; // 若方法不是协程，直接 return;
                }

                col.collider.GetComponent<controller>()?.ApplyDropletSlow();
                SoundManager.Instance.PlaySound(SoundType.WaterSplashPlayer, null, 0.3f);
                TweenUtils.ShowStatusText("SLOWED!", transform.position, 1.0f, GradientType.Slow);
                Destroy(gameObject);
            }
            else if (col.collider.CompareTag("ground"))
            {
                ContactPoint2D cp = col.GetContact(0);
                Vector2 sum = Vector2.zero;
                for (int i = 0; i < col.contactCount; i++) sum += col.GetContact(i).point;
                Vector2 avgPoint = sum / col.contactCount;
                Vector2 hitNormal = cp.normal;  // 法线

                SoundManager.Instance.PlaySound(SoundType.WaterDropletLand, null, 0.55f);
                //// 为了避免与地面重叠，沿法线抬高一点
                //Vector3 spawnPos = (Vector3)avgPoint + (Vector3)hitNormal * 0.02f;

                float heightOffset = parentLeakage != null ? parentLeakage.dangerZoneHeightOffset : 0.02f;
                Vector3 spawnPos = (Vector3)avgPoint + (Vector3)hitNormal * heightOffset;

                // 如需让危险区朝向地面法线（可选）
                Quaternion rot = Quaternion.identity;

                Instantiate(dangerPrefab, spawnPos, rot);
                Destroy(gameObject);
            }
        }
    }
}
