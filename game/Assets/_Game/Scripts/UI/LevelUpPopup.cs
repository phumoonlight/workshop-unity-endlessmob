using TMPro;
using UnityEngine;

// A "LEVEL UP!" message that pops in, stays for a moment, then fades out.
public class LevelUpPopup : MonoBehaviour
{
    [SerializeField] TMP_Text text;
    [SerializeField] float showSeconds = 2f;
    [SerializeField] float fadeSeconds = 0.5f;

    float timeLeft;

    void Awake()
    {
        text.alpha = 0f; // hidden until the first level up
    }

    public void Show(int level, float attackPercent, float hpGained)
    {
        ShowMessage($"STAGE LEVEL UP!  <size=70%>LV {level}</size>",
                    $"<color=#ffb060>ATK +{attackPercent:0.#}%</color>     " +
                    $"<color=#80ff80>MAX HP +{hpGained:0.#}</color>     HP fully restored");
    }

    // Any big announcement: a title line and a smaller line under it.
    public void ShowMessage(string title, string subtitle)
    {
        text.text = $"{title}\n<size=50%>{subtitle}</size>";
        timeLeft = showSeconds;
    }

    void Update()
    {
        if (timeLeft <= 0f)
            return;

        // unscaledDeltaTime keeps counting even if the game is paused.
        timeLeft -= Time.unscaledDeltaTime;

        // Fully visible, then fade out during the last fadeSeconds.
        text.alpha = Mathf.Clamp01(timeLeft / fadeSeconds);

        // A little "pop": start 30% bigger and shrink to normal size quickly.
        float age = showSeconds - timeLeft;
        transform.localScale = Vector3.one * Mathf.Lerp(1.3f, 1f, age / 0.15f);
    }
}
