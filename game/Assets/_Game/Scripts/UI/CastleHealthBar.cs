using TMPro;
using UnityEngine;

// The castle's health bar at the top of the screen.
public class CastleHealthBar : MonoBehaviour
{
    [SerializeField] Castle castle;
    [SerializeField] RectTransform fill;
    [SerializeField] TMP_Text label;

    int shownHealth = -1;

    void Update()
    {
        fill.anchorMax = new Vector2(castle.Health / castle.MaxHealth, 1f);

        int health = Mathf.CeilToInt(castle.Health);
        if (health != shownHealth)
        {
            shownHealth = health;
            label.text = $"CASTLE  {health} / {castle.MaxHealth}";
        }
    }
}
