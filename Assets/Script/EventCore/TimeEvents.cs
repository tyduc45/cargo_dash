using System;

[Serializable]
public class TimedEvent
{
    public float timestamp;
    public string eventName;    // Corresponds to the name in EventPool, or special "SpawnCargo"
    public string payloadJson;  // Optional: extra parameters (JSON), read by specific systems
}

