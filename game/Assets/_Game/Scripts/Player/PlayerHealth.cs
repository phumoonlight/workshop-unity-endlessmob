using System;
using System.Collections.Generic;
using UnityEngine;

// Tracks the player's health. When it reaches zero the player disappears
// for a few seconds, then respawns at the castle with full health.
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] float maxHealth = 100f;

    [Tooltip("Seconds to wait before respawning.")]
    [SerializeField] float respawnDelay = 5f;

    [Tooltip("Where the player comes back after dying.")]
    [SerializeField] Transform respawnPoint;

    [Tooltip("On: dying ends the run, like Vampire Survivors. Off: respawn after a few seconds and keep going.")]
    [SerializeField] bool deathEndsRun = true;

    [Header("Revive (relics)")]
    [Tooltip("Health after a relic brings you back, as a fraction of max. 0.5 = half.")]
    [Range(0.05f, 1f)]
    [SerializeField] float reviveHealth = 0.5f;

    [Tooltip("Seconds you can't be hurt after reviving, so the crowd that killed you can't do it again at once.")]
    [SerializeField] float reviveInvulnerableSeconds = 2f;

    // The run ended for good. RunBank banks the coins, items and XP.
    public event Action Defeated;

    // A relic was used up to get back up. Carries the item, for the message.
    public event Action<ItemData> Revived;

    public float Health { get; private set; }
    public float MaxHealth => maxHealth;
    public bool IsDead { get; private set; }
    public float RespawnTimeLeft { get; private set; }

    CharacterController controller;
    PlayerController movement;
    Inventory inventory;
    float invulnerableUntil;
    Weapon[] weapons;
    readonly List<Weapon> weaponsToRestore = new List<Weapon>();
    readonly List<Renderer> renderersToRestore = new List<Renderer>();

    float baseMaxHealth; // max HP before any level ups

    void Awake()
    {
        baseMaxHealth = maxHealth;
        Health = maxHealth;
        controller = GetComponent<CharacterController>();
        movement = GetComponent<PlayerController>();
        inventory = GetComponent<Inventory>();
        weapons = GetComponents<Weapon>();
    }

    void Update()
    {
        // After a game over the respawn timer must not run: it starts at 0, so
        // it would "finish" on the very first frame and bring the hero back.
        if (!IsDead || GameStats.IsGameOver)
            return;

        RespawnTimeLeft -= Time.deltaTime;
        if (RespawnTimeLeft <= 0f)
            Respawn();
    }

    public bool IsInvulnerable => Time.time < invulnerableUntil;

    public void TakeDamage(float amount)
    {
        if (IsDead || IsInvulnerable)
            return;

        GameAudio.Play(GameAudio.Sfx.PlayerHurt);
        Health = Mathf.Max(0f, Health - amount);
        if (Health <= 0f)
            Die();
    }

    // Used when a class is picked: sets max HP and fills it up.
    public void SetBaseMaxHealth(float amount)
    {
        baseMaxHealth = amount;
        maxHealth = amount;
        Health = amount;
    }

    // Raises max HP by a flat amount, and fills it. Used by armor carried into a run.
    public void AddMaxHealth(float amount)
    {
        maxHealth += amount;
        Health += amount;
    }

    // Raises max HP by a percent of the starting max HP. Returns how much was added.
    public float AddMaxHealthPercentOfBase(float percent)
    {
        float amount = baseMaxHealth * percent / 100f;
        maxHealth += amount;
        return amount;
    }

    // Puts health back on, never above the maximum. Returns how much was
    // actually restored, so a full-health player can be told "that did nothing"
    // instead of quietly eating the berry.
    public float Heal(float amount)
    {
        if (IsDead || amount <= 0f)
            return 0f;

        float before = Health;
        Health = Mathf.Min(maxHealth, Health + amount);
        return Health - before;
    }

    public void HealToFull()
    {
        if (!IsDead)
            Health = maxHealth;
    }

    void Die()
    {
        if (TryRevive())
            return;

        IsDead = true;
        SetAlive(false);

        if (deathEndsRun)
        {
            // The run is over: freeze everything and let the HUD show the result.
            GameStats.IsGameOver = true;
            GameStats.SurvivedSeconds = Time.timeSinceLevelLoad;
            Time.timeScale = 0f;
            Defeated?.Invoke();
            return;
        }

        RespawnTimeLeft = respawnDelay;
    }

    // Uses up a carried item that revives (a relic) and gets straight back up,
    // right where you fell. Returns false if there was nothing to use.
    bool TryRevive()
    {
        if (inventory == null)
            return false;

        ItemData used = inventory.RemoveOneWhere(item => item.revivesOnDeath);
        if (used == null)
            return false;

        Health = maxHealth * reviveHealth;
        invulnerableUntil = Time.time + reviveInvulnerableSeconds;
        Revived?.Invoke(used);
        return true;
    }

    void Respawn()
    {
        // The CharacterController is still disabled here, which lets us teleport.
        // (An enabled CharacterController would fight the position change.)
        if (respawnPoint != null)
            transform.position = respawnPoint.position;

        Health = maxHealth;
        IsDead = false;
        SetAlive(true);
    }

    // Hide/show the player and turn movement and attacks off/on.
    // Only things that were on before dying get turned back on, so the
    // unused class's weapon and prop stay off.
    void SetAlive(bool alive)
    {
        controller.enabled = alive;
        movement.enabled = alive;

        if (!alive)
        {
            weaponsToRestore.Clear();
            foreach (Weapon w in weapons)
                if (w.enabled) { w.enabled = false; weaponsToRestore.Add(w); }

            renderersToRestore.Clear();
            foreach (Renderer r in GetComponentsInChildren<Renderer>())
                if (r.enabled) { r.enabled = false; renderersToRestore.Add(r); }
        }
        else
        {
            foreach (Weapon w in weaponsToRestore) w.enabled = true;
            foreach (Renderer r in renderersToRestore) r.enabled = true;
        }
    }
}
