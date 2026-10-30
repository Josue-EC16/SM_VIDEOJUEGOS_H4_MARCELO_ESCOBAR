using UnityEngine;

/// <summary>Resuelve las caídas y retira objetos, sin destruir los ascensores reciclables.</summary>
[RequireComponent(typeof(BoxCollider2D))]
public class DeathBarrier : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (other.TryGetComponent(out Player player)) player.Fall();
        }
        else
        {
            // Los ascensores se reciclan por debajo del escenario.
            if (other.GetComponent<MovingPlatform>() == null) Destroy(other.gameObject);
        }
    }

}
