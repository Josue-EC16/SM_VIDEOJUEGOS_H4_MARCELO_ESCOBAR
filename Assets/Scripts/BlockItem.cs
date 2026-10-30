using System.Collections;
using UnityEngine;

/// <summary>Oculta física y recogida hasta completar la salida del objeto desde el bloque.</summary>
public class BlockItem : MonoBehaviour
{
    private void Start()
    {
        StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        Rigidbody2D rigidbody = GetComponent<Rigidbody2D>();
        CircleCollider2D physicsCollider = GetComponent<CircleCollider2D>();
        BoxCollider2D triggerCollider = GetComponent<BoxCollider2D>();
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        EntityMovement movement = GetComponent<EntityMovement>();
        if (movement != null) movement.SetMovementAllowed(false);
        GameAudio.Instance?.Play(SoundEffect.Item);

        rigidbody.bodyType = RigidbodyType2D.Kinematic;
        physicsCollider.enabled = false;
        triggerCollider.enabled = false;
        spriteRenderer.enabled = false;

        yield return new WaitForSeconds(0.25f);

        spriteRenderer.enabled = true;

        float elapsed = 0f;
        float duration = 0.5f;

        Vector3 startPosition = transform.position;
        Vector3 endPosition = transform.position + Vector3.up;

        while (elapsed < duration)
        {
            float t = elapsed / duration;

            transform.position = Vector3.Lerp(startPosition, endPosition, t);
            elapsed += Time.deltaTime;

            yield return null;
        }

        // Fijar el destino evita depender de la duración del último fotograma.
        transform.position = endPosition;
        rigidbody.bodyType = RigidbodyType2D.Dynamic;
        physicsCollider.enabled = true;
        triggerCollider.enabled = true;
        if (movement != null) movement.SetMovementAllowed(true);
    }

}
