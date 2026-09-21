using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One clickable choice card (used on the hero select screen).
public class UpgradeCard : MonoBehaviour
{
    [SerializeField] Button button;
    [SerializeField] TMP_Text title;
    [SerializeField] TMP_Text description;
    [SerializeField] TMP_Text footer;

    public Button Button => button;

    public void Show(string titleText, string descriptionText, string footerText)
    {
        title.text = titleText;
        description.text = descriptionText;
        footer.text = footerText;
    }
}
