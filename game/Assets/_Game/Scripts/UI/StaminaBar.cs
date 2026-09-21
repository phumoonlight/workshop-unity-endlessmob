using UnityEngine;
using UnityEngine.UI;

// A thin bar showing stamina. Fades out when full so it only shows when it matters.
[RequireComponent(typeof(CanvasGroup))]
public class StaminaBar : MonoBehaviour
{
    [SerializeField] PlayerStamina stamina;
    [SerializeField] RectTransform fill;
    [SerializeField] Image fillImage;

    [SerializeField] Color normalColor = new Color(1f, 0.85f, 0.25f);
    [SerializeField] Color exhaustedColor = new Color(1f, 0.3f, 0.25f);

    [Tooltip("How fast the bar fades in and out.")]
    [SerializeField] float fadeSpeed = 4f;

    CanvasGroup group; // lets us fade the whole bar at once

    void Awake()
    {
        group = GetComponent<CanvasGroup>();
        group.alpha = 0f;
    }

    void Update()
    {
        float fraction = stamina.Stamina / stamina.MaxStamina;
        fill.anchorMax = new Vector2(fraction, 1f);
        fillImage.color = stamina.IsExhausted ? exhaustedColor : normalColor;

        bool show = fraction < 0.999f || stamina.IsSprinting;
        group.alpha = Mathf.MoveTowards(group.alpha, show ? 1f : 0f, fadeSpeed * Time.unscaledDeltaTime);
    }
}
