using UnityEngine;
using System.Collections;

public class WaterLeakage3D : GameEventBehaviour
{
    [Header("Spawn Area (�� BoxCollider ���巶Χ)")]
    public BoxCollider spawnArea;       // 3D ������
    public GameObject dropletPrefab;    // ˮ�Σ��� Rigidbody + Collider��

    [Header("Danger Zone")]
    public GameObject dangerZonePrefab; // Σ�������� DangerArea3D + BoxCollider isTrigger��
    public float dropletFallSpeed = 10f;
    public int droplets = 3;
    [Tooltip("��Ե��淨�ߵ�̧�߾���")]
    public float dangerZoneHeightOffset = 0.02f;
    [Tooltip("�������������ٸ�һЩ�ĵ������")]
    public float extraSpawnHeight = 2f;

    public override void Trigger()
    {
        StartCoroutine(SpawnDroplets(droplets));
    }

    private IEnumerator SpawnDroplets(int count)
    {
        if (spawnArea == null)
        {
            Debug.LogWarning("[WaterLeakage3D] spawnArea δ����");
            yield break;
        }

        for (int i = 0; i < count; i++)
        {
            Vector3 worldPos = RandomPointOnTopOfBox(spawnArea, extraSpawnHeight);

            var d = Instantiate(dropletPrefab, worldPos, Quaternion.identity);
            var rb = d.GetComponent<Rigidbody>();
            if (rb) rb.linearVelocity = Vector3.down * dropletFallSpeed;

            var droplet = d.AddComponent<Droplet3D>();
            droplet.Init(this, dangerZonePrefab);

            // �ɼ���Ч��WaterDropletSpawn
            yield return new WaitForSeconds(0.2f);
        }
    }

    private static Vector3 RandomPointOnTopOfBox(BoxCollider box, float extraHeight)
    {
        // �� BoxCollider �ı���������������ת�������꣨������ת��
        var t = box.transform;
        Vector3 local = new Vector3(
            Random.Range(-box.size.x * 0.5f, box.size.x * 0.5f),
            box.size.y * 0.5f + extraHeight,
            Random.Range(-box.size.z * 0.5f, box.size.z * 0.5f)
        );
        return t.TransformPoint(box.center + local);
    }

    /// <summary> ����ˮ�ε���ײ�߼���3D�� </summary>
    class Droplet3D : MonoBehaviour
    {
        private GameObject dangerPrefab;
        private WaterLeakage3D parent;

        public void Init(WaterLeakage3D p, GameObject dpf)
        {
            parent = p;
            dangerPrefab = dpf;
        }

        void OnCollisionEnter(Collision col)
        {
            if (col.collider.CompareTag("player"))
            {
                // ��ѡ����ˮ��������΢����
                if (col.rigidbody) col.rigidbody.linearVelocity *= 0.5f;
                var player = col.gameObject.GetComponent<Controller3D>();
                player.ApplyDropletSlow();
                Destroy(gameObject);
                return;
            }

            if (col.collider.CompareTag("ground"))
            {
                Vector3 avg = transform.position;
                Vector3 normal = Vector3.up;

                if (col.contactCount > 0)
                {
                    avg = Vector3.zero;
                    for (int i = 0; i < col.contactCount; i++)
                        avg += col.contacts[i].point;
                    avg /= Mathf.Max(1, col.contactCount);
                    normal = col.contacts[0].normal;
                }

                // ✅ 避免侧面法线导致竖立
                if (normal.y < 0.5f)
                    normal = Vector3.up;

                // ✅ 让Plane模型(+Z方向)法线对齐地面法线
                Quaternion rot = Quaternion.FromToRotation(Vector3.forward, -normal);

                // ✅ 根据 BoxCollider 半高 + 自定义高度偏移 推出地面
                var box = dangerPrefab.GetComponent<BoxCollider>();
                float h = box != null ? box.size.y * 0.5f : 0.05f;
                Vector3 spawnPos = avg + normal * (h + parent.dangerZoneHeightOffset);

                Instantiate(dangerPrefab, spawnPos, rot);

                Destroy(gameObject);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (spawnArea == null) return;
        var t = spawnArea.transform;
        Gizmos.matrix = t.localToWorldMatrix;
        Gizmos.color = new Color(0, 1, 1, 0.6f);
        Gizmos.DrawWireCube(spawnArea.center, spawnArea.size);
        Gizmos.color = new Color(0, 1, 1, 0.15f);
        // ����ʾ��
        Gizmos.DrawCube(spawnArea.center + Vector3.up * (spawnArea.size.y * 0.5f + extraSpawnHeight),
                        new Vector3(spawnArea.size.x, 0.02f, spawnArea.size.z));
    }
}
