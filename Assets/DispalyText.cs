using UnityEngine;
using TMPro;
using System.Collections.Generic;

    public class DispalyText : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI textField;
        [TextArea(3,8)]
        [SerializeField] private List<string> messages = new List<string>
        {
            "An estimated 27.6 million people worldwide were in forced labour on any given day in 2021.",
            "In 2016, about 1.9 million people died from work-related risks, with the vast majority of these deaths arising from occupational diseases rather than accidents.",
            "Roughly 58% of the global workforce—nearly 2 billion people—are engaged in informal employment.",
            "In Australia, 188 workers died from traumatic incidents in 2024. During 2023–24, there were about 146,700 serious workers’ compensation claims (involving at least one week off work), averaging 400+ claims per day.",
            "Globally, the economic cost of occupational injuries and illnesses is estimated at about 3.94% of GDP each year.",
            "On average, women still earn about 20% less than men globally, though the gap varies substantially by country.",
            "Globally, over one-third of workers regularly put in more than 48 hours per week, while roughly one-fifth work short (part-time) hours below 35 per week—both ends of the spectrum with big implications for well-being and productivity.",
            "Despite progress since 2020, working poverty still affects millions: the global rate stood at about 6.9% in 2023, equivalent to roughly 241 million workers.",
            "The online gig economy is far larger than once thought, with an estimated 154 million to 435 million people doing online gig work worldwide—offering income opportunities but often without standard protections."
        };

        private void Awake()
        {
          //// if (textField == null)
                textField = GetComponent<TextMeshProUGUI>();
            ///if (textField != null)
               /// textField.gameObject.SetActive(false);
        }

        public void ShowRandomMessage()
        {
            if (textField == null || messages == null || messages.Count == 0) return;
            int idx = Random.Range(0, messages.Count);
            textField.text = messages[idx];
            textField.gameObject.SetActive(true);
        }

        public void Hide()
        {
            //if (textField != null)
            //    textField.gameObject.SetActive(false);
        }
}
