using Febucci.UI;
using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    private int score = 0;
    private int comboCount = 0;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI comboText;


    //private Febucci.UI.Core.TAnimCore textAnimator;
    [SerializeField]
    private Febucci.UI.TypewriterByCharacter scoreTypewriter;

    [SerializeField]
    private Febucci.UI.TypewriterByCharacter comboTypewriter;



    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        UpdateUI(false);
    }

    /// <summary>仅加分，不再自增连击</summary>
    public void AddScore(int amount)
    {
        score += amount;
        Debug.Log($"[ScoreManager] +{amount} 分, 总分 = {score}");
        UpdateUI(true);
    }

    /// <summary>由外部（如 DeliveryReceiver）设置当前连击数</summary>
    public void SetCombo(int combo)
    {
        comboCount = combo;
        Debug.Log($"[ScoreManager] Combo 设置为 {comboCount}");
        UpdateUI(false);
    }
    public void IncreaseCombo()
    {
        comboCount++;
        Debug.Log($"[ScoreManager] Combo +1 = {comboCount}");
        UpdateUI(false);
    }

    /// <summary>重置总分</summary>
    public void ResetScore()
    {
        score = 0;
        UpdateUI(false);
    }

    /// <summary>重置连击</summary>
    public void ResetCombo(string reason = "")
    {
        if (!string.IsNullOrEmpty(reason))
            Debug.Log($"[ScoreManager] Combo reset: {reason}");
        TweenUtils.ShowText(reason, Vector3.zero, 1.5f, GradientType.Hit, FontType.Combo);
        comboCount = 0;
        UpdateUI(false);
    }

    public int GetScore() => score;
    public int GetCombo() => comboCount;

    private void UpdateUI(bool bounceScore)
    {
        if (scoreText != null)
        {
            string text = bounceScore ? $"<bounce>{score}</bounce>" : score.ToString();
            scoreTypewriter.ShowText(text);
        }

        if (comboText != null)
        {
            string text = $"<incr>COMBO x{comboCount}</incr>";
            comboTypewriter.ShowText(text);
        }
    }

}
