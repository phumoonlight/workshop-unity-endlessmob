using UnityEngine;

// A district building with health. At 0 HP it turns into a ruin and stops working.
// A ruin must be repaired all the way to full HP before it works again.
public class Building : Structure
{
    [SerializeField] string displayName = "Building";

    [Tooltip("The normal look. Hidden while ruined.")]
    [SerializeField] GameObject intactVisuals;

    [Tooltip("The rubble look. Shown only while ruined.")]
    [SerializeField] GameObject ruinVisuals;

    [Tooltip("Scripts that make the building work (e.g. Tower, Barrack). Turned off while ruined.")]
    [SerializeField] Behaviour[] disableWhenRuined;

    public bool IsRuined { get; private set; }

    public override string DisplayName => IsRuined ? $"the ruined {displayName.ToLower()}" : $"the {displayName.ToLower()}";
    public string Name => displayName;
    public override bool IsTargetable => !IsRuined;

    protected override void Awake()
    {
        base.Awake(); // run Structure's Awake too (sets health)
        SetRuined(false);
    }

    protected override void OnHealthDepleted() => SetRuined(true);

    protected override void OnFullyRepaired()
    {
        if (IsRuined)
            SetRuined(false);
    }

    void SetRuined(bool ruined)
    {
        IsRuined = ruined;
        if (intactVisuals != null) intactVisuals.SetActive(!ruined);
        if (ruinVisuals != null) ruinVisuals.SetActive(ruined);
        foreach (Behaviour b in disableWhenRuined)
            if (b != null) b.enabled = !ruined;
    }
}
