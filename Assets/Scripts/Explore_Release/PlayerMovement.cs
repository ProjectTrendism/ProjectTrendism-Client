using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 4f;

    [Header("캐릭터 시각 오브젝트")]
    public Transform visualRoot;

    [Header("애니메이터")]
    public Animator animator;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 lastMoveDirection = Vector2.down;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (visualRoot == null)
        {
            Transform foundVisual = transform.Find("Visual");
            if (foundVisual != null)
                visualRoot = foundVisual;
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void Update()
    {
        ReadInput();
        UpdateFacingDirection();
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = moveInput * moveSpeed;
    }

    private void ReadInput()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        moveInput = new Vector2(x, y).normalized;

        if (moveInput != Vector2.zero)
        {
            lastMoveDirection = moveInput;
        }
    }

    private void UpdateFacingDirection()
    {
        if (visualRoot == null)
            return;

        Vector3 scale = visualRoot.localScale;

        if (moveInput.x > 0.01f)
        {
            scale.x = Mathf.Abs(scale.x);
            visualRoot.localScale = scale;
        }
        else if (moveInput.x < -0.01f)
        {
            scale.x = -Mathf.Abs(scale.x);
            visualRoot.localScale = scale;
        }
    }

    private void UpdateAnimation()
    {
        if (animator == null)
            return;

        bool isMoving = moveInput.sqrMagnitude > 0.01f;

        animator.SetBool("IsMoving", isMoving);

        animator.SetFloat("MoveX", moveInput.x);
        animator.SetFloat("MoveY", moveInput.y);

        animator.SetFloat("LastMoveX", lastMoveDirection.x);
        animator.SetFloat("LastMoveY", lastMoveDirection.y);
    }
}