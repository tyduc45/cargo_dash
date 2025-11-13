using UnityEngine;
using UnityEngine.EventSystems;

public class HighlightOnHover : MonoBehaviour
{
    public Material mat;
    public float highlightWidth = 4f;
    public Color highlightColor = Color.white;

    private float originalWidth;
    private Color originalColor;

    private void Start()
    {
        originalWidth = mat.GetFloat("_OutlineWidth");
        originalColor = mat.GetColor("_OutlineColor");
        mat.SetFloat("_OutlineActive", 0f); // ≥ı ºπÿ±’
    }

    private void OnMouseEnter()
    {
        mat.SetFloat("_OutlineActive", 1f);
    }

    private void OnMouseExit()
    {
        mat.SetFloat("_OutlineActive", 0f);
    }
}
