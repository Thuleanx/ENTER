using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine;

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
    private PlayerAnimator       _pa;
    private InputData            _in;

    private const float _eps = 0.001f;

    #region ================== Variables

    [Header("Movement")]

    [SerializeField, Tooltip("Maximum horizontal speed.")]
    private float _speed;

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

    #endregion

    #region ================== Methods

    void Awake()
    {
      Instance = this;
    }

    void Start()
    {
      _rb = GetComponent<Rigidbody2D>();
      _co = GetComponent<PlayerColliderScript>();
      _pa = GetComponent<PlayerAnimator>();
      _pa.setMaxJumpVelocity(_jumpSpeed);
      _in = InputManager.Instance.Data;
    }

    void FixedUpdate()
    {
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
      _rb.velocity = new Vector2(_in.Move.x * _speed, _rb.velocity.y);
    }

    private void handleJump()
    {
      // Store last grounded time for coyote-time purposes
      if (_co.OnGround)
      {
        _lastGroundedTime = Time.time;
      }

      _pa.setJumpProgressFromYVelocity(_rb.velocity);

      // Implements additional coyote time for falling off an RCBox while stationary
      if (_co.OnRCBox && Mathf.Abs(_rb.velocity.x) < _eps) _lastGroundedTime = Time.time + _additionalRCBoxCoyoteTime;

      // Jump if grounded or within coyote-time interval
      if (_in.Jump && (_co.OnGround || Time.time - _coyoteTime < _lastGroundedTime))
      {
        _rb.velocity = new Vector2(_rb.velocity.x, _jumpSpeed);
        Debug.Log(_rb.velocity);
        Debug.Log(_gravity);

        // _pa.jump();
        // _isGrounded = false; // TODO: Bad?

        _lastGroundedTime = -Mathf.Infinity;
      }
    }

    private void landOnGround()
    {
      Debug.Log("Landed on ground");
      _isGrounded = _co.OnGround;
      _pa.land();
      //TODO: Add particles
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
      approach(_rb.velocity.y, _maxFall, _gravity * multiplier * Time.deltaTime));
    }

    private float approach(float start, float stop, float c)
    {
      if (start < stop) return Mathf.Min(start + c, stop);

      return Mathf.Max(start + c, stop);
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