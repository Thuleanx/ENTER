using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerColliderScript))]
public class PlayerScript : MonoBehaviour
{
  private Rigidbody2D          _rb;
  private PlayerColliderScript _co;

  private InputData _in;

  [Header("Movement")]

  [SerializeField, Tooltip("Maximum horizontal speed.")]
  private float _speed = 5;

  [SerializeField, Tooltip("Maximum initial upward velocity when a jump is started.")]
  private float _jumpSpeed = 12;

  [SerializeField, Tooltip("Acceleration due to gravity.")]
  private float _gravity = -35;

  [SerializeField, Tooltip("Maximum speed when falling due to gravity.")]
  private float _maxFall = -10;

  [SerializeField, Tooltip("Gravity multiplier for when the jump input is not held down.")]
  private float _lowJumpMultiplier = 2.5f;

  [SerializeField, Tooltip("Time after becoming ungrounded by any means other than jumping, during which jumping is permitted.")]
  private float _coyoteTime = 0.1f;
  private float _lastGroundedTime = -Mathf.Infinity;

  // ================== Methods

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

	// ================== Helpers

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
    // Do not nudge if grounded or falling
    if (_co.OnGround || _rb.velocity.y < Mathf.Epsilon) return;

    // Do not nudge if center two raycasts are obstructed
    if (_co.TopLeft || _co.TopRight) return;

    // Nudge
    if (_co.TopLeftmost && !_co.TopRightmost) _rb.position += _co.ToRightNudge;
    if (!_co.TopLeftmost && _co.TopRightmost) _rb.position += _co.ToLeftNudge;
  }

  private void handleGravity()
  {
    float multiplier = 1;

    // Implement "low jumps"
    if (_rb.velocity.y > 0 && !_in.Jump) multiplier = _lowJumpMultiplier;

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
}
