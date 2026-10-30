using System.Collections;
using UnityEngine;

/// <summary>Traslado local con animaci?n y bloqueo de reentrada; conserva los destinos originales.</summary>
public class Pipe : MonoBehaviour
{
    public Transform connection;
    public KeyCode enterKeyCode = KeyCode.S;
    public Vector3 enterDirection = Vector3.down;
    public Vector3 exitDirection = Vector3.zero;
    private bool entering;

    private void OnTriggerStay2D(Collider2D other)
    {
        if (entering || connection == null || !other.CompareTag("Player") || !Input.GetKey(enterKeyCode)) return;
        if (other.TryGetComponent(out Player player)) TryEnter(player);
    }

    public bool TryEnter(Player player)
    {
        if (entering || connection == null || player.dead || player.IsTransitioning || GameManager.Instance.IsResetPending) return false;
        // También las llamadas de pruebas deben respetar el área física de la entrada.
        if (!GetComponent<Collider2D>().bounds.Intersects(player.capsuleCollider.bounds)) return false;
        entering = true;
        StartCoroutine(Enter(player));
        return true;
    }

    private IEnumerator Enter(Player player)
    {
        player.SetTransitioning(true);
        GameAudio.Instance?.Play(SoundEffect.Pipe);
        SpriteRenderer[] renderers = player.GetComponentsInChildren<SpriteRenderer>();
        int[] orders = new int[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) { orders[i] = renderers[i].sortingOrder; renderers[i].sortingOrder = -1; }
        yield return Move(player.transform, transform.position + enterDirection, 0.6f);
        yield return new WaitForSeconds(0.15f);
        player.transform.position = connection.position - exitDirection;
        SideScrollingCamera camera = Camera.main.GetComponent<SideScrollingCamera>();
        if (connection.TryGetComponent(out PipeRegion region)) region.Apply(camera);
        else camera.SetUnderground(connection.position.y < camera.undergroundThreshold);
        if (exitDirection != Vector3.zero)
        {
            GameAudio.Instance?.Play(SoundEffect.Pipe);
            yield return Move(player.transform, connection.position + exitDirection, 0.6f);
        }
        for (int i = 0; i < renderers.Length; i++) renderers[i].sortingOrder = orders[i];
        player.GetComponent<Rigidbody2D>().position = player.transform.position;
        Physics2D.SyncTransforms();
        player.SetTransitioning(false);
        entering = false;
    }

    private IEnumerator Move(Transform player, Vector3 destination, float duration)
    {
        Vector3 start = player.position;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            player.position = Vector3.Lerp(start, destination, elapsed / duration);
            yield return null;
        }
        player.position = destination;
    }
}
