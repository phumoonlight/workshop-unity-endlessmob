using TMPro;
using UnityEngine;

// The bar across the top of the screen that fills as you collect XP.
public class XpBar : MonoBehaviour
{
    [SerializeField] PlayerExperience experience;

    [Tooltip("The colored part of the bar. We stretch it from 0% to 100% of the width.")]
    [SerializeField] RectTransform fill;

    [SerializeField] TMP_Text levelLabel;

    int shownLevel = -1;

    void Update()
    {
        float progress = (float)experience.CurrentXp / experience.XpToNextLevel;
        fill.anchorMax = new Vector2(progress, 1f);

        // Only rebuild the text when the level actually changes.
        if (experience.Level != shownLevel)
        {
            shownLevel = experience.Level;
            levelLabel.text = "Stage LV " + shownLevel;
        }
    }
}
