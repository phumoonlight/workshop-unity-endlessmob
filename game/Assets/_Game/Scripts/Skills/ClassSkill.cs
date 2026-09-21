using UnityEngine;

// Base for a class's active skill (press Q). Each skill decides what happens in
// Activate and, for skills that last a while, when it's finished (IsRunning).
// The cooldown starts once the skill has finished.
public abstract class ClassSkill : MonoBehaviour
{
    [SerializeField] string displayName = "Skill";

    [Tooltip("Player level needed to use this skill.")]
    [SerializeField] int unlockLevel = 10;

    [Tooltip("Seconds to wait after the skill ends before it can be used again.")]
    [SerializeField] float cooldown = 10f;

    public string DisplayName => displayName;
    public int UnlockLevel => unlockLevel;
    public float Cooldown => cooldown;
    public float CooldownLeft { get; private set; }

    // True while a skill with a duration (like a spin) is still going.
    public virtual bool IsRunning => false;

    bool wasRunning;

    public bool TryActivate()
    {
        if (CooldownLeft > 0f || IsRunning || !CanActivate())
            return false;
        Activate();
        wasRunning = true;
        if (!IsRunning)
            StartCooldown(); // instant skills start their cooldown right away
        return true;
    }

    // A skill can refuse to fire (e.g. no enemy to throw at), so the key press
    // doesn't waste its cooldown. By default a skill can always fire.
    protected virtual bool CanActivate() => true;

    protected abstract void Activate();

    // Skills that end early (e.g. the player died) call this.
    public virtual void Cancel() { }

    protected virtual void Update()
    {
        // A lasting skill just finished: start the cooldown now.
        if (wasRunning && !IsRunning)
        {
            wasRunning = false;
            if (CooldownLeft <= 0f)
                StartCooldown();
        }

        if (CooldownLeft > 0f)
            CooldownLeft = Mathf.Max(0f, CooldownLeft - Time.deltaTime);
    }

    void StartCooldown() => CooldownLeft = cooldown;
}
