using UnityEngine;

public class ZoomIn : MonoBehaviour
{
    void Start()
    {
        transform.localScale = Vector3.zero;
        LeanTween.scale(gameObject, Vector3.one, 0.2f).setEaseInOutBounce();
    }
}
