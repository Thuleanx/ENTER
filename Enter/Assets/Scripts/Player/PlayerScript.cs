using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using Enter.Utils;

namespace Enter
{
  [DisallowMultipleComponent]
  public class PlayerScript : MonoBehaviour
  {
    public static PlayerScript Instance = null;

    // Responsible for providing the PlayerManager with all these things:
    private Rigidbody2D           _rb;
    private BoxCollider2D         _bc;
    private PlayerColliderScript  _co;
    private PlayerStretcherScript _ps;
    [SerializeField] private SpriteRenderer _sr;
    [SerializeField] private Animator       _an;

    private InputData _in => InputManager.Instance.Data;

    private const float _eps = 0.001f;

    #region ================== Variables

    #region ====== Horizontal Movement

    [Header("Horizontal Movement")]

    [SerializeField, Tooltip("Maximum horizontal speed.")]
    private float _horizontalSpeed;

    [SerializeField, Tooltip("Time from idle to maximum horizontal speed.")]
    private float _timeToMaxSpeed;

    [SerializeField, Tooltip("Time from maximum horizontal speed to zero.")]
    private float _timeToZeroSpeed;

    [SerializeField, Tooltip("Multiplier to horizontal acceleration when in the air.")]
    private float _midairHorizontalAccelerationMultiplier;

    #endregion

    #region ====== Vertical Movement

    [Header("Vertical Movement")]

    [SerializeField, Tooltip("Maximum height for a jump.")]
    private float _jumpHeight;

    [SerializeField, Tooltip("Maximum time before the maximum jump height of the player is reached.")]
    private float _timeToMaxHeight;

    [SerializeField, Tooltip("Max fall as a multiple of jump speed.")]
    private float _maxFallMultiple;

    [property:SerializeField, Tooltip("Maximum speed when falling due to gravity.")]
    private float _maxFall { get {return _maxFallMultiple * _jumpSpeed; } }

    #endregion

    #region ====== Movement Tweaks

    [Header("Movement Tweaks")]

    [SerializeField, Tooltip("Gravity multiplier for when absolute vertical velocity is less than threshold.")]
    private float _jumpPeakMultiplier;

    [SerializeField, Tooltip("Threshold for the above.")]
    private float _jumpPeakThreshold;

    [SerializeField, Tooltip("Gravity multiplier for when the jump input is not held down.")]
    private float _lowJumpMultiplier;

    [SerializeField, Tooltip("Time after becoming ungrounded by any means other than jumping, during which jumping is permitted.")]
    private float _coyoteTime;
    private float _lastGroundedTime = -Mathf.Infinity;
    private float _lastJumpedTime = -Mathf.Infinity;

    [SerializeField, Tooltip("Additional coyote time after falling off an RCBox while stationary. This is added to coyote time.")]
    private float _additionalRCBoxCoyoteTime;

    private float _earlyJumpReleaseTime = 0.15f;

    #endregion

    #region ====== Animation

    [Header("Animation")]

    [SerializeField, Tooltip("Minimum time between landing events occurring.")]
    private float _minTimeBetweenLandingEffects = 0.25f;

    [SerializeField, Tooltip("Minimum time after a jump for ground contact to be considered landing.")]
    private float _minTimeLandAfterJump = 0.01f;

    [SerializeField, Tooltip("Landing events, e.g. playing particles.")]
    private UnityEvent _onLandEvents;

    public UnityEvent OnJump;
    public UnityEvent OnEarlyJumpRelease;
    public UnityEvent OnJumpRelease;

    #endregion

    #region ====== Death

    [Header("Death")]

    [SerializeField, Tooltip("Amount of time between the player dying and respawning.")]
    private float _deathRespawnDelay = 0.5f;

    #endregion
    
    #region ====== Math For Jumping

    private float v0 => _jumpSpeed;
    private float vc => _jumpPeakThreshold;
    private float tm => _timeToMaxHeight;
    private float h  => _jumpHeight;
    private float g  => _gravity;
    
    // Maximum initial upward velocity when a jump is started.
    private float _jumpSpeed => (h + Mathf.Sqrt(Mathf.Pow(h, 2) + 2 * h * tm * vc - Mathf.Pow(tm, 2) * Mathf.Pow(vc, 2))) / tm;

    // Acceleration due to gravity.
    private float _gravity => -(_jumpSpeed + vc) / tm;

    #endregion

    #region ====== Internal

    private bool _alreadyNudged;
    private bool _isDead;

    private float _accelerationGrounded => _horizontalSpeed / _timeToMaxSpeed;
    private float _decelerationGrounded => _horizontalSpeed / _timeToZeroSpeed;

    private TimedDataBuffer<float> _horizontalSpeedBuffer = new TimedDataBuffer<float>(0.25f);

    private Vector2 _velocityOfGround {
      get { return (Grounded && _co.CarryingRigidbody) ? _co.CarryingRigidbody.velocity : Vector2.zero; }
    }

    private Vector2 _velocityOnGround {
      get { return _rb.velocity - _velocityOfGround; }
      set { _rb.velocity = value + _velocityOfGround; }
    }

    #endregion

    #endregion

    #region ================== Accessors

    public bool Grounded => _co.OnGround;
    
    public Rigidbody2D           Rigidbody2D           => _rb;
    public BoxCollider2D         BoxCollider2D         => _bc;
    public PlayerStretcherScript PlayerStretcherScript => _ps;
    public PlayerColliderScript  PlayerColliderScript  => _co;
    public SpriteRenderer        SpriteRenderer        => _sr;
    public Animator              Animator              => _an;
    public float                 MaxJumpSpeed          => _jumpSpeed;
    public float                 MaxFallSpeed          => _maxFall;

    #endregion

    #region ================== Methods

    void Awake()
    {
      // This prevents multiple copies of the player from existing at once
      if (Instance) DestroyImmediate(this);
	  else {
		Instance = this;

		_rb = GetComponent<Rigidbody2D>();
		_bc = GetComponent<BoxCollider2D>();
		_co = GetComponent<PlayerColliderScript>();
		_ps = GetComponent<PlayerStretcherScript>();
	  }
    }

    void Start()
    {
      /* _in = InputManager.Instance.Data; */
    }

    void FixedUpdate()
    {
      if (_isDead) return;

      if (_co.Crushed)
      {
        Debug.Log("Crushed");
        _co.Crushed = false;
        Die();
        return;
      }

      handleMovement();
    }

    void LateUpdate()
    {
      // best practice to keep things updating animator states in LateUpdate
      // this is right before things get rendered on screen, so there
      // won't be one frame where your animation and actual input/physics are mismatched
      handleMovementAnimation();
    }

    public void Die()
    {
      SetFieldsDead();
      StartCoroutine(waitForRespawn());
    }

    public void SetFieldsDead()
    {
      _isDead = true;
      _an.speed = 0;
      _rb.velocity = Vector2.zero;
      _rb.constraints = RigidbodyConstraints2D.FreezeAll;
      _bc.enabled = false;
    }

    public void SetFieldsAlive()
    {
      _isDead = false;
      _an.speed = 1;
      _sr.flipX = false;
      _lastGroundedTime = Time.time; // Prevents landing effect playing on respawn
      _rb.velocity = Vector2.zero;
      _rb.position = SceneTransitioner.Instance.SpawnPosition;
      _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
      _bc.enabled = true;
    }

    // Allows for freezing this component (in place and in animation). By default also disable the current component.
    public void ToggleTimeSensitiveComponents(bool enabled, bool affectSelf = true)
    {
      List<Behaviour> behaviours = new List<Behaviour>();

      // anything where disabling would effectively freeze time for the player appears here
      behaviours.Add(GetComponent<Animator>());
      behaviours.Add(GetComponent<PlayerColliderScript>());
      if (affectSelf) behaviours.Add(this);

      // rigidbody have to be treated a little differently, since it is a Component but not a Behaviour
      Rigidbody2D rigidbody = GetComponent<Rigidbody2D>();
      // this should disable the rigidbody
      rigidbody.simulated = enabled;

      foreach (Behaviour behaviour in behaviours)
        if (behaviour) behaviour.enabled = enabled; // null check not neccessary, since List<T> prevents adding null into it iirc.
    }

    #endregion

    #region ================== Helpers

    private void handleMovement()
    {
      handleWalk();
      handleJump();
      handleMidairNudge();
      handleGravity();
    }

    private void handleWalk()
    {
      // Handles horizontal motion

      float currentVx = _velocityOnGround.x;
      float desiredVx = _in.Move.x * _horizontalSpeed;

      _horizontalSpeedBuffer.Push(Mathf.Abs(_velocityOnGround.x));
      
      // Allows instant momentum flipping while on the ground
      if (Grounded && !Mathf.Approximately(0, desiredVx)) {
        if (desiredVx < 0) currentVx = -_horizontalSpeedBuffer.GetMax();
        else               currentVx = +_horizontalSpeedBuffer.GetMax();
      }

      // Do acceleration
      float acceleration = Mathf.Abs(desiredVx) > Mathf.Abs(currentVx) ? _accelerationGrounded : _decelerationGrounded;
      acceleration *= Grounded ? 1 : _midairHorizontalAccelerationMultiplier;
      float amountAccelerated = Mathf.Sign(desiredVx - currentVx) * acceleration * Time.fixedDeltaTime;
      float actualVelocityX = Math.Approach(currentVx, desiredVx, amountAccelerated);
      _velocityOnGround = new Vector2(actualVelocityX, _velocityOnGround.y);
    }

    private void handleJump()
    {
      // Store last grounded time for coyote-time purposes
      if (Grounded)
      {
        _lastGroundedTime = Time.time;
      }

      // Implements additional coyote time for falling off an RCBox while stationary
      if (_co.OnRCBox && Mathf.Abs(_rb.velocity.x) < _eps) _lastGroundedTime = Time.time + _additionalRCBoxCoyoteTime;

      // Jump if grounded or within coyote-time interval
      // WARN: This actually is called twice every time you press jump, due to the fact that 
      // the character technically won't leave the ground on the next fixed update due to 
      // our raycast down distance. 
      if (_in.Jump) // TODO: Remove this before commiting!
      // if (_in.Jump && (Grounded || Time.time - _coyoteTime < _lastGroundedTime))
      {
        // Instant diagonal jumping
        float jumpVx = _rb.velocity.x;
        float desiredVx = _in.Move.x * _horizontalSpeed;
        if (Mathf.Abs(jumpVx) < Mathf.Abs(desiredVx)) jumpVx = desiredVx;

        // since this jump code gets triggered twice, we want to capture
        // just the first time and only emit OnJump once. If you're on the ground
        // and jumping, supposely your velocity is negative or 0, hence this check.
        bool isRealJump = _rb.velocity.y <= 0.01f;

        _rb.velocity = new Vector2(jumpVx, _jumpSpeed);
        _lastGroundedTime = -Mathf.Infinity;
        _lastJumpedTime = Time.time;

        if (isRealJump) {
            // certain code are only run once per jump
            // if we don't do this, and this jump event playoneshot a sound effect
            // you'll hear two. Same with particle effects.
            OnJump?.Invoke();
        }
      }
    }

    private void handleMidairNudge()
    {
      // Do not nudge if falling; also reset the _alreadyNudged boolean
      if (_rb.velocity.y < _eps)
      {
        _alreadyNudged = false;
        return;
      }

      // Do not nudge if there's any significant horizontal velocity
      if (Mathf.Abs(_rb.velocity.x) >= _eps) return;

      // Do not nudge if already nudged
      if (_alreadyNudged) return;

      // Do not nudge if center two raycasts are obstructed
      if (_co.TopLeft || _co.TopRight) return;

      // Nudge if one, but not both, side raycasts are obstructed
      if (_co.TopLeftmost ^ _co.TopRightmost)
      {
        _alreadyNudged = true;
        _rb.position += _co.Nudge;
      }
    }

    private void handleGravity()
    {
      float multiplier = 1;

      // Implement "low jumps"
      if (_rb.velocity.y > 0 && !_in.JumpHeld) {
          multiplier *= _lowJumpMultiplier;
          if (Time.time - _lastJumpedTime <= _earlyJumpReleaseTime) {
              OnEarlyJumpRelease?.Invoke();
          }
          OnJumpRelease?.Invoke();
      }

      // Implement "lower gravity at peak of jump"
      if (Mathf.Abs(_rb.velocity.y) < _jumpPeakThreshold) multiplier *= _jumpPeakMultiplier;

      // Fall, but cap falling velocity
      _rb.velocity = new Vector2(
        _rb.velocity.x,
        Math.Approach(_rb.velocity.y, _maxFall, _gravity * multiplier * Time.deltaTime)
      );
    }

    private void handleMovementAnimation()
    {
      // Running/jumping
      float Vx = _velocityOnGround.x / _horizontalSpeed;
      float Vy = _velocityOnGround.y / _jumpSpeed;
      bool idle = Mathf.Abs(_velocityOnGround.x) < _eps;

      _sr.flipX = idle ? _sr.flipX : Vx < 0;

      _an.SetFloat("Vx", Vx);
      _an.SetFloat("Vy", Vy);
      _an.SetBool("Grounded", Grounded);

      // Landing effects
      if (_co.OnGround)
      {
        if (Time.time - _lastGroundedTime > _minTimeBetweenLandingEffects && Time.time - _lastJumpedTime > _minTimeLandAfterJump)
        {
          _onLandEvents?.Invoke();
          _ps.PlayLandingSquash();
        }

        _lastGroundedTime = Time.time;
      }
    }

    private IEnumerator waitForRespawn()
    {
      // Todo: particles, etc

      yield return new WaitForSeconds(_deathRespawnDelay);

      SceneTransitioner.Instance.Reload();
    }

    #endregion
  }
}
