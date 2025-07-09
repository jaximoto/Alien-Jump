using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D _rb;
    private FrameInput _frameInput;
    private Vector2 _frameVelocity;
    private float _time;
    

    //-------------Editor Interface---------------------------

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

   
    /*
    void OnDrawGizmos()
    {
        //Gather Component Vectors
        if (ChargeHeld)
        {
            //Debug.Log($"");
            Gizmos.color = Color.green;
            Gizmos.DrawLine(gameObject.transform.position,
                new Vector3(gameObject.transform.position.x - _frameInput.ChargeDir.x, gameObject.transform.position.y - _frameInput.ChargeDir.y, gameObject.transform.position.z));
        }

        for (int i = 0; i < _pointTargets.Length; i++)
        {
            //ReShaping Force
            Gizmos.color = Color.red;
            Vector3 target = gameObject.transform.position + (_pointTargets[i] * gameObject.transform.localScale.x);
            Gizmos.DrawLine(points[i].transform.position, target);

            //connecting lines
            Gizmos.color = Color.blue;



            if (i == points.Count - 1) Gizmos.DrawLine(points[i].transform.position, points[0].transform.position);
            else Gizmos.DrawLine(points[i].transform.position, points[i + 1].transform.position);
        }
    }
   */

   




  
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
        Vector2 origin = _col.bounds.center + Vector3.up * 0.5f;

        
        bool groundHit = Physics2D.CircleCast
            (
            origin,
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
        if (!_grounded && groundHit)
        {
            _grounded = true;
            _bufferedJumpUsable = true;
            _endedJumpEarly = false;
            // Can call grounded change event
        }

        // Left the Ground
        else if (_grounded && !groundHit)
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
