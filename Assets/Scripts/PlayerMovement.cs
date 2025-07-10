using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D _rb;
    private FrameInput _frameInput;
    private Vector2 _frameVelocity;
    private float _time;
    // Circle Cast for ground check
    private RaycastHit2D _hit;
    private Vector2 _castOrigin;
    private float _castRadius;
    private float _castDistance;
    

    //-------------Editor Interface---------------------------
    public bool _debug = false;
    // Horizontal Movement
    public bool takingInput = true;
    public float MaxSpeed = 14f;
    public float Acceleration = 0.5f;
    public float GroundDeceleration = 60f;
    public float AirDeceleration = 30f;

    // Vertical Movement
    public bool _grounded;
    public bool _endedJumpEarly;
    private bool _bufferedJumpUsable, _jumpToConsume;
    private float _timeJumpWasPressed;
    private bool HasBufferedJump => _bufferedJumpUsable && _time < _timeJumpWasPressed + JumpBuffer;
    public float GroundedGravity = -1.5f;
    public float FallAcceleration = 50f;
    public float JumpEndEarlyGravityModifier = 3f;
    public float MaxFallSpeed = 40.0f;
    public float JumpBuffer = .2f;
    public float JumpPower = 36;
    public float GForce;

    public bool levelEnding;

   
   

   


    // Collison
    private bool _cachedQueryStartInColliders;
    public LayerMask groundCheckIgnoreLayers;
    public CircleCollider2D _col;
    public float GroundDistance = 0.5f;
    public struct FrameInput
    {
        public Vector2 Move;
        public bool JumpDown;
        public bool JumpHeld;

    }

   
    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<CircleCollider2D>();
        _cachedQueryStartInColliders = Physics2D.queriesStartInColliders;

        _castOrigin = _col.bounds.center + Vector3.up * 0.5f;
        _castRadius = _col.radius;
        _castDistance = GroundDistance;

    }

    // --------------------------UPDATE METHODS------------------
  

    void Update()
    {
        _time += Time.deltaTime;
        if (_time > .5f && takingInput)
        {
            GatherInput();
        }

       
    }



    private void GatherInput()
    {
        _frameInput = new FrameInput
        {
            Move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")),
            JumpDown = Input.GetButtonDown("Jump"),
            JumpHeld = Input.GetButton("Jump"),

           
        };


        if (_frameInput.JumpDown)
        {
            _jumpToConsume = true;
            _timeJumpWasPressed = _time;
        }

       

    }

   
    
    void OnDrawGizmos()
    {
        if (_debug)
        {
            Gizmos.color = Color.red;
            DrawCircle(_castOrigin, _castRadius, Color.green);

            Vector2 endPoint = _castOrigin + Vector2.down * _castDistance;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(_castOrigin, endPoint);

            if (_hit.collider)
                DrawCircle(_hit.point, _castRadius, Color.cyan);
        }
        
    }

    void DrawCircle(Vector2 center, float radius, Color color, int steps = 32)
    {
        Gizmos.color = color;
        float angleStep = 360f / steps;
        Vector3 prevPoint = center * radius;

        for (int i = 1; i <= steps; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }

   




  
    private void HandleJump()
    {
        if (!_endedJumpEarly && !_grounded && !_frameInput.JumpHeld && _rb.linearVelocityY > 0)
            _endedJumpEarly = true;

        if (!_jumpToConsume && !HasBufferedJump)
            return;

        if (_grounded)
        {
            ExecuteJump();
            //SoundManager.PlayRandomSoundPitch(SoundType.JUMP, .25f, true);
        }


        _jumpToConsume = false;
    }

    private void ExecuteJump()
    {
        _endedJumpEarly = false;
        _timeJumpWasPressed = 0;
        _bufferedJumpUsable = false;
        _frameVelocity.y = JumpPower;
        //Jumped?.Invoke();
    }

    // -------------------------------FIXED UPDATE METHODS--------------
    private void FixedUpdate()
    {
        if (_time > 0.5f)
            CheckCollisions();
       
        HandleJump();
        //HandleHorizontal();

        Gravity();

        ApplyMovement();

        
    }

    bool groundHit;
    private void CheckCollisions()
    {
        Physics2D.queriesStartInColliders = false;
        _castOrigin = _col.bounds.center + Vector3.up * 0.5f;
        _castRadius = _col.radius;
        _castDistance = GroundDistance;
        
        _hit = Physics2D.CircleCast
            (
            _castOrigin,
            _col.radius,
            Vector2.down,
            GroundDistance,
            ~groundCheckIgnoreLayers.value
            );
        
        /*
        int groundedCount = 0;

        //We are gonna try and check ground collision for each particle
        for (int i = 0; i < points.Count; i++)
        {
            //Debug.DrawRay(points[i].transform.position, transform.localScale.x * Vector3.down * points[i].transform.localScale.x, Color.yellow);
            bool groundRay = Physics2D.Raycast(points[i].transform.position,
                Vector3.down,
                transform.localScale.x * points[i].transform.localScale.x,
                ~groundCheckIgnoreLayers.value);
            groundedCount += groundRay ? 1 : 0;
        }
        */
        


        // Landed on the Ground
        if (!_grounded && _hit.collider)
        {
            _grounded = true;
            _bufferedJumpUsable = true;
            _endedJumpEarly = false;
            // Can call grounded change event
        }

        // Left the Ground
        else if (_grounded && !_hit.collider)
        {
            _grounded = false;
            //_frameLeftGrounded = _time;
            //GroundedChanged?.Invoke(false, 0);
        }

        // Set the default where raycasts return true if they start in their colliders
        Physics2D.queriesStartInColliders = _cachedQueryStartInColliders;
    }
    private void HandleHorizontal()
    {
        if (_frameInput.Move.x == 0)
        {
            var deceleration = _grounded ? GroundDeceleration : AirDeceleration;
            _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, 0, deceleration * Time.fixedDeltaTime);
           
        }

        else
        {
            _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, _frameInput.Move.x * MaxSpeed, Acceleration * Time.fixedDeltaTime);
            //SoundManager.PlayRandomSoundPitch(SoundType.SLIMESTEPS, .25f, false);
           
        }

    }
    private void ApplyMovement()
    {
        _rb.linearVelocity = _frameVelocity;

        

    }

    private void Gravity()
    {


        if (_grounded && _frameVelocity.y <= 0f)
        {
            _frameVelocity.y = GroundedGravity;
        }
        
        else
        {
            var inAirGravity = FallAcceleration;
            //if (_endedJumpEarly && _frameVelocity.y > 0) inAirGravity *= JumpEndEarlyGravityModifier;
            //falling
            //else if(frameVelocity.y < 0) Falling?.Invoke();
            GForce = Mathf.MoveTowards(_frameVelocity.y, -MaxFallSpeed, inAirGravity * Time.fixedDeltaTime);
            _frameVelocity.y = GForce;
        }
    }

}
