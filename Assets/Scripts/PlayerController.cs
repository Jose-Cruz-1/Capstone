using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

    [Header("Horizontal Movement Settings")]
    [SerializeField] private float walkSpeed = 1;

    private int jumpBufferCounter;
    [SerializeField] private int jumpBufferFrames;
    private int airJumpCounter = 0;
    [SerializeField] private int maxAirjumps;
    private float coyoteTimeCounter = 0;
    [SerializeField] private float coyoteTime;
    [Space(5)]

    [Header("Ground Check Settings")]
    [SerializeField] private float jumpForce = 10;
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private float groundCheckY = 0.2f;
    [SerializeField] private float groundCheckX = 0.5f;
    [SerializeField] private LayerMask whatIsGround;

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed;
    [SerializeField] private float dashtime;
    [SerializeField] private float dashCooldown;
    [Space(5)]

    [Header("Wall Jump and Slide Settings")]
    [SerializeField] private float wallJumpForce = 10f; // Force applied for wall jumping.
    [SerializeField] private float wallSlideSpeed = 2f; // Speed for wall sliding.
    [SerializeField] private float wallDetectDistance = 0.1f; // Distance to detect walls.
    [SerializeField] private LayerMask whatIsWall; // Layer mask for walls.
    [Space(5)]

    [Header("Attack Settings")]
    bool attack = false;
    float timeBetweenAttack, timeSinceAttack;
    [SerializeField] Transform SideAttackTransform;
    [SerializeField] Vector2 SideAttackArea;
    [SerializeField] LayerMask attackableLayer;
    [SerializeField] float damage;
    [Space(5)]

    [Header("Recoil Settings")]
    [SerializeField] int recoilXSteps = 5;
    [SerializeField] int recoilYSteps = 5;
    [SerializeField] float recoilXSpeed = 100;
    [SerializeField] float recoilYSpeed = 100;
    int stepsXRecoiled, stepsYRecoiled;

    PlayerStateList pState;
    private float xAxis, yAxis;
    private Rigidbody2D rb;
    private float gravity;
    Animator anim;
    private bool canDash = true;
    private bool dashed;

    public static PlayerController Instance;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        pState = GetComponent<PlayerStateList>();

        rb = GetComponent<Rigidbody2D>();

        anim = GetComponent<Animator>();

        gravity = rb.gravityScale;
    }

    private void OnDrawGizmos()
    {
       Gizmos.color = Color.magenta;
       Gizmos.DrawWireCube(SideAttackTransform.position, SideAttackArea);
    }

    // Update is called once per frame
    void Update()
    {
        GetInputs();
        UpdateJumpVariables();
        if (pState.dashing) return;
        Flip();
        Move();
        Jump();
        WallSlideAndJump();
        StartDash();
        Attack();
    }

    void Attack()
    {
        timeSinceAttack += Time.deltaTime;
        if (attack && timeSinceAttack >= timeBetweenAttack)
        {
            timeSinceAttack = 0;
            anim.SetTrigger("Attacking");

            if (yAxis == 0 || yAxis < 0 && Grounded())
            {
                Hit(SideAttackTransform, SideAttackArea, ref pState.recoilingX, recoilXSpeed);
            }
            /*else if(yAxis > 0)
            {
                Hit(UpAttackTransform, UpAttackArea, ref pstate.recoilingY, recoilYSpeed);
            }
            else if (yAxis > 0 && !Grounded())
            {
                Hit(DownAttackTransform, DownAttackArea, ref pstate.recoilingY, recoilYSpeed);
            }*/
        }
    }

    private void Hit(Transform _attackTransform, Vector2 _attackArea, ref bool _recoilDir, float recoilStrength)
    {
        Collider2D[] objectsToHit = Physics2D.OverlapBoxAll(_attackTransform.position, _attackArea, 0, attackableLayer);

        if(objectsToHit.Length > 0)
        {
            _recoilDir = true;
        }
        for(int i = 0; i < objectsToHit.Length; i++)
        {
            if (objectsToHit[i].GetComponent<Enemy>() != null)
            {
                objectsToHit[i].GetComponent<Enemy>().EnemyHit(damage,(transform.position - objectsToHit[i].transform.position).normalized, recoilStrength);
            }
        }
    }

    void Recoil()
    {
        if(pState.recoilingX)
        {
            if (pState.lookingRight) //Determines whether recoil sends the player to the right or left, opposite of the enemy hit
            {
                rb.velocity = new Vector2(-recoilXSpeed, 0);
            }
            else
            {
                rb.velocity = new Vector2(recoilXSpeed, 0);
            }
        }

        if(pState.recoilingY)
        {
            if(yAxis < 0)
            {
                rb.gravityScale = 0;
                rb.velocity = new Vector2(rb.velocity.x, recoilYSpeed);
            }
            else
            {
                rb.velocity = new Vector2(rb.velocity.x, -recoilYSpeed);
            }
            airJumpCounter = 0;
        }
        else
        {
            rb.gravityScale = gravity;
        }

        //Stops Recoil
        if(pState.recoilingX && stepsXRecoiled < recoilXSteps)
        {
            stepsXRecoiled++;
        }
        else
        {
            StopRecoilX();
        }

        if (pState.recoilingY && stepsYRecoiled < recoilYSteps)
        {
            stepsYRecoiled++;
        }
        else
        {
            StopRecoilY();
        }

        if(Grounded())
        {
            StopRecoilY();
        }
    }

    void StopRecoilX()
    {
        stepsXRecoiled = 0;
        pState.recoilingX = false;
    }

    void StopRecoilY()
    {
        stepsYRecoiled = 0;
        pState.recoilingY = false;
    }

    void GetInputs()
    {
        xAxis = Input.GetAxisRaw("Horizontal");
        yAxis = Input.GetAxisRaw("Vertical");
        anim.SetBool("Walking", rb.velocity.x != 0 && Grounded());
        attack = Input.GetButtonDown("Attack");
    }

    void Flip()
    {
        if(xAxis < 0)
        {
            transform.localScale = new Vector2(-5, transform.localScale.y);
            pState.lookingRight = false;
        }
        else if(xAxis > 0)
        {
            transform.localScale = new Vector2(5, transform.localScale.y);
            pState.lookingRight = true;
        }
    }

    private void Move()
    { 
        rb.velocity = new Vector2(walkSpeed * xAxis, rb.velocity.y);
        anim.SetBool("Walking", rb.velocity.x != 0 && Grounded());
    }

    void StartDash()
    {
        if(Input.GetButtonDown("Dash") && canDash && !dashed)
        {
            StartCoroutine(Dash());
            dashed = true;
        }

        if (Grounded())
        {
            dashed = false;
        }
    }

    IEnumerator Dash()
    {
        canDash = false;
        pState.dashing = true;
        anim.SetTrigger("Dashing");
        rb.gravityScale = 0;
        rb.velocity = new Vector2(transform.localScale.x * dashSpeed, 0);
        yield return new WaitForSeconds(dashtime);
        rb.gravityScale = gravity;
        pState.dashing = false;
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    public bool Grounded()
    {
        if (Physics2D.Raycast(groundCheckPoint.position, Vector2.down, groundCheckY, whatIsGround)
            || Physics2D.Raycast(groundCheckPoint.position + new Vector3(groundCheckX, 0, 0), Vector2.down, groundCheckY, whatIsGround)
            || Physics2D.Raycast(groundCheckPoint.position + new Vector3(-groundCheckX, 0, 0), Vector2.down, groundCheckY, whatIsGround))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    void Jump()
    {
        if(Input.GetButtonUp("Jump") && rb.velocity.y > 0)
        {
            rb.velocity = new Vector2(rb.velocity.x, 5); //Allows a bit of jump momentum to be carried after ending a jump early

            pState.jumping = false;
        }

        if (!pState.jumping)
        {
            if (jumpBufferCounter > 0 && coyoteTimeCounter > 0)
            {
                rb.velocity = new Vector3(rb.velocity.x, jumpForce);

                pState.jumping = true;
            }
            if(!Grounded() && airJumpCounter < maxAirjumps && Input.GetButtonDown("Jump")) //Prevents air jumps exceeded the specified amount
            {
                pState.jumping = true;

                airJumpCounter++;

                rb.velocity = new Vector3(rb.velocity.x, jumpForce);
            }
        }

        anim.SetBool("Jumping", !Grounded());
    }

    void UpdateJumpVariables()
    {
        if (Grounded())
        {
            pState.jumping = false;
            coyoteTimeCounter = coyoteTime;
            airJumpCounter = 0; //Resets double jump when landing on the ground.
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime; //Counts how long the player has been off the ground for.
        }

        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBufferFrames;
        }
        else
        {
            jumpBufferCounter--;
        }
    }

    private void WallSlideAndJump()
    {
        // Raycast to detect walls.
        bool isTouchingWall = Physics2D.Raycast(groundCheckPoint.position, Vector2.right * transform.localScale.x, wallDetectDistance, whatIsWall);

        if (isTouchingWall && !Grounded() && rb.velocity.y < 0)
        {
            // Apply wall slide effect.
            rb.velocity = new Vector2(rb.velocity.x, -wallSlideSpeed);
            anim.SetBool("Touching Wall", true);
        }
        else
        {
            anim.SetBool("Touching Wall", false);
        }

        // Wall jump logic.
        if (isTouchingWall && Input.GetButtonDown("Jump"))
        {
            // Apply force for wall jump.
            rb.velocity = new Vector2(-transform.localScale.x * wallJumpForce, jumpForce);
            anim.SetTrigger("Jumping");
            rb.AddForce(Vector2.right * wallJumpForce, ForceMode2D.Impulse);
        }
    }
}