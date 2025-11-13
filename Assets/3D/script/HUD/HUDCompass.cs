using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    public class HUDCompass : MonoBehaviour
    {
        [Header("References")]
        public Camera cam;
        public Transform player;              // 玩家
        public Transform forwardSource;       // 用你的 aimIndicator（朝向更稳）；留空则用 player.forward
        public RectTransform ring;            // 圆环UI（RectTransform）
        public RectTransform markerPrefab;    // 小图标预制（UI Image）

        [Header("Appearance")]
        public Color spawnerColor = new Color(1f, 0.6f, 0.1f, 1f);
        public Color receiverColor = new Color(0.2f, 0.7f, 1f, 1f);
        public Sprite spawnerSprite;
        public Sprite receiverSprite;

        [Header("Behaviour")]
        public bool ringFollowsPlayerScreen = true; // 圆环跟随玩家屏幕位置
        public float ringScreenYOffset = 0f;         // 往上/下偏移
        [Range(0, 0.2f)]
        public float onscreenHideMargin = 0.05f;     // 进入画面后隐藏的边缘留白

        // 内部
        readonly Dictionary<CompassTarget, RectTransform> _map = new();
        readonly Queue<RectTransform> _pool = new();
        Vector3 _lastForward = Vector3.forward;

        void Awake()
        {
            if (!cam) cam = Camera.main;
        }

        void OnEnable() => Rebuild();
        void OnDisable() => Clear();

        public void Rebuild()
        {
            Clear();
            foreach (var t in CompassTarget.Active) AddTarget(t);
        }

        void Clear()
        {
            foreach (var kv in _map) ReleaseMarker(kv.Value);
            _map.Clear();
        }

        void AddTarget(CompassTarget t)
        {
            var m = GetMarker();
            ConfigureMarker(m, t);
            _map[t] = m;
        }

        RectTransform GetMarker()
        {
            if (_pool.Count > 0)
            {
                var m = _pool.Dequeue();
                m.gameObject.SetActive(true);
                return m;                         // ❌ 不再 m.SetParent(ring, false)
            }
            return Instantiate(markerPrefab, ring);
        }

        // 归还对象：只隐藏就好
        void ReleaseMarker(RectTransform m)
        {
            if (!m) return;
            m.gameObject.SetActive(false);       // ❌ 不再 m.SetParent(transform, false)
            _pool.Enqueue(m);
        }

        void ConfigureMarker(RectTransform m, CompassTarget t)
        {
            var img = m.GetComponent<Image>();
            if (!img) return;
            if (t.kind == CompassKind.Spawner)
            {
                img.color = spawnerColor;
                if (spawnerSprite) img.sprite = spawnerSprite;
            }
            else
            {
                img.color = receiverColor;
                if (receiverSprite) img.sprite = receiverSprite;
            }
        }

        void Update()
        {
            if (!player || !cam || !ring) return;

            // 动态增删（新目标启用/停用）
            foreach (var t in CompassTarget.Active)
                if (!_map.ContainsKey(t)) AddTarget(t);

            var toRemove = ListPool<CompassTarget>.Get();
            foreach (var kv in _map)
                if (kv.Key == null || !kv.Key.isActiveAndEnabled) toRemove.Add(kv.Key);
            foreach (var t in toRemove) { ReleaseMarker(_map[t]); _map.Remove(t); }
            ListPool<CompassTarget>.Release(toRemove);

            // 圆环跟随玩家屏幕位置（或固定在屏幕中央）
            if (ringFollowsPlayerScreen)
            {
                var sp = cam.WorldToScreenPoint(player.position);
                ring.position = new Vector3(sp.x, sp.y + ringScreenYOffset, 0f);
            }

            // 取朝向（优先指示器）
            Vector3 refFwd = Vector3.forward; // 世界Z+为北
            refFwd.y = 0f;
            _lastForward = refFwd.normalized;

            float radius = Mathf.Min(ring.rect.width, ring.rect.height) * 0.5f;

            foreach (var (t, marker) in _map)
            {
                if (!t || !marker) continue;

                Vector3 tp = t.WorldPos;
                Vector3 to = tp - player.position;
                Vector3 toPlanar = new Vector3(to.x, 0f, to.z);
                if (toPlanar.sqrMagnitude < 1e-6f) { marker.gameObject.SetActive(false); continue; }

                // 在屏幕里吗？在的话隐藏
                Vector3 vp = cam.WorldToViewportPoint(tp);
                bool onScreen = vp.z > 0f &&
                                vp.x > onscreenHideMargin && vp.x < 1f - onscreenHideMargin &&
                                vp.y > onscreenHideMargin && vp.y < 1f - onscreenHideMargin;
                marker.gameObject.SetActive(!onScreen);
                if (onScreen) continue;

                // 与玩家朝向的夹角（度）：0=正前方
                float angle = Vector3.SignedAngle(_lastForward, toPlanar, Vector3.up);
                float rad = angle * Mathf.Deg2Rad;

                // 放在圆环上：0°在正上（12点方向）
                Vector2 local = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)) * radius;
                marker.anchoredPosition = local;

                // 让小箭头朝外（可选）
                marker.localRotation = Quaternion.Euler(0, 0, -angle);
            }
        }

        // 小列表池（减少GC；也可用 Unity.Collections 等）
        static class ListPool<T>
        {
            static readonly Stack<List<T>> pool = new();
            public static List<T> Get() => pool.Count > 0 ? pool.Pop() : new List<T>(8);
            public static void Release(List<T> list) { list.Clear(); pool.Push(list); }
        }
    }
}