using UnityEngine;

/// <summary>Distingue pisotón, daño lateral y derrota por estrella o caparazón.</summary>
public class Goomba : MonoBehaviour
{
    public Sprite flatSprite;
    private bool defeated;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!defeated && collision.gameObject.CompareTag("Player") && collision.gameObject.TryGetComponent(out Player player))
        {
            if (player.starpower) {
                Hit();
            } else if (collision.HasContactNormal(Vector2.down)) {
                Flatten();
            } else {
                player.Hit();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!defeated && other.gameObject.layer == LayerMask.NameToLayer("Shell")) {
            Hit();
        }
    }

    private void Flatten()
    {
        defeated = true;
        GameAudio.Instance?.Play(SoundEffect.Stomp);
        GetComponent<Collider2D>().enabled = false;
        GetComponent<EntityMovement>().SetMovementAllowed(false);
        GetComponent<AnimatedSprite>().enabled = false;
        GetComponent<SpriteRenderer>().sprite = flatSprite;
        Destroy(gameObject, 0.5f);
    }

    private void Hit()
    {
        if (defeated) return;
        defeated = true;
        GameAudio.Instance?.Play(SoundEffect.Kick);
        GetComponent<AnimatedSprite>().enabled = false;
        GetComponent<DeathAnimation>().enabled = true;
        Destroy(gameObject, 3f);
    }

}
