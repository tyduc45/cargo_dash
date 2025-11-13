using UnityEngine;

public class ParallaxCamera : MonoBehaviour
{
    public float moveAmount = 0.5f;
    public float smoothness = 5f;

    private Vector3 startPos;

    void Start() { startPos = transform.position; }

    // ✓ 新增：启用时用当前相机位当作新的基准
    void OnEnable() { startPos = transform.position; }

    // ✓ 新增：外部可手动重置基准
    public void ResetBaseToCurrent() { startPos = transform.position; }

    void Update()
    {
        float mouseX = (Input.mousePosition.x / Screen.width - 0.5f) * 2f;
        float mouseY = (Input.mousePosition.y / Screen.height - 0.5f) * 2f;
        Vector3 targetPos = startPos + new Vector3(mouseX * moveAmount, mouseY * moveAmount, 0f);
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.unscaledDeltaTime * smoothness);
    }
}
