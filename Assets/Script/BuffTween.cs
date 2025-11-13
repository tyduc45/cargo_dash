using UnityEngine;

public class BuffTween : MonoBehaviour
{
    void Start()
    {
        TweenUtils.PulseLoop(gameObject);
    }
}
