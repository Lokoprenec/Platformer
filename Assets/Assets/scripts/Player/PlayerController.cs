using UnityEngine;

public class PlayerController : MonoBehaviour
{
    //ESSENTIALS
    private PlayerManager pM;
    private Rigidbody2D rb;
    private Collider2D col;

    public PlayerStates currentState;

    [Header("Visuals")]

    [Header("Essentials")]
    public SpriteRenderer graphic;
    private Animator anim;

    [Header("Animations")]
    public PlayerAnimations idleAnimation;
    public PlayerAnimations runAnimation;
    public PlayerAnimations jumpAnimation;
    public PlayerAnimations fallAnimation;
    public PlayerAnimations landingAnimation;

    [Header("Movement")]

    [Header("Horizontal movement")] //acceleration - max speed - deceleration - quick turn
    public float direction;
    public float acceleration;
    public float deceleration;
    public float maxSpeed;
    [SerializeField] private float currentSpeed;
    public float minMovementSpeed;
    public float airTurnDeceleration;

    [Header("Vertical movement")] //coyote time - jump - bonus air time - fast fall
    public float jumpForce;
    public float maxFallSpeed;
    public float initialGravity;
    public float jumpCutGravity;
    public float fallGravity;
    public float hangTimeVelocityThreshold;
    public float hangTimeGravity;
    public bool isGrounded;
    public float groundCheckDistance;
    public LayerMask groundLayer;
    public float landingCooldown;
    private float landingTimer;

    [Header("Movement bonuses")]
    public float coyoteTime;
    private float coyoteTimeCounter;
    public float jumpBufferTime;
    private float jumpBufferCounter;

    [Header("Bash mechanic")] //detect target - get direction input - launch - bonus air control grace
    public float bashDetectionRange;
    public float launchVelocity;
    public float bashLockTime;
    private float bashLockTimer;
    public float bashGravity;
    public float bashTime;
    private float bashTimer;
    public LayerMask bashTargetLayer;
    private Vector2 bashDir;
    public float exitBashGravity;

    void Awake()
    {
        currentSpeed = 0;
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        pM = GetComponent<PlayerManager>();
        anim = graphic.GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        //STATE MACHINE
        if (currentState == PlayerStates.Idle)
        {
            CheckForMovement();
            CheckForAbilities();

            coyoteTimeCounter = coyoteTime;

            anim.Play(idleAnimation.ToString());
        }
        else if (currentState == PlayerStates.Walk)
        {
            CheckForMovement();
            CheckForAbilities();

            coyoteTimeCounter = coyoteTime;

            anim.Play(runAnimation.ToString());

            if (rb.linearVelocityX == 0)
            {
                currentState = PlayerStates.Idle;
            }
        }
        else if (currentState == PlayerStates.Jump)
        {
            CheckForHorizontalMovement();
            WhileJumping();
            CheckForAbilities();
        }
        else if (currentState == PlayerStates.Fall)
        {
            CheckForHorizontalMovement();
            Fall();
            WhileFalling();
            CheckForAbilities();
        }
        else if (currentState == PlayerStates.Landing)
        {
            CheckForMovement();
            CheckForAbilities();

            coyoteTimeCounter = coyoteTime;

            landingTimer -= Time.deltaTime;
            
            if (landingTimer <= 0)
            {
                currentState = PlayerStates.Idle;
                landingTimer = landingCooldown;
            }
            else
            {
                anim.Play(landingAnimation.ToString());
            }
        }
        else if (currentState == PlayerStates.Dash)
        {
            if (isGrounded)
            {
                rb.linearVelocityY = 0;
                SetStateToFall();
                return;
            }

            if (bashTimer <= 0)
            {
                // Only apply upward velocity if we're in the air
                if (!isGrounded && rb.linearVelocityY > 0)
                {
                    if (rb.linearVelocityX == 0)
                    {
                        rb.linearVelocityY = maxSpeed * 1.5f;
                    }
                    else
                    {
                        rb.linearVelocityY = maxSpeed;
                    }
                }

                rb.gravityScale = jumpCutGravity;
                currentState = PlayerStates.ExitDash;
            }
            else
            {
                rb.linearVelocity = launchVelocity * bashDir;
                bashTimer -= Time.deltaTime;
            }

            if (bashLockTimer <= 0)
            {
                CheckForHorizontalMovement();
                CheckForAbilities();

                if ((Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) || (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) && rb.linearVelocityY > 0)
                {
                    rb.linearVelocityY = maxSpeed;
                    currentState = PlayerStates.ExitDash;
                }
            }
            else
            {
                bashLockTimer -= Time.deltaTime;
            }
        }
        else if (currentState == PlayerStates.ExitDash)
        {
            CheckForHorizontalMovement();
            CheckForAbilities();

            rb.gravityScale = exitBashGravity;

            if (rb.linearVelocityY <= hangTimeVelocityThreshold)
            {
                rb.gravityScale = hangTimeGravity; //bonus air time
                SetStateToFall();
            }
        }

        RaycastHit2D groundCheck = Physics2D.Raycast(new Vector2(col.bounds.min.x + 0.1f, col.bounds.min.y - groundCheckDistance), Vector2.right, col.bounds.max.x - col.bounds.min.x - 0.1f, groundLayer); //checking in a horizontal line right bellow player's feet
        isGrounded = groundCheck.collider != null;
        Debug.DrawLine(new Vector2(col.bounds.min.x, col.bounds.min.y - groundCheckDistance), new Vector2(col.bounds.max.x, col.bounds.min.y - groundCheckDistance), Color.red);

        transform.localScale = new Vector2(direction, transform.localScale.y);
    }

    void CheckForMovement()
    {
        CheckForHorizontalMovement();
        CheckForVerticalMovement();
    }

    void CheckForHorizontalMovement()
    {
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) //right movement
        {
            direction = 1;
            Movement();
        }
        else if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) //left movement
        {
            direction = -1;
            Movement();
        }
        else if (currentState != PlayerStates.Dash)
        {
            Decelerate(direction, deceleration); //slow down
        }
    }

    void CheckForVerticalMovement()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpBufferCounter = jumpBufferTime;
        }

        if (jumpBufferCounter > 0 && coyoteTimeCounter > 0) //jump
        {
            Jump();
        }

        if (rb.linearVelocityY < -0.01) //fall
        {
            SetStateToFall();
        }
        else if (currentState != PlayerStates.Jump)
        {
            rb.gravityScale = initialGravity;
        }
    }

    void CheckForAbilities()
    {
        if (Input.GetKeyDown(KeyCode.X))
        {
            SearchForBashTargets();
        }
    }

    void Movement()
    {
        currentSpeed += acceleration * Time.deltaTime;
        currentSpeed = Mathf.Clamp(currentSpeed, 0, maxSpeed); // Can't go above max
        int bashX = Mathf.Abs(bashDir.x) < 0.1f ? 0 : (int)Mathf.Sign(bashDir.x);

        if (currentState != PlayerStates.Dash)
        {
            // Handling direction change
            if (rb.linearVelocityX * direction < 0) // Moving opposite to desired direction
            {
                if (isGrounded)
                {
                    currentSpeed = 0;
                }
                else
                {
                    Decelerate(-direction, airTurnDeceleration);
                    return;
                }
            }

            rb.linearVelocityX = currentSpeed * direction;

            if (Mathf.Abs(rb.linearVelocityX) < minMovementSpeed && currentSpeed > 0)
            {
                rb.linearVelocityX = minMovementSpeed * direction; //minimal movement value when the button is pressed
            }
        }
        else if (direction != bashX && bashX != 0)
        {
            rb.linearVelocityY = maxSpeed;
            currentState = PlayerStates.ExitDash;
        }

        if (currentState == PlayerStates.Idle || currentState == PlayerStates.Landing)
        {
            currentState = PlayerStates.Walk;
        }
    }

    void Decelerate(float dir, float dec)
    {
        currentSpeed -= dec * Time.deltaTime;

        if (rb.linearVelocityX == 0 || (rb.linearVelocityX <= 0 && dir > 0) || (rb.linearVelocityX >= 0 && dir < 0))
        {
            currentSpeed = 0; //makes you stop from going in the opposite direction
        }

        rb.linearVelocityX = currentSpeed * dir;
    }

    void Jump()
    {
        if (isGrounded || coyoteTimeCounter > 0)
        {
            rb.gravityScale = initialGravity;
            rb.linearVelocityY = jumpForce;
            currentState = PlayerStates.Jump;
            anim.Play(jumpAnimation.ToString());
        }
    }

    void WhileJumping()
    {
        if (Mathf.Abs(rb.linearVelocityY) <= hangTimeVelocityThreshold)
        {
            rb.gravityScale = hangTimeGravity; //bonus air time
            SetStateToFall();
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            coyoteTimeCounter = 0;
        }

        if (rb.linearVelocityY < -0.01) //fall
        {
            SetStateToFall();
        }
        else if (!Input.GetKey(KeyCode.Space))
        {
            rb.gravityScale = jumpCutGravity; //cuts the jump when the button is released
        }
    }

    void SetStateToFall()
    {
        anim.Play(fallAnimation.ToString());
        currentState = PlayerStates.Fall;
    }

    void Fall()
    {
        rb.gravityScale = fallGravity; //fast fall
        rb.linearVelocityY = Mathf.Clamp(rb.linearVelocityY, -maxFallSpeed, 0); //can't fall faster than max
        currentState = PlayerStates.Fall;
    }

    void WhileFalling()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime; //can jump even if the button was pressed slightly before hitting the ground
        }

        coyoteTimeCounter -= Time.deltaTime; //can jump for a short period after already falling off a ledge

        if (isGrounded)
        {
            currentState = PlayerStates.Landing;
            landingTimer = landingCooldown;
        }
    }

    void SearchForBashTargets() 
    {
        Collider2D[] targets = Physics2D.OverlapCircleAll(transform.position, bashDetectionRange, bashTargetLayer);
        float closestDistance = Mathf.Infinity;
        Transform selectedTarget = null;

        foreach (Collider2D target in targets) 
        {
            float distance = Vector2.Distance(transform.position, target.transform.position);

            if (distance >= closestDistance) continue;

            selectedTarget = target.transform;
            closestDistance = distance;
        }

        if (selectedTarget != null)
        {
            Bash(selectedTarget);
        }
    }

    void Bash(Transform target)
    {
        currentState = PlayerStates.Dash;
        coyoteTimeCounter = 0;
        jumpBufferCounter = 0;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = bashGravity;
        transform.position = target.position;
        bashLockTimer = bashLockTime;
        bashTimer = bashTime;
        bashDir = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;

        if (bashDir.x == 0 && bashDir.y == 0)
        {
            bashDir.y = 1;
        }
    }
}

public enum PlayerStates
{
    Idle, Walk, Jump, Fall, Landing, Dash, ExitDash
}

public enum PlayerAnimations
{
    staticIdleSketch, idleSketch, 
    testRunSketch, runSketch, 
    jumpStartSketch, jumpEndSketch, 
    fallStartSketch, fallEndSketch, 
    landingSketch
}
