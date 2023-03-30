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

    private InputData _in;

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

    [SerializeField, Tooltip("Time window in which highest grounded horizontal velocity is stored.")]
    private float _horizontalVelocityBufferDuration = 0.25f;
    private TimedDataBuffer<float> _horizontalVelocityBuffer;

    #endregion

    #region ====== Vertical Movement

    [Header("Vertical Movement")]

    [SerializeField, Tooltip("Maximum height for a jump.")]
    private float _jumpHeight;

    [SerializeField, Tooltip("Maximum time before the maximum jump height of the player is reached.")]
    private float _timeToMaxHeight;

    [SerializeField, Tooltip("Maximum speed when falling due to gravity.")]
    private float _maxFall;

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

    [SerializeField, Tooltip("Additional coyote time after falling off an RCBox while stationary. This is added to coyote time.")]
    private float _additionalRCBoxCoyoteTime;

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

    private Vector2 _velocityOfGround {
      get { return (_co.OnGround && _co.CarryingRigidbody) ? _co.CarryingRigidbody.velocity : Vector2.zero; }
    }

    private Vector2 _velocityOnGround {
      get { return _rb.velocity - _velocityOfGround; }
      set { _rb.velocity = value + _velocityOfGround; }
    }

    #endregion

    #endregion

    #region ================== Accessors

    public Rigidbody2D           Rigidbody2D           => _rb;
    public BoxCollider2D         BoxCollider2D         => _bc;
    public PlayerStretcherScript PlayerStretcherScript => _ps;
    public PlayerColliderScript  PlayerColliderScript  => _co;
    public SpriteRenderer        SpriteRenderer        => _sr;
    public Animator              Animator              => _an;
    public float                 MaxJumpSpeed          => _jumpSpeed;
    public float                 MaxFallSpeed          => _maxFall;

    public UnityEvent OnJump;
    
    [Header("Checking For 'Landing' Effects")]
    [SerializeField] private float _minTimeBetweenLandingEffects = 0.25f;

    #endregion

    #region ================== Methods

    void Awake()
    {
      // This prevents multiple copies of the player from existing at once
      if (Instance) Destroy(this);
      Instance = this;

      _rb = GetComponent<Rigidbody2D>();
      _bc = GetComponent<BoxCollider2D>();
      _co = GetComponent<PlayerColliderScript>();

      _horizontalVelocityBuffer = new TimedDataBuffer<float>(_horizontalVelocityBufferDuration);
      _ps = GetComponent<PlayerStretcherScript>();
    }

    void Start()
    {
      _in = InputManager.Instance.Data;
    }

    void FixedUpdate()
    {
      _maxFall = -_jumpSpeed; // fixme: delete

      if (_isDead) return;

      handleMovement();
    }

    public void Die()
    {
      StartCoroutine(die());
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

    private float _vx = 0;
    private float _vy = 0;

    private void handleMovement()
    {
      handleWalk();
      handleJump();
      handleMidairNudge();
      handleGravity();
      
      // TO FIX: currently buggy due to weird x velocity when stopping
      float Vx = _velocityOnGround.x / _horizontalSpeed;
      float Vy = _velocityOnGround.y / _jumpSpeed;
      bool idle = Mathf.Abs(Vx) < _eps;
      _sr.flipX = idle ? _sr.flipX : Vx < 0;
      /* Debug.Log("_vx = " + _vx + ", Vx = " + Vx + "; idle = " + (idle) + "; grounded = " + _co.OnGround); */
      _an.SetFloat("Vx", Vx);
      _an.SetFloat("Vy", Vy);
      _vx = Vx;
      _vy = Vy;
      _an.SetBool("Grounded", _co.OnGround);
    }

    private void handleWalk()
    {

      _horizontalVelocityBuffer.Push(_velocityOnGround.x);
      // Handles horizontal motion
      float currentVelocityX = _velocityOnGround.x;
      float desiredVelocityX = _in.Move.x * _horizontalSpeed; 

      // allows turnaround to be free / happens instantaneously
      if (!Mathf.Approximately(desiredVelocityX, 0) && Mathf.Approximately(Mathf.Sign(desiredVelocityX) * Mathf.Sign(currentVelocityX), -1)) {
          if (desiredVelocityX < 0)     currentVelocityX = _horizontalVelocityBuffer.GetMin();
          else                          currentVelocityX = _horizontalVelocityBuffer.GetMax();
      }

      float acceleration = Mathf.Abs(desiredVelocityX) > Mathf.Abs(currentVelocityX) ? _accelerationGrounded : _decelerationGrounded;
      float mult = _co.OnGround ? 1 : _midairHorizontalAccelerationMultiplier;

      float amountAccelerated = Mathf.Sign(desiredVelocityX - currentVelocityX) * acceleration * mult * Time.fixedDeltaTime;
      float actualVelocityX = Math.Approach(currentVelocityX, desiredVelocityX, amountAccelerated);

      _velocityOnGround = new Vector2(actualVelocityX, _velocityOnGround.y);
      // _rb.velocity = new Vector2(_in.Move.x * _horizontalSpeed, _rb.velocity.y);
    }

    private void handleJump()
    {
      // Store last grounded time for coyote-time purposes
      if (_co.OnGround)
      {
        _lastGroundedTime = Time.time;
      }

      // Implements additional coyote time for falling off an RCBox while stationary
      if (_co.OnRCBox && Mathf.Abs(_rb.velocity.x) < _eps) _lastGroundedTime = Time.time + _additionalRCBoxCoyoteTime;

      // Jump if grounded or within coyote-time interval
      if (_in.Jump && (_co.OnGround || Time.time - _coyoteTime < _lastGroundedTime))
      {
        _rb.velocity = new Vector2(_rb.velocity.x, _jumpSpeed);
        _lastGroundedTime = -Mathf.Infinity;
        OnJump?.Invoke();
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
      if (_rb.velocity.y > 0 && !_in.Jump) multiplier *= _lowJumpMultiplier;

      // Implement "lower gravity at peak of jump"
      if (Mathf.Abs(_rb.velocity.y) < _jumpPeakThreshold) multiplier *= _jumpPeakMultiplier;

      // Fall, but cap falling velocity
      _rb.velocity = new Vector2(
        _rb.velocity.x,
        Math.Approach(_rb.velocity.y, _maxFall, _gravity * multiplier * Time.deltaTime)
      );
    }

    private IEnumerator die()
    {
      if (_isDead) yield break;

      _isDead = true;
      _rb.velocity = Vector2.zero;

      yield return new WaitForSeconds(_deathRespawnDelay);
      yield return SceneTransitioner.Instance.Reload();
      _isDead = false;
    }

    #endregion
  }
}
