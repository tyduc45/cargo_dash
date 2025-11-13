using System.Collections.Generic;
using UnityEngine;

namespace Project.UI
{
    public enum CompassKind { Spawner, Receiver }

    public class CompassTarget : MonoBehaviour
    {
        public CompassKind kind = CompassKind.Spawner;
        public float yOffset = 0f; // 需要把标签抬高/降低可用这个

        // 所有激活的目标，HUD 会自动订阅
        public static readonly HashSet<CompassTarget> Active = new HashSet<CompassTarget>();

        void OnEnable() => Active.Add(this);
        void OnDisable() => Active.Remove(this);

        public Vector3 WorldPos => transform.position + Vector3.up * yOffset;
    }
}