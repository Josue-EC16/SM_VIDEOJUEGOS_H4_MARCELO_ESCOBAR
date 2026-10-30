using UnityEngine;

/// <summary>Velocidad propia y gravedad variable; la plataforma aporta s?lo su desplazamiento f?sico.</summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    private Camera mainCamera;
    private Rigidbody2D rb;
    private Collider2D capsuleCollider;
    private Vector2 velocity;
    private float inputAxis;
    private MovingPlatform support;
    private float strokeDelay;
    public bool swimming { get; private set; }
    public float moveSpeed = 8f;
    public float maxJumpHeight = 5f;
    public float maxJumpTime = 1f;
    public float jumpForce => 2f * maxJumpHeight / (maxJumpTime / 2f);
    public float gravity => -2f * maxJumpHeight / Mathf.Pow(maxJumpTime / 2f, 2f);
    public bool grounded { get; private set; }
    public bool jumping { get; private set; }
    public bool running => Mathf.Abs(velocity.x) > 0.25f || Mathf.Abs(inputAxis) > 0.25f;
    public bool sliding => inputAxis > 0f && velocity.x < 0f || inputAxis < 0f && velocity.x > 0f;
    public bool falling => velocity.y < 0f && !grounded;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // S?lo se usa en las pruebas; la compilaci?n normal siempre lee el teclado.
    internal bool simulatedInput;
    internal float simulatedAxis;
    internal bool simulatedJump;
    internal bool simulatedJumpPressed;
    internal bool simulatedDive;
#endif

    private void Awake()
    {
        mainCamera = Camera.main;
        rb = GetComponent<Rigidbody2D>();
        capsuleCollider = GetComponent<Collider2D>();
    }
    private void OnEnable()
    {
        rb.bodyType = RigidbodyType2D.Dynamic;
        capsuleCollider.enabled = true;
        velocity = Vector2.zero;
        jumping = false;
        support = null;
        strokeDelay = 0f;
    }
    private void OnDisable()
    {
        rb.bodyType = RigidbodyType2D.Kinematic;
        capsuleCollider.enabled = false;
        velocity = Vector2.zero;
        inputAxis = 0f;
        jumping = grounded = false;
        support = null;
    }
    private void Update()
    {
        float axis = Input.GetAxis("Horizontal");
        bool jumpPressed = Input.GetButtonDown("Jump");
        bool jumpHeld = Input.GetButton("Jump");
        bool diveHeld = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (simulatedInput)
        {
            axis = simulatedAxis;
            jumpPressed = simulatedJumpPressed;
            jumpHeld = simulatedJump;
            simulatedJumpPressed = false;
            diveHeld = simulatedDive;
        }
#endif
        inputAxis = axis;
        float speed = swimming ? 4.5f : moveSpeed;
        velocity.x = Mathf.MoveTowards(velocity.x, axis * speed, moveSpeed * Time.deltaTime);
        // La física continua resuelve las paredes sin inmovilizar la cápsula al rozar esquinas.
        if (velocity.x != 0f) transform.eulerAngles = velocity.x > 0f ? Vector3.zero : new Vector3(0f, 180f, 0f);

        grounded = rb.TryGetGround(out RaycastHit2D ground) && velocity.y <= 0f;
        support = grounded && ground.rigidbody != null ? ground.rigidbody.GetComponent<MovingPlatform>() : null;
        if (swimming)
        {
            Swim(jumpPressed, jumpHeld || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow), diveHeld);
            return;
        }
        if (grounded)
        {
            velocity.y = Mathf.Max(velocity.y, 0f);
            jumping = false;
            if (jumpPressed)
            {
                velocity.y = jumpForce;
                jumping = true;
                grounded = false;
                support = null;
                GameAudio.Instance?.Play(SoundEffect.Jump);
            }
        }
        velocity.y += gravity * (velocity.y < 0f || !jumpHeld ? 2f : 1f) * Time.deltaTime;
        velocity.y = Mathf.Max(velocity.y, gravity / 2f);
    }

    public void SetSwimming(bool value)
    {
        if (swimming == value) return;
        swimming = value;
        velocity = Vector2.zero;
        support = null;
        strokeDelay = 0f;
    }

    private void Swim(bool pressed, bool held, bool diving)
    {
        strokeDelay = Mathf.Max(0f, strokeDelay - Time.deltaTime);
        if (grounded) velocity.y = Mathf.Max(0f, velocity.y);
        if (!diving && (pressed || held) && strokeDelay <= 0f)
        {
            velocity.y = 4.5f;
            strokeDelay = 0.28f;
            GameAudio.Instance?.Play(SoundEffect.Swim);
        }
        velocity.y -= (diving ? 16f : 8f) * Time.deltaTime;
        velocity.y = Mathf.Max(velocity.y, diving ? -3.5f : -2.5f);
        jumping = velocity.y > 0.1f;
    }
    private void FixedUpdate()
    {
        Vector2 position = rb.position + velocity * Time.fixedDeltaTime;
        if (support != null && grounded) position += support.Displacement;
        Vector2 left = mainCamera.ViewportToWorldPoint(Vector2.zero);
        Vector2 right = mainCamera.ViewportToWorldPoint(Vector2.one);
        position.x = Mathf.Clamp(position.x, left.x + 0.5f, right.x - 0.5f);
        rb.MovePosition(position);
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            if (collision.HasContactNormal(Vector2.up)) { velocity.y = jumpForce / 2f; jumping = true; }
        }
        // Las esquinas redondeadas de la cápsula pueden rozar una pared sin ser un techo.
        else if (collision.gameObject.layer != LayerMask.NameToLayer("PowerUp") && collision.HasContactNormal(Vector2.down, 0.9f))
            velocity.y = 0f;
    }
}
