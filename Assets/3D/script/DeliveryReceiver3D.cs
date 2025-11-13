using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Project.Gameplay3D
{
    [RequireComponent(typeof(Collider))]
    public class DeliveryReceiver3D : MonoBehaviour
    {
        [Header("Accept Rule")]
        public string acceptName = "Red";

        [Header("Scoring (Same as 2D)")]
        public int baseScore = 10;
        public int multiStepBonus = 2;
        public int wrongPenalty = 30;

        [Header("UI Feedback")]
        public TextMeshPro textPrefab;
        public Color positiveScoreColor = new Color(1f, 0f, 0.91f);
        public Color negativeScoreColor = Color.red;
        public Vector3 uiOffset = new Vector3(0, 2f, 0);
        private Camera mainCam;

        [Header("Receiver Animation")]
        public float squashScale = 0.85f;
        public float squashDuration = 0.3f;
        public float bounceScale = 1.1f;
        public float bounceDuration = 0.15f;
        public float returnDuration = 0.1f;

        [Header("Delivered Item Shrink")]
        [Tooltip("Duration for the per-item shrink tween before the cargo is marked Delivered")]
        public float shrinkDuration = 0.35f;

        Collider col;

        void Awake()
        {
            col = GetComponent<Collider>();
            col.isTrigger = false;

            mainCam = Camera.main;
        }

        void LateUpdate()
        {
            if (!textPrefab) return;
            textPrefab.transform.position = transform.position + uiOffset;
            if (mainCam)
                textPrefab.transform.rotation = Quaternion.LookRotation(mainCam.transform.forward);
        }

        private void ShowFloatingScore(int amount, Color color)
        {
            if (!textPrefab) return;

            var clone = Instantiate(textPrefab, textPrefab.transform.parent);
            clone.text = (amount >= 0 ? $"+{amount}" : $"{amount}");
            clone.color = color;
            clone.alpha = 1f;

            Vector3 startPos = textPrefab.transform.position;
            Vector3 endPos = startPos + new Vector3(0, 2f, 0);
            clone.rectTransform.anchoredPosition = startPos;

            LeanTween.value(clone.gameObject, 0f, 1f, 0.7f)
                .setOnUpdate((float t) =>
                {
                    clone.transform.position = Vector3.Lerp(startPos, endPos, t);
                    clone.alpha = 1f - t;
                })
                .setOnComplete(() => Destroy(clone.gameObject));
        }

        /// <summary>
        /// Delivers matching items from the carrier's stack.
        /// Shrinks each delivered cargo (visual), then marks it Delivered and plays a generic Interact SFX.
        /// </summary>
        public int DeliverFrom(PlayerCarry3D carrier)
        {
            if (carrier == null || carrier.Count == 0) return 0;

            var first = carrier.Peek();
            if (first == null || !IsMatch(first))
            {
                WrongDelivery();
                TweenUtils.WrongDeliveryShake(gameObject, 0.5f);
                return 0;
            }

            int delivered = 0;
            List<Cargo3D> batch = new List<Cargo3D>();

            // collect matching items from top of carrier stack
            while (carrier.Count > 0)
            {
                var top = carrier.Peek();
                if (top == null) { carrier.Pop(); continue; }
                if (!IsMatch(top)) break;

                var deliveredCargo = carrier.Pop();
                if (deliveredCargo.isGrounded)
                    GameManager3D.Instance?.OnBacklogRemove();

                // collect for shrink animation; do NOT SetState here
                batch.Add(deliveredCargo);
                delivered++;
            }

            if (delivered > 0)
            {

                for (int i = 0; i < batch.Count; i++)
                {
                    var cargo = batch[i];
                    if (cargo == null) continue;

                    
                    TweenUtils.ShrinkAndDisable(cargo.gameObject, shrinkDuration, 0f, () =>
                    {
                     
                        cargo.SetState(CargoState3D.Delivered, null);

                                        
                    });
                }

                // batch-level effects and scoring
                DeliverBatch(delivered);

                TweenUtils.ReceiverPopEffect(
                    gameObject,
                    -0.15f, squashDuration,
                    0.15f, bounceDuration,
                    returnDuration
                );
            }

            return delivered;
        }

        private bool IsMatch(Cargo3D c)
        {
            if (!c) return false;
            return c.cargoName == acceptName;
        }

        public void DeliverBatch(int n)
        {
            if (n <= 0) return;

            // existing receiver sound (keeps previous behavior)
            SoundManager.Instance?.PlaySound(SoundType.RightReciever, null, 0.35f);

            int multiBonusPerItem = (n - 1) * multiStepBonus;

            if (!ScoreManager.Instance)
            {
                Debug.LogWarning("DeliveryReceiver3D: ScoreManager missing");
                return;
            }

            int currentGlobalCombo = ScoreManager.Instance.GetCombo() + 1;
            int comboBonus = GetComboBonus(currentGlobalCombo);

            int totalScore = 0;
            for (int i = 1; i <= n; i++)
            {
                int single = baseScore + comboBonus + multiBonusPerItem;
                ScoreManager.Instance.AddScore(single);
                totalScore += single;
            }

            ShowFloatingScore(totalScore, positiveScoreColor);

            ScoreManager.Instance.IncreaseCombo();
        }

        private void WrongDelivery()
        {
            SoundManager.Instance?.PlaySound(SoundType.WrongReciever, null, 0.35f);
            ScoreManager.Instance?.AddScore(-wrongPenalty);
            ScoreManager.Instance?.ResetCombo("wrong delivery");

            ShowFloatingScore(-wrongPenalty, negativeScoreColor);
        }

        private int GetComboBonus(int comboIndex)
        {
            if (comboIndex <= 3) return 0;
            if (comboIndex <= 6) return 5;
            return 10;
        }
    }
}
