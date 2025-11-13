using UnityEngine;

public abstract class GameEventBehaviour : MonoBehaviour
{
    // 每个事件触发的公共接口
    public abstract void Trigger();
}