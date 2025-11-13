using UnityEngine;

public class RestartDebug : MonoBehaviour
{
    public EventScheduler scheduler;
    public TimelineGenerator generator;
    public GameManager gameManager;

    public void LogStates(string step)
    {
        Debug.Log($"--- [DEBUG] {step} ---");
        Debug.Log($"GameManager.currentState = {gameManager?.currentState}");
        Debug.Log($"Time.timeScale = {Time.timeScale}");
        Debug.Log($"Scheduler ref = {(scheduler ? "OK" : "NULL")}");
        Debug.Log($"Scheduler.eventList = {(GetEventList() == null ? "NULL" : $"Count={GetEventList().Count}")}");
        Debug.Log($"Generator ref = {(generator ? "OK" : "NULL")}");
        Debug.Log($"Generator.Generated = {(generator?.Generated == null ? "NULL" : $"Count={generator.Generated.Count}")}");
    }

    private System.Collections.Generic.List<TimedEvent> GetEventList()
    {
        var field = typeof(EventScheduler).GetField("eventList",
                     System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field?.GetValue(scheduler) as System.Collections.Generic.List<TimedEvent>;
    }
}
