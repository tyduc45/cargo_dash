using UnityEngine;
using UnityEngine.UI;

public class MenuParallax : MonoBehaviour
{
    public float offsetMultiplier = .1f;
    public float smoothTime = .3f;


    private Vector2 startPosition;
    private Vector3 velocity;


    private void Start()
    {
        startPosition = transform.position;
    }

    public void Update()
    {
        Vector2 offset = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        transform.position = Vector3.SmoothDamp(transform.position, startPosition + (offset * offsetMultiplier), ref velocity, smoothTime);
    }


}
