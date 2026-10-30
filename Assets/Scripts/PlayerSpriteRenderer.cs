using UnityEngine;

/// <summary>Selecciona la animación visual sin decidir el tamaño ni la salud de Mario.</summary>
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerSpriteRenderer : MonoBehaviour
{
    private PlayerMovement movement;
    public SpriteRenderer spriteRenderer { get; private set; }
    public Sprite idle;
    public Sprite jump;
    public Sprite slide;
    public AnimatedSprite run;

    private void Awake()
    {
        movement = GetComponentInParent<PlayerMovement>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        if (movement.swimming && !movement.grounded)
        {
            run.enabled = false;
            spriteRenderer.sprite = Mathf.FloorToInt(Time.time * 6f) % 2 == 0 ? jump : idle;
            return;
        }
        run.enabled = movement.running && movement.grounded && !movement.jumping;

        if (movement.jumping || movement.falling) {
            spriteRenderer.sprite = jump;
        } else if (movement.sliding) {
            spriteRenderer.sprite = slide;
        } else if (!movement.running) {
            spriteRenderer.sprite = idle;
        }
    }

    private void OnEnable()
    {
        spriteRenderer.enabled = true;
    }

    private void OnDisable()
    {
        spriteRenderer.enabled = false;
        run.enabled = false;
    }

}
