using System.Runtime.InteropServices.WindowsRuntime;
using TMPro;
using UnityEngine;

public class EventFont : MonoBehaviour
{
   public static EventFont Instance;

    
    public TMP_FontAsset eventFont;

    [Header("Font Presets")]
    public TMP_FontAsset slowFont;
    public TMP_FontAsset stunFont;
    public TMP_FontAsset hitFont;
    public TMP_FontAsset buffFont;


    [Header("UI Elements")]
    public TMP_FontAsset messageFont;
    public TMP_FontAsset comboFont;

    [Header("Gradient Presets")]
    public TMP_ColorGradient slowGradient;
    public TMP_ColorGradient stunGradient;
    public TMP_ColorGradient hitGradient;
    public TMP_ColorGradient messageGradient;
    public TMP_ColorGradient comboGradient;
    public TMP_ColorGradient buffGradient;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }


    public void ApplyGradient(TextMeshPro tmpComponent, GradientType gradientType)
    {
        if (tmpComponent != null)
        {
            tmpComponent.colorGradientPreset = GetGradient(gradientType);
        }
    }

    public void ApplyFont(TextMeshPro tmpComponent, GradientType gradientType)
    {
        if (tmpComponent != null)
        {
            switch (gradientType)
            {
                case GradientType.Slow:
                    tmpComponent.font = slowFont;
                    break;
                case GradientType.Stun:
                    tmpComponent.font = stunFont;
                    break;
                case GradientType.Hit:
                    tmpComponent.font = hitFont;
                    break;
                case GradientType.Buff:
                     tmpComponent.font = buffFont;
                    break;
                default:
                    tmpComponent.font = eventFont;
                    break;
            }
        }
    }

  

    public TMP_ColorGradient GetGradient(GradientType gradientType)
    {
        switch (gradientType)
        {
            case GradientType.Slow:
                return slowGradient;
            case GradientType.Stun:
                return stunGradient;
            case GradientType.Hit:
                return hitGradient;
            case GradientType.Message:
                return messageGradient;
            case GradientType.Buff:
                return buffGradient;
            default:
                return null;
        }
    }

    public TMP_FontAsset GetFont(FontType fontType)
    {
        switch (fontType)
        {
            case FontType.Message:
                return messageFont;
            case FontType.Combo:
                return comboFont;
            case FontType.Buff:
                return buffFont;
            default:
                return eventFont;
        }
    }

    public void ApplyFont(TextMeshPro tmpComponent, FontType fontType)
    {
        if (tmpComponent != null)
        {
            tmpComponent.font = GetFont(fontType);
        }
    }


}

public enum GradientType
{
    Slow,
    Stun,
    Hit,
    Message,
    Buff
}

public enum FontType
{
    Message,
    Combo,
    Buff
}
