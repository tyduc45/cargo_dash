using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraDollyOutOnce : MonoBehaviour
{
    public bool runOnlyOnce = true;
    public string prefKeyPrefix = "CamDollyOut_";
    public KeyCode skipKey = KeyCode.Escape;

    public float startForwardOffset = 3f;
    public float duration = 1.6f;
    public float delay = 0f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("在过场期间暂时禁用的组件（拖入 ParallaxCamera / CinemachineBrain 等）")]
    public Behaviour[] disableDuringPlay;
    [Header("（可选）Parallax 引用，用于结束后重置基准")]
    public ParallaxCamera parallax;

    Vector3 finalPos, startPos;
    Quaternion finalRot;

    void Awake()
    {
        // 记录最终位，并在第一帧前就把相机放到“起点”，避免闪帧
        finalPos = transform.position;
        finalRot = transform.rotation;
        startPos = finalPos + finalRot * Vector3.forward * startForwardOffset;

        transform.position = startPos;
        transform.rotation = finalRot;
    }

    void Start()
    {
        string key = prefKeyPrefix + SceneManager.GetActiveScene().name;

        if (runOnlyOnce && PlayerPrefs.GetInt(key, 0) == 1)
        {
            // 本场景已播过：保持在最终位，同时确保被禁用的组件开启并校准基准
            ToggleHelpers(true);
            if (parallax) parallax.ResetBaseToCurrent();
            enabled = false;
            return;
        }

        StartCoroutine(PlayOnce(key));
    }

    System.Collections.IEnumerator PlayOnce(string saveKey)
    {
        ToggleHelpers(false);

        if (delay > 0f) yield return new WaitForSeconds(delay);

        float t = 0f;
        while (t < 1f)
        {
            if (Input.GetKeyDown(skipKey)) break;

            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, duration);
            float e = ease.Evaluate(Mathf.Clamp01(t));
            transform.position = Vector3.LerpUnclamped(startPos, finalPos, e);
            yield return null;
        }

        transform.position = finalPos;
        transform.rotation = finalRot;

        ToggleHelpers(true);
        if (parallax) parallax.ResetBaseToCurrent();

        if (runOnlyOnce)
        {
            PlayerPrefs.SetInt(saveKey, 1);
            PlayerPrefs.Save();
        }

        enabled = false;
    }

    void ToggleHelpers(bool on)
    {
        if (disableDuringPlay == null) return;
        foreach (var b in disableDuringPlay) if (b) b.enabled = on;
    }
}

