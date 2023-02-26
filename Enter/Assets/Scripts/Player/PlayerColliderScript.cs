using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerColliderScript : MonoBehaviour
{
  [Header("Layers")]
  [SerializeField] private LayerMask _envLayer;

  [Header("Circle Collider Positioning")]
  [SerializeField] private Vector2 _bottomOffset    = new Vector2( 0, -0.25f);
  [SerializeField] private Vector2 _leftOffset      = new Vector2(-0.25f, 0);
  [SerializeField] private Vector2 _rightOffset     = new Vector2( 0.25f, 0);
  [SerializeField] private float   _collisionRadius = 0.1f;

  [Header("Raycast Positioning")]
  [SerializeField] private float _rayDistance = 0.5f;
  [SerializeField] private Vector2 _topLeftmostOffset  = new Vector2(-0.25f, 0.25f);
  [SerializeField] private Vector2 _topLeftOffset      = new Vector2(-0.1f,  0.25f);
  [SerializeField] private Vector2 _topRightOffset     = new Vector2( 0.1f,  0.25f);
  [SerializeField] private Vector2 _topRightmostOffset = new Vector2( 0.25f, 0.25f);

  // Funky syntax (this defines a getter for _lineOffset)
  private Vector2 _lineOffset => new Vector2(0, _rayDistance);

  // ================== Accessors

  [field:SerializeField] public bool OnGround    { get; private set; }
  [field:SerializeField] public bool OnRightWall { get; private set; }
  [field:SerializeField] public bool OnLeftWall  { get; private set; }

  [field:SerializeField] public bool TopLeftmost  { get; private set; }
  [field:SerializeField] public bool TopLeft      { get; private set; }
  [field:SerializeField] public bool TopRight     { get; private set; }
  [field:SerializeField] public bool TopRightmost { get; private set; }

  [property:SerializeField] public Vector2 ToRightNudge => _topLeftOffset  - _topLeftmostOffset;
  [property:SerializeField] public Vector2 ToLeftNudge  => _topRightOffset - _topRightmostOffset;

  // ================== Methods

  void FixedUpdate()
  {
    OnGround    = Physics2D.OverlapCircle(getOffsetPosition(_bottomOffset), _collisionRadius, _envLayer);
    OnLeftWall  = Physics2D.OverlapCircle(getOffsetPosition(_leftOffset),   _collisionRadius, _envLayer);
    OnRightWall = Physics2D.OverlapCircle(getOffsetPosition(_rightOffset),  _collisionRadius, _envLayer);

    TopLeftmost  = Physics2D.Raycast(getOffsetPosition(_topLeftmostOffset ), Vector2.up, _rayDistance, _envLayer).collider != null;
    TopLeft      = Physics2D.Raycast(getOffsetPosition(_topLeftOffset),      Vector2.up, _rayDistance, _envLayer).collider != null;
    TopRight     = Physics2D.Raycast(getOffsetPosition(_topRightOffset),     Vector2.up, _rayDistance, _envLayer).collider != null;
    TopRightmost = Physics2D.Raycast(getOffsetPosition(_topRightmostOffset), Vector2.up, _rayDistance, _envLayer).collider != null;
  }

  void OnDrawGizmos()
  {
    Gizmos.color = Color.red;

    Gizmos.DrawWireSphere(getOffsetPosition(_bottomOffset), _collisionRadius);
    Gizmos.DrawWireSphere(getOffsetPosition(_rightOffset),  _collisionRadius);
    Gizmos.DrawWireSphere(getOffsetPosition(_leftOffset),   _collisionRadius);

    Gizmos.DrawLine(getOffsetPosition(_topLeftmostOffset),  getOffsetPosition(_topLeftmostOffset)  + _lineOffset);
    Gizmos.DrawLine(getOffsetPosition(_topLeftOffset),      getOffsetPosition(_topLeftOffset)      + _lineOffset);
    Gizmos.DrawLine(getOffsetPosition(_topRightOffset),     getOffsetPosition(_topRightOffset)     + _lineOffset);
    Gizmos.DrawLine(getOffsetPosition(_topRightmostOffset), getOffsetPosition(_topRightmostOffset) + _lineOffset);
  }

  // ================== Helpers

  private Vector2 getOffsetPosition(Vector2 vec)
  {
    return (Vector2)transform.position + vec;
  }
}
