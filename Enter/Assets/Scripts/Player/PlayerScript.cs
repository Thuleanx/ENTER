using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine;
using Enter.Utils;

namespace Enter
{
  [DisallowMultipleComponent]
  [RequireComponent(typeof(Rigidbody2D))]
  [RequireComponent(typeof(PlayerColliderScript))]
  public class PlayerScript : MonoBehaviour
  {
    public static PlayerScript Instance = null;

    private Rigidbody2D          _rb;
    private PlayerColliderScript _co;
    private PlayerStretcher      _ps;
    private InputData            _in;

    [SerializeField] private SpriteRenderer _sr;
    [SerializeField] private Animator       _an;

    private const float _eps = 0.001f;

    #region ================== Variables

    [Header("Movement")]

    [SerializeField, Tooltip("Maximum horizontal speed.")]
    private float _speed;

    [SerializeField, Tooltip("Maximum horizontal acceleration. This does not have to be too high."), Range(0.01f,1)]
    private float _accelerationSecondsToMaxSpeed;

    [SerializeField, Tooltip("Maximum horizontal acceleration. This needs to be high to feel responsive."), Range(0.01f, 1)]
    private float _decellerationSecondsToZeroSpeed;

    [SerializeField, Tooltip("Multiplier to horizontal acceleration when in the air."), Range(0,1)]
    private float _airMult;

    [SerializeField, Tooltip("Maximum height for a jump.")]
    private float _jumpHeight;

    [SerializeField, Tooltip("Maximum time before the maximum jump height of the player is reached.")]
    private float _timeToMaxHeight;

    [SerializeField, Tooltip("Maximum speed when falling due to gravity.")]
    private float _maxFall;

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

    [SerializeField, Tooltip("Additional coyote time after falling off an RCBox while stationary.")]
    private float _additionalRCBoxCoyoteTime;

    private bool _alreadyNudged;

    [Header("Death")]

    [SerializeField, Tooltip("Amount of time between the player dying and respawning.")]
    private float _deathRespawnDelay = 0.5f;

    private bool _isDead;

    // Separate from player collider on ground so that we can see when it hit the ground
    private bool _isGrounded;

    #endregion

    #region ================== Math For Jump

    private float v0 => _jumpSpeed;
    private float vc => _jumpPeakThreshold;
    private float tm => _timeToMaxHeight;
    private float h  => _jumpHeight;
    private float g  => _gravity;
    
    // Maximum initial upward velocity when a jump is started.
    private float _jumpSpeed => (h + Mathf.Sqrt(Mathf.Pow(h, 2) + 2 * h * tm * vc - Mathf.Pow(tm, 2) * Mathf.Pow(vc, 2))) / tm;

    // Acceleration due to gravity.
    private float _gravity => -(v0 + vc) / tm;

    float _accelerationGrounded => _speed / _accelerationSecondsToMaxSpeed;
    float _deccelerationGrounded => _speed / _decellerationSecondsToZeroSpeed;

    #endregion

    #region ================== Accessors

    public Rigidbody2D Rigidbody2D { get { return _rb; } }

    public Vector2 velocityOfGround {
      get
      { 
        if (_co.Carrying && _co.CarryingRigidBody) return _co.CarryingRigidBody.velocity;

        return Vector2.zero;
      }
    }

    public Vector2 velocityOnGround {
      get { return _rb.velocity - velocityOfGround; }
      set { _rb.velocity = value + velocityOfGround; }
    }

    #endregion

    #region ================== Methods

    void Awake()
    {
      if (Instance) Destroy(this);

      Instance = this;
    }

    void Start()
    {
      _rb = GetComponent<Rigidbody2D>();
      _co = GetComponent<PlayerColliderScript>();
      _ps = GetComponent<PlayerStretcher>();
      _ps.MaxJumpSpeed = _jumpSpeed;
      _in = InputManager.Instance.Data;
    }

    void FixedUpdate()
    {
      // TEMPORARY
      _maxFall = -_jumpSpeed;

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
      float Vx = _rb.velocity.x / _speed;
      float Vy = _rb.velocity.y / _jumpSpeed;
      _sr.flipX = Mathf.Abs(Vx) < 0.01f ? _sr.flipX : Vx < 0;
      Debug.Log("_vx = " + _vx + ", Vx = " + Vx + "; test = " + (Mathf.Abs(Vx) < 0.01f) + "; flip = " + _sr.flipX);
      _an.SetFloat("Vx", Vx);
      _an.SetFloat("Vy", Vy);
      _vx = Vx;
      _vy = Vy;
    }

    private void handleWalk()
    {
      // Handles horizontal motion
      float currentVelocityX = velocityOnGround.x;
      float desiredVelocityX = _in.Move.x * _speed; 

      // allows turnaround to be free / happens instantaneously
      if (Mathf.Sign(desiredVelocityX) != 0 && Mathf.Sign(desiredVelocityX) != Mathf.Sign(currentVelocityX)) currentVelocityX *= -1;

      float acceleration = Mathf.Abs(desiredVelocityX) > Mathf.Abs(currentVelocityX) ? _accelerationGrounded : _deccelerationGrounded;
      float mult = _co.OnGround ? 1 : _airMult;

      float amountAccelerated = Mathf.Sign(desiredVelocityX - currentVelocityX) * acceleration * mult * Time.fixedDeltaTime;
      float actualVelocityX = Math.Approach(currentVelocityX, desiredVelocityX, amountAccelerated);

      velocityOnGround = new Vector2(actualVelocityX, velocityOnGround.y);
      // _rb.velocity = new Vector2(_in.Move.x * _speed, _rb.velocity.y);
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

      _isDead = false;
      _rb.position = SceneTransitioner.Instance.SpawnPosition;
    }

    #endregion
  }
}
