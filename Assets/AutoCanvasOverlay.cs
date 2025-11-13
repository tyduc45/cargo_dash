using UnityEngine;
using UnityEngine.SceneManagement;

public class AutoCanvasOverlay : MonoBehaviour
{
    private Canvas canvas;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();

        int buildIndex = SceneManager.GetActiveScene().buildIndex;

        // 如果当前是3D MainMenu (1) 或 3DLevelSelect (2)
        if (buildIndex == 1 || buildIndex == 2)
        {
            ForceOverlay();
        }
    }

    private void ForceOverlay()
    {
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 600;
        Debug.Log($"{gameObject.name}: Canvas forced to Overlay in Scene {SceneManager.GetActiveScene().name}");
    }
}
