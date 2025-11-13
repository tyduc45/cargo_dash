using UnityEngine;
using UnityEngine.SceneManagement;

public class ScrollCredits : MonoBehaviour
{
    [Header("Target RectTransform (Canvas UI)")]
    [SerializeField] private RectTransform targetRect = null;

    [Header("Scroll settings")]
    [Tooltip("Y position to scroll to")]
    [SerializeField] private float targetY = 12580f;
    [Tooltip("Y position to reset to when finished")]
    [SerializeField] private float resetY = -568f;
    [Tooltip("Scroll speed in units per second. If <= 0 will snap immediately.")]
    [SerializeField] private float speed = 300f;
    [Tooltip("Delay before restarting the loop (seconds, unscaled)")]
    [SerializeField] private float restartDelay = 0.25f;
    [Tooltip("Start scrolling automatically on Start")]
    [SerializeField] private bool autoStart = true;

    [Header("Main menu")]
    [Tooltip("Scene build index to load when Escape is pressed")]
    [SerializeField] private int mainMenuSceneBuildIndex = 0;

    private bool isScrolling = false;

    private void Awake()
    {
        if (targetRect == null)
            targetRect = GetComponent<RectTransform>();
    }

    private void Start()
    {
        if (autoStart)
            StartScrolling();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            
            SceneManager.LoadScene(mainMenuSceneBuildIndex);
        }
    }

    public void StartScrolling()
    {
        if (targetRect == null || isScrolling)
            return;

        LeanTween.cancel(gameObject);

        float startY = targetRect.anchoredPosition.y;

       
        if (speed <= 0f || Mathf.Approximately(startY, targetY))
        {
            SetYAndLoop(targetY);
            return;
        }

        float distance = Mathf.Abs(targetY - startY);
        float duration = distance / speed;

        isScrolling = true;

        LeanTween.value(gameObject, startY, targetY, duration)
            .setOnUpdate((float val) =>
            {
                Vector2 ap = targetRect.anchoredPosition;
                ap.y = val;
                targetRect.anchoredPosition = ap;
            })
            .setEase(LeanTweenType.linear)
            .setUseEstimatedTime(true) // uses unscaled time so it runs when timeScale == 0
            .setOnComplete(() =>
            {
                SetYAndLoop(resetY);
            });
    }

    private void SetYAndLoop(float y)
    {
        LeanTween.cancel(gameObject);
        Vector2 ap = targetRect.anchoredPosition;
        ap.y = y;
        targetRect.anchoredPosition = ap;

        isScrolling = false;

        LeanTween.delayedCall(gameObject, restartDelay, () =>
        {
            StartScrolling();
        }).setUseEstimatedTime(true);
    }

    public void StopScrolling()
    {
        LeanTween.cancel(gameObject);
        isScrolling = false;
    }
}
