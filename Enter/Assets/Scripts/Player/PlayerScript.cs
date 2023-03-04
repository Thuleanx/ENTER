using System.Collections;
using System.Collections.Generic;
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
    private InputData            _in;

    private const float _eps = 0.001f;

    #region ================== Variables

    [Header("Movement")]

    [SerializeField, Tooltip("Maximum horizontal speed.")]
    private float _speed = 5;

    [SerializeField, Tooltip("Maximum height for a jump.")]
    private float _jumpHeight = 3;

    [SerializeField, Tooltip("Maximum time before the maximum jump height of the player is reached.")]
    private float _timeToMaxHeight = .5f;

    // Maximum initial upward velocity when a jump is started.
    private float _jumpSpeed;

    // Acceleration due to gravity.
    private float _gravity;

    [SerializeField, Tooltip("Maximum speed when falling due to gravity.")]
    private float _maxFall = -10;

    [Header("Movement Tweaks")]

    [SerializeField, Tooltip("Gravity multiplier for when absolute vertical velocity is less than threshold.")]
    private float _jumpPeakMultiplier = .5f;

    [SerializeField, Tooltip("Threshold for the above.")]
    private float _jumpPeakThreshold = 2f;

    [SerializeField, Tooltip("Gravity multiplier for when the jump input is not held down.")]
    private float _lowJumpMultiplier = 3f;

    [SerializeField, Tooltip("Time after becoming ungrounded by any means other than jumping, during which jumping is permitted.")]
    private float _coyoteTime = 0.05f;
    private float _lastGroundedTime = -Mathf.Infinity;

    [SerializeField, Tooltip("Additional coyote time after falling off an RCBox while stationary.")]
    private float _additionalRCBoxCoyoteTime = 0.1f;

    private bool _alreadyNudged;

    [Header("Death")]

    [SerializeField, Tooltip("Amount of time between the player dying and respawning.")]
    private float _deathRespawnDelay = 0.5f;

    private bool _isDead;

    #endregion

    #region ================== Methods

    void Awake()
    {
      Instance = this;
      calculateJumpConstants();
    }
    
    void Start()
    {
      _rb = GetComponent<Rigidbody2D>();
      _co = GetComponent<PlayerColliderScript>();
      _in = InputManager.Instance.Data;
    }

    void FixedUpdate()
    {
      if (_isDead) return;

      handleMovement();
    }

    public void Die()
    {
      StartCoroutine("die");
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
    
    private void calculateJumpConstants() {
      _gravity = -((2 * _jumpHeight) - _jumpPeakThreshold + (2 * _timeToMaxHeight * _jumpPeakThreshold)) / Mathf.Pow(_timeToMaxHeight, 2);
      _jumpSpeed = _jumpPeakThreshold + (-_gravity * _timeToMaxHeight) - (2 * _jumpPeakThreshold);
    }

    private void handleWalk()
    {
      // Handles horizontal motion
      _rb.velocity = new Vector2(_in.Move.x * _speed, _rb.velocity.y);
    }

    private void handleJump()
    {
      // calculateJumpConstants();
      // Store last grounded time for coyote-time purposes
      if (_co.OnGround) _lastGroundedTime = Time.time;

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
        approach(_rb.velocity.y, _maxFall, _gravity * multiplier * Time.deltaTime));
    }

    private float approach(float start, float stop, float c)
    {
      if (start < stop) return Mathf.Min(start + c, stop);
      
      return Mathf.Max(start + c, stop);
    }

    private IEnumerator die()
    {
      _isDead = true;
      _rb.velocity = Vector2.zero;

      Debug.Log("Oops, you died");

      yield return new WaitForSeconds(_deathRespawnDelay);

      _isDead = false;
      _rb.position = SceneTransitioner.Instance.SpawnPosition;
    }

    #endregion
  }
}