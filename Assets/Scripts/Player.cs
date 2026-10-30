using System.Collections;
using UnityEngine;

/// <summary>Valores transferibles; no guarda referencias a objetos de la escena anterior.</summary>
public struct PlayerState
{
    public bool big;
    public float starSeconds;
}

/// <summary>El estado l?gico permanece estable aunque sus sprites est?n parpadeando.</summary>
public class Player : MonoBehaviour
{
    public CapsuleCollider2D capsuleCollider { get; private set; }
    public PlayerMovement movement { get; private set; }
    public DeathAnimation deathAnimation { get; private set; }
    public PlayerSpriteRenderer smallRenderer;
    public PlayerSpriteRenderer bigRenderer;
    public bool big { get; private set; }
    public bool dead { get; private set; }
    public bool starpower => starSeconds > 0f;
    public bool IsTransitioning { get; private set; }
    private float starSeconds;
    private float damageProtection;
    private Coroutine scaleAnimation;

    private void Awake()
    {
        capsuleCollider = GetComponent<CapsuleCollider2D>();
        movement = GetComponent<PlayerMovement>();
        deathAnimation = GetComponent<DeathAnimation>();
        big = bigRenderer.enabled;
    }

    private void Update()
    {
        if (dead || IsTransitioning) return;
        damageProtection = Mathf.Max(0f, damageProtection - Time.deltaTime);
        starSeconds = Mathf.Max(0f, starSeconds - Time.deltaTime);
        Color color = starpower ? Color.HSVToRGB(Mathf.Repeat(Time.time * 2f, 1f), 0.8f, 1f) : Color.white;
        if (damageProtection > 0f && Mathf.FloorToInt(Time.time * 16f) % 2 == 0) color.a = 0.35f;
        smallRenderer.spriteRenderer.color = bigRenderer.spriteRenderer.color = color;
    }

    public PlayerState CaptureState() => new PlayerState { big = big, starSeconds = starSeconds };

    public void RestoreState(PlayerState state)
    {
        CancelTransformation();
        dead = false;
        starSeconds = Mathf.Max(0f, state.starSeconds);
        damageProtection = 0f;
        ApplySize(state.big);
    }

    public void SetTransitioning(bool value)
    {
        IsTransitioning = value;
        movement.enabled = !value && !dead;
    }

    public void Hit()
    {
        if (dead || starpower || IsTransitioning || damageProtection > 0f) return;
        if (big) Shrink();
        else Death();
    }

    public void Death()
    {
        if (dead || IsTransitioning) return;
        dead = true;
        starSeconds = 0f;
        CancelTransformation();
        smallRenderer.spriteRenderer.color = bigRenderer.spriteRenderer.color = Color.white;
        smallRenderer.enabled = bigRenderer.enabled = false;
        deathAnimation.enabled = true;
        GameAudio.Instance?.PlayerDied();
        GameManager.Instance.ResetLevel(3f);
    }

    public void Fall()
    {
        if (dead || IsTransitioning) return;
        Death();
        gameObject.SetActive(false);
    }

    public void Grow()
    {
        if (dead || IsTransitioning || big) return;
        ChangeSize(true);
        GameAudio.Instance?.Play(SoundEffect.PowerUp);
    }

    public void Shrink()
    {
        if (dead || IsTransitioning) return;
        damageProtection = 1f;
        ChangeSize(false);
        GameAudio.Instance?.Play(SoundEffect.Damage);
    }

    private void ApplySize(bool value)
    {
        big = value;
        capsuleCollider.size = new Vector2(1f, value ? 2f : 1f);
        capsuleCollider.offset = new Vector2(0f, value ? 0.5f : 0f);
        smallRenderer.enabled = !value;
        bigRenderer.enabled = value;
    }

    private void ChangeSize(bool value)
    {
        CancelTransformation();
        ApplySize(value);
        scaleAnimation = StartCoroutine(ScaleAnimation());
    }

    private void CancelTransformation()
    {
        if (scaleAnimation != null) StopCoroutine(scaleAnimation);
        scaleAnimation = null;
    }

    private IEnumerator ScaleAnimation()
    {
        for (float elapsed = 0f; elapsed < 0.5f; elapsed += Time.deltaTime)
        {
            bool showBig = Mathf.FloorToInt(elapsed * 16f) % 2 == 0;
            smallRenderer.enabled = !showBig;
            bigRenderer.enabled = showBig;
            yield return null;
        }
        ApplySize(big);
        scaleAnimation = null;
    }

    public void Starpower()
    {
        if (dead || IsTransitioning) return;
        // Una sola cuenta evita que la primera estrella cancele a la segunda.
        starSeconds = 10f;
        GameAudio.Instance?.Play(SoundEffect.PowerUp);
    }
}
