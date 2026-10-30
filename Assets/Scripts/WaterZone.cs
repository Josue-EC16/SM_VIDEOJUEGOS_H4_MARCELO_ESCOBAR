using UnityEngine;

/// <summary>El agua cambia el movimiento; una tubería puede sacar al jugador a una zona terrestre.</summary>
[RequireComponent(typeof(BoxCollider2D))]
public sealed class WaterZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other) => Apply(other);
    private void OnTriggerStay2D(Collider2D other) => Apply(other);
    private void Apply(Collider2D other)
    {
        if (!other.TryGetComponent(out Player player) || player.IsTransitioning || player.dead) return;
        player.movement.SetSwimming(true);
        GameAudio.Instance?.SetTheme(MusicTheme.Underwater);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent(out Player player) && !player.IsTransitioning)
            player.movement.SetSwimming(false);
    }
}
