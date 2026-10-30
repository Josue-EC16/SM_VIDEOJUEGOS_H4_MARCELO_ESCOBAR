using System.Collections;
using UnityEngine;

/// <summary>Finaliza una sola vez el nivel antes de solicitar la carga del siguiente.</summary>
public class FlagPole : MonoBehaviour
{
    public Transform flag;
    public Transform poleBottom;
    public Transform castle;
    public float speed = 6f;
    public int nextWorld = 1;
    public int nextStage = 1;
    private bool completing;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (completing || !other.CompareTag("Player") || !other.TryGetComponent(out Player player)) return;
        if (player.dead || player.IsTransitioning || GameManager.Instance.IsResetPending) return;
        completing = true;
        StartCoroutine(MoveTo(flag, poleBottom.position));
        StartCoroutine(LevelCompleteSequence(player));
    }

    private IEnumerator LevelCompleteSequence(Player player)
    {
        player.SetTransitioning(true);
        GameAudio.Instance?.LevelCompleted();
        yield return MoveTo(player.transform, poleBottom.position);
        yield return MoveTo(player.transform, player.transform.position + Vector3.right);
        yield return MoveTo(player.transform, player.transform.position + Vector3.right + Vector3.down);
        yield return MoveTo(player.transform, castle.position);
        // Se mantienen los datos del jugador disponibles para capturar su estado.
        foreach (SpriteRenderer renderer in player.GetComponentsInChildren<SpriteRenderer>()) renderer.enabled = false;
        yield return new WaitForSeconds(2f);
        GameManager.Instance.LoadLevel(nextWorld, nextStage);
    }

    private IEnumerator MoveTo(Transform subject, Vector3 position)
    {
        while (Vector3.Distance(subject.position, position) > 0.025f)
        {
            subject.position = Vector3.MoveTowards(subject.position, position, speed * Time.deltaTime);
            yield return null;
        }
        subject.position = position;
    }
}
