using System.Collections;
using UnityEngine;

/// <summary>Recompensa o rotura al recibir un contacto desde debajo del bloque.</summary>
public class BlockHit : MonoBehaviour
{
    public GameObject item;
    public Sprite emptyBlock;
    public int maxHits = -1;
    [Tooltip("Variante subterránea: Mario grande puede romper un ladrillo sin recompensa.")]
    public bool breakable;
    private bool animating;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!animating && maxHits != 0 && collision.gameObject.CompareTag("Player"))
        {
            if (collision.HasContactNormal(Vector2.up)) {
                if (breakable && item == null && collision.gameObject.TryGetComponent(out Player player) && player.big)
                {
                    Break();
                    return;
                }
                Hit();
            }
        }
    }

    private void Hit()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        GameAudio.Instance?.Play(SoundEffect.Bump);
        spriteRenderer.enabled = true; // show if hidden

        maxHits--;

        if (maxHits == 0) {
            spriteRenderer.sprite = emptyBlock;
        }

        if (item != null) {
            Instantiate(item, transform.position, Quaternion.identity);
        }

        StartCoroutine(Animate());
    }

    private void Break()
    {
        GameAudio.Instance?.Play(SoundEffect.Break);
        SpriteRenderer source = GetComponent<SpriteRenderer>();
        // Fragmentos físicos del recurso existente; no necesitan imágenes nuevas.
        for (int i = 0; i < 4; i++)
        {
            GameObject fragment = new GameObject("Fragmento de ladrillo");
            fragment.transform.position = transform.position + new Vector3(i % 2 == 0 ? -0.2f : 0.2f, i < 2 ? 0.2f : -0.2f);
            fragment.transform.localScale = Vector3.one * 0.45f;
            SpriteRenderer renderer = fragment.AddComponent<SpriteRenderer>();
            renderer.sprite = source.sprite;
            renderer.color = source.color;
            renderer.sortingOrder = source.sortingOrder;
            Rigidbody2D body = fragment.AddComponent<Rigidbody2D>();
            body.gravityScale = 3f;
            body.linearVelocity = new Vector2(i % 2 == 0 ? -3f : 3f, i < 2 ? 9f : 5f);
            body.angularVelocity = i % 2 == 0 ? 180f : -180f;
            Destroy(fragment, 1.5f);
        }
        Destroy(gameObject);
    }

    private IEnumerator Animate()
    {
        animating = true;

        Vector3 restingPosition = transform.localPosition;
        Vector3 animatedPosition = restingPosition + Vector3.up * 0.5f;

        yield return Move(restingPosition, animatedPosition);
        yield return Move(animatedPosition, restingPosition);

        animating = false;
    }

    private IEnumerator Move(Vector3 from, Vector3 to)
    {
        float elapsed = 0f;
        float duration = 0.125f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;

            transform.localPosition = Vector3.Lerp(from, to, t);
            elapsed += Time.deltaTime;

            yield return null;
        }

        transform.localPosition = to;
    }

}
