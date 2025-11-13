using Assets.script;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class DeliveryReceiver : MonoBehaviour, IInteractable
{
    [Header("Config")]
    [SerializeField] public string acceptName;   // ✅ 仅用 name 判定
    [SerializeField] public int baseScore = 10;
    [SerializeField] public int multiStepBonus = 2;
    [SerializeField] public int wrongPenalty = 30;

    [Header("Close Port Settings")]
    [SerializeField] private float baseCloseSec = 5f;   // 初次关闭时长
    [SerializeField] private float closeIncSec = 3f;    // 每次错误递增时长

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI textPrefab;
    [SerializeField] private Color positiveScoreColor = new Color(1f, 0f, 0.91f);
    [SerializeField] private Color negativeScoreColor = Color.red;

    [SerializeField] private Vector3 uiOffset = new Vector3(0, 2f, 0);

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;
    [SerializeField] private bool logPerItem = false;

    //private int comboCount = 0;
    private bool isClosed = false;
    private float reopenAt = 0f;
    private int wrongCount = 0;
    private Camera mainCam;

    [SerializeField]
    private SpriteRenderer sr;

    [Header("Receiver Pop Effect Settings")]
    private float squashScale = 0.85f;
    private float squashDuration = 0.3f;
    private float bounceScale = 1.1f;
    private float bounceDuration = 0.15f;
    private float returnDuration = 0.1f;


    [SerializeField]
    private ParticleSystem successEffect;

    private void Start()
    {
        mainCam = Camera.main;
    }

    private void Update()
    {
        // ✅ 先检查是否到时间自动开口
        if (isClosed && Time.time >= reopenAt)
            isClosed = false;

        // ✅ 更新文字内容（含倒计时）
        if (textPrefab != null)
        {
            if (isClosed)
            {
                float remain = Mathf.Max(0, reopenAt - Time.time);
                textPrefab.text = $"<color=red>{remain:0.0}s</color>";
            }
            else
            {
                textPrefab.text = "";
            }
        }
    }

    private void LateUpdate()
    {
        // ✅ 让文字始终位于物体上方并朝向相机
        if (textPrefab != null)
        {
            textPrefab.transform.position = transform.position + uiOffset;
            if (mainCam != null)
                textPrefab.transform.rotation = Quaternion.LookRotation(mainCam.transform.forward);
        }
    }
    private void ShowFloatingScore(int amount, Color color)
    {
        if (textPrefab == null) return;

        // ✅ 克隆一份独立文字（不会被 Update 清空）
        var clone = Instantiate(textPrefab, textPrefab.transform.parent);
        clone.text = (amount >= 0 ? $"+{amount}" : $"{amount}");
        clone.color = color;
        clone.alpha = 1f;

        // ✅ 初始位置在原 textPrefab 上方
        Vector3 startPos = textPrefab.rectTransform.anchoredPosition + new Vector2(0, 30f);
        Vector3 endPos = startPos + new Vector3(0, 40f, 0);
        clone.rectTransform.anchoredPosition = startPos;

        // ✅ 播放动画：上浮 + 淡出 + 销毁
        LeanTween.value(clone.gameObject, 0f, 1f, 0.7f)
            .setOnUpdate((float t) =>
            {
                clone.rectTransform.anchoredPosition = Vector3.Lerp(startPos, endPos, t);
                clone.alpha = 1f - t;
            })
            .setOnComplete(() => Destroy(clone.gameObject));
    }

    public string GetInteractPrompt()
    {
        if (isClosed) return $"{acceptName} (Closed)";
        return $"Press F to deliver to {acceptName}";
    }

    public void Interact(controller player, bool isHold)
    {
        Debug.Log($"[DeliveryReceiver] Interact called | isHold={isHold} | cargoCount={player?.cargoStack?.Count}");
        if (isClosed) return;
        if (player == null || player.cargoStack == null || player.cargoStack.Count == 0) return;

       
        DeliverCombo(player);
        
    }

    

    /// <summary>
    /// 改造后的统一交付逻辑：
    /// 1. 先看手上第一个是否匹配，不匹配直接判错并中断
    /// 2. 如果匹配则一次性交付全部能匹配的物品（一个或多个）
    /// </summary>
    private void DeliverCombo(controller player)
    {
        if (player.cargoStack.Count <= 0) return;

        // 先预判栈顶货物是否匹配
        GameObject firstGO = player.cargoStack.Peek();
        Cargo firstCargo = firstGO != null ? firstGO.GetComponent<Cargo>() : null;
        if (firstCargo == null) return;

        bool firstIsUniversal = (firstCargo.cargoType == CargoType.Universality);
        if (!firstIsUniversal && firstCargo.cargoName != acceptName)
        {
            // 一旦第一个就错，直接错误处理
            TweenUtils.WrongDeliveryShake(this.gameObject, 0.5f);
            WrongDelivery();
            return;
        }

        int delivered = 0;
        var deliveredItems = new List<GameObject>();

        // 从栈顶开始批量交付，只要匹配就一直交付
        while (player.cargoStack.Count > 0)
        {
            GameObject cargoGO = player.cargoStack.Peek();
            Cargo cargo = cargoGO != null ? cargoGO.GetComponent<Cargo>() : null;
            if (cargo == null) break;

            bool isUniversal = (cargo.cargoType == CargoType.Universality);
            if (!isUniversal && cargo.cargoName != acceptName)
                break; // 遇到不匹配就停

            deliveredItems.Add(cargoGO);
            CreateComboEffect(deliveredItems);
            player.cargoStack.Pop();
            player.carryUI.Pop();
            GameManager.Instance.OnBacklogRemove();

            cargo.SetState(CargoState.Delivered);
            delivered++;
        }
        Debug.Log($"delivered: {delivered}");
        if (delivered > 0)
        {
            DeliverBatch(delivered);
            TweenUtils.ReceiverPopEffect(gameObject, -0.15f, squashDuration, 0.15f, bounceDuration, returnDuration);
        }
    }

    /// <summary>
    /// 批次结算：每成功一批连击+1；多件叠加 multiStepBonus；combo 分段加成
    /// </summary>
    public void DeliverBatch(int n)
    {
        if (n <= 0) return;

        SoundManager.Instance?.PlaySound(SoundType.RightReciever, null, 0.35f);

        int multiBonusPerItem = (n - 1) * multiStepBonus;

        successEffect?.Play();

        if (debugLogs)
            Debug.Log($"[DeliveryReceiver] >>> DeliverBatch START | n={n}");

        int currentGlobalCombo = ScoreManager.Instance.GetCombo() + 1;
        int comboBonus = GetComboBonus(currentGlobalCombo);

        int totalScore = 0;
        for (int i = 1; i <= n; i++)
        {
            int single = baseScore + comboBonus + multiBonusPerItem;
            ScoreManager.Instance?.AddScore(single);
            totalScore += single;
        }

        ShowFloatingScore(totalScore, positiveScoreColor); //  custom positive color 
        // ✅ 用 ScoreManager 统一管理全局 combo
        ScoreManager.Instance?.IncreaseCombo();

        if (debugLogs)
            Debug.Log($"[DeliveryReceiver] <<< DeliverBatch END | globalCombo={ScoreManager.Instance.GetCombo()}");
   
    }

 



    private GameObject CreateEffectCopy(GameObject original)
    {
        // create a new gameObject just for the effect
        var sr = original.GetComponentInChildren<SpriteRenderer>();
        GameObject copy = new GameObject("DeliveryEffect_" + original.name);
        var newSr = copy.AddComponent<SpriteRenderer>();
        if (sr) { newSr.sprite = sr.sprite; newSr.color = sr.color; }
        copy.transform.position = transform.position + Vector3.up * 0.5f;
        return copy;
    }

    private void CreateComboEffect(List<GameObject> deliveredItems)
    {
        float delay = 0f;
        foreach (var cargo in deliveredItems)
        {
            GameObject effectCopy = CreateEffectCopy(cargo);
            effectCopy.transform.position = transform.position + Vector3.up * 0.5f + new Vector3(Random.Range(-0.3f, 0.3f), 0, 0);

            TweenUtils.ComboDeliveryEffect(effectCopy, 1.2f, 0.8f, delay);
            Destroy(effectCopy, 1.5f + delay);
            delay += 0.1f;
        }
    }

    /// <summary>错误投递：扣分 + 清空连击 + 关闭接收口</summary>
    private void WrongDelivery()
    {
        SoundManager.Instance?.PlaySound(SoundType.WrongReciever, null, 0.35f);
        ScoreManager.Instance?.AddScore(-wrongPenalty);
        ScoreManager.Instance?.ResetCombo("wrong delivery");

        ShowFloatingScore(-wrongPenalty, negativeScoreColor); //custom negative color 

        ClosePort();
    }

    public void OnCargoBroken()
    {
        ScoreManager.Instance?.ResetCombo("cargo broken");
        
    }


    private void ClosePort()
    {
        wrongCount++;
        float closeTime = baseCloseSec + (wrongCount - 1) * closeIncSec;
        isClosed = true;
        reopenAt = Time.time + closeTime;

        if (debugLogs)
            Debug.Log($"[DeliveryReceiver] Port closed {closeTime:F1}s (wrongCount={wrongCount})");
    }

    private int GetComboBonus(int comboIndex)
    {
        if (comboIndex <= 3) return 0;
        if (comboIndex <= 6) return 5;
        return 10;
    }
}
