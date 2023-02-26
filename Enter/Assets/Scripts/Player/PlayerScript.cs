using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Enter
{
  [RequireComponent(typeof(Rigidbody2D))]
  [RequireComponent(typeof(PlayerColliderScript))]
  public class PlayerScript : MonoBehaviour
  {
    static PlayerScript Instance = null;
    private Rigidbody2D          _rb;
    private PlayerColliderScript _co;
    private InputData            _in;

    #region ================== Variables

    [Header("Movement")]

    [SerializeField, Tooltip("Maximum horizontal speed.")]
    private float _speed = 5;

    [SerializeField, Tooltip("Maximum initial upward velocity when a jump is started.")]
    private float _jumpSpeed = 12;

    [SerializeField, Tooltip("Acceleration due to gravity.")]
    private float _gravity = -35;

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
    private float _coyoteTime = 0.15f;
    private float _lastGroundedTime = -Mathf.Infinity;

    private bool _alreadyNudged;

    #endregion

    #region ================== Methods

    private void Awake()
    {
      if (Instance) 
      {
        Destroy(gameObject);
      }
      else
      {
        transform.SetParent(null);
        Instance = this;
        DontDestroyOnLoad(gameObject);
      }
    }

    void Start()
    {
      _rb = GetComponent<Rigidbody2D>();
      _co = GetComponent<PlayerColliderScript>();
      _in = InputManager.Instance.Data;
    }

    void FixedUpdate()
    {
      handleMovement();
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
      if (_co.OnGround) _lastGroundedTime = Time.time;

      // Jump if grounded or within coyote-time interval
      if (_in.Jump && (_co.OnGround || Time.time - _coyoteTime < _lastGroundedTime))
      {
        _rb.velocity = new Vector2(_rb.velocity.x, _jumpSpeed);
        _lastGroundedTime = -Mathf.Infinity;
      }
    }

    private void handleMidairNudge()
    {
      // Do not nudge if falling
      if (_rb.velocity.y < 0.001f) 
      {
        _alreadyNudged = false;
        return;
      }

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

    #endregion
  }
}