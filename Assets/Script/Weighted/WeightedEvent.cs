using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "GameEvent", menuName = "Events/Game Event")]
public class GameEvent : ScriptableObject, IWeighted
{
    [Header("Identity")]
    public string eventName;                  // 与 Timeline 中的 eventName 一致
    [Tooltip("指向实现 GameEventBehaviour prefab")]
    public GameObject behaviourPrefab;

    [Header("Weight")]
    [Range(1, 100)]
    public int weight = 1;
    public int Weight => weight;

    /// <summary>
    /// 调度器最终调用这个方法
    /// </summary>
    public void Invoke()
    {
        if (!behaviourPrefab)
        {
            Debug.LogWarning($"[GameEvent] {eventName} 没有绑定 Prefab");
            return;
        }

        // 在当前激活场景实例化
        var instance = Instantiate(behaviourPrefab);
        GameEventBehaviour behaviour = instance.GetComponent<GameEventBehaviour>();
        SceneManager.MoveGameObjectToScene(instance.gameObject, SceneManager.GetActiveScene());
        behaviour.Trigger();
    }
}
