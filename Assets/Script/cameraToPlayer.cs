using UnityEngine;

public class PlayerClampToCamera : MonoBehaviour
{
    public Camera cam;
    public float padding = 0.5f; 

    private float minX, maxX, minY, maxY;

    void Start()
    {
        if (!cam) cam = Camera.main;
        UpdateBounds();
    }

    void LateUpdate()
    {
        Vector3 pos = transform.position;

        
        pos.x = Mathf.Clamp(pos.x, minX + padding, maxX - padding);
        pos.y = Mathf.Clamp(pos.y, minY + padding, maxY - padding);

        transform.position = pos;
    }

    void UpdateBounds()
    {
        
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        Vector3 c = cam.transform.position;

        minX = c.x - halfW;
        maxX = c.x + halfW;
        minY = c.y - halfH;
        maxY = c.y + halfH;
    }
}

