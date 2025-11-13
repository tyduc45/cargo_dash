using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class IconFxControllerUI : MonoBehaviour
{
    [Header("段落时长（秒）")]
    public float preFlashDuration = 0.35f;
    public float invertDuration = 2.0f;
    public float postFlashDuration = 0.35f;

    [Header("闪烁参数")]
    public float preFlashSpeed = 14f, preFlashAmp = 0.9f;
    public float invertFlashSpeed = 3.2f, invertFlashAmp = 0.35f;
    public float postFlashSpeed = 14f, postFlashAmp = 0.9f;

    [Header("循环设置")]
    public bool loop = false;          // 勾上就循环
    public float loopDelay = 0.5f;      // 每次循环之间停顿
    public int loopCount = 0;         // 循环次数（<=0 表示无限循环）

    [Header("自动播放")]
    public bool playOnEnable = true;    // OnEnable 时按上面模式播放

    // Shader 属性
    static readonly int PID_Invert = Shader.PropertyToID("_Invert");
    static readonly int PID_FlashStrength = Shader.PropertyToID("_FlashStrength");
    static readonly int PID_FlashSpeed = Shader.PropertyToID("_FlashSpeed");

    Image _img;
    Material _mat;
    bool _running;
    Coroutine _co;

    void Awake()
    {
        _img = GetComponent<Image>();
        // UI 不支持 MPB，给每个图标实例化一份材质
        _img.material = _img.material ? new Material(_img.material)
                                      : new Material(Shader.Find("UI/IconFX-UI (URP)"));
        _mat = _img.material;
        Set(0f, 0f, 8f);
    }

    void OnEnable()
    {
        if (!playOnEnable) return;
        if (loop) StartLoop(); else PlayOnce();
    }

    void OnDisable()
    {
        StopLoop(immediateReset: true);
    }

    // —— 对外接口 —— //
    public void PlayOnce()
    {
        StopLoop();
        _co = StartCoroutine(SeqOnce());
    }

    public void StartLoop()
    {
        StopLoop();
        _co = StartCoroutine(SeqLoop());
    }

    public void StopLoop(bool immediateReset = false)
    {
        if (_co != null) StopCoroutine(_co);
        _co = null;
        _running = false;
        if (immediateReset) Set(0f, 0f, 8f);
    }

    // —— 播放流程 —— //
    System.Collections.IEnumerator SeqOnce()
    {
        _running = true;
        yield return FlashBlock(0f, preFlashDuration, preFlashSpeed, preFlashAmp);
        yield return InvertBlock(invertDuration, invertFlashSpeed, invertFlashAmp);
        yield return FlashBlock(0f, postFlashDuration, postFlashSpeed, postFlashAmp);
        Set(0f, 0f, 8f); // 复位
        _running = false;
    }

    System.Collections.IEnumerator SeqLoop()
    {
        _running = true;
        int played = 0;
        while (loop && (loopCount <= 0 || played < loopCount))
        {
            yield return SeqOnce();
            played++;
            if (!loop) break;
            if (loopDelay > 0f)
                yield return new WaitForSecondsRealtime(loopDelay);
        }
        _running = false;
    }

    // —— 段落块 —— //
    System.Collections.IEnumerator FlashBlock(float inv, float dur, float spd, float amp)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float flash = Mathf.Abs(Mathf.Sin(Time.unscaledTime * spd)) * amp;
            Set(inv, flash, spd);
            yield return null;
        }
    }

    System.Collections.IEnumerator InvertBlock(float dur, float spd, float amp)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float flash = Mathf.Abs(Mathf.Sin(Time.unscaledTime * spd)) * amp;
            Set(1f, flash, spd);
            yield return null;
        }
    }

    // —— 写材质参数 —— //
    void Set(float invert, float flash, float speed)
    {
        if (!_mat) return;
        _mat.SetFloat(PID_Invert, invert);
        _mat.SetFloat(PID_FlashStrength, flash);
        _mat.SetFloat(PID_FlashSpeed, speed);
    }
}

