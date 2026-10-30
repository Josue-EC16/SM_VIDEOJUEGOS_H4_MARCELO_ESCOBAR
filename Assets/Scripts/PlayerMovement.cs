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
    // Default es la capa 0. Una constante evita consultar Unity al deserializar el componente.
    private const int GroundMask = 1;
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (simulatedInput)
        {
            axis = simulatedAxis;
            jumpPressed = simulatedJumpPressed;
            jumpHeld = simulatedJump;
            simulatedJumpPressed = false;
        }
#endif
        inputAxis = axis;
        velocity.x = Mathf.MoveTowards(velocity.x, axis * moveSpeed, moveSpeed * Time.deltaTime);
        if (rb.Raycast(Vector2.right * velocity.x)) velocity.x = 0f;
        if (velocity.x != 0f) transform.eulerAngles = velocity.x > 0f ? Vector3.zero : new Vector3(0f, 180f, 0f);

        Bounds bounds = capsuleCollider.bounds;
        RaycastHit2D ground = Physics2D.BoxCast(new Vector2(bounds.center.x, bounds.min.y + 0.08f),
            new Vector2(bounds.size.x * 0.75f, 0.04f), 0f, Vector2.down, 0.18f, GroundMask);
        grounded = ground.collider != null && !ground.collider.isTrigger && velocity.y <= 0f;
        support = grounded && ground.rigidbody != null ? ground.rigidbody.GetComponent<MovingPlatform>() : null;
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
