using UnityEngine;

// Applies poison damage while player stays inside.
public class PoisonField : BiomeGimmick
{
    [Header("Damage per second")]
    [SerializeField] float damagePerSecond = 5f;

    protected override void Update()
    {
        // Keep base lifecycle behavior.
        base.Update();
    }

    // No additional spawn logic needed.
    protected override void OnSpawn() { }

    // Trigger logic is handled in OnTriggerStay2D.
    protected override void OnPlayerTrigger(Player player) { }

    void OnTriggerStay2D(Collider2D collision)
    {
        // Ignore non-player colliders.
        if (!collision.CompareTag("Player"))
            return;

        // Apply poison as DoT and bypass i-frames.
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.TakeDamage(
                damagePerSecond * Time.deltaTime,
                applyIFrames: false,
                PlayerDamageKind.PerSecondFrame);
    }
}
