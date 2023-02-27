using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Enter
{
  public class PlayerColliderScript : MonoBehaviour
  {
    [Header("Layers")]
    [SerializeField] private LayerMask _solidLayers;
    [SerializeField] private LayerMask _rcBoxLayer;

    [Header("Collider Bounds")]
    [SerializeField] private float _halfH = 0.25f;
    [SerializeField] private float _halfW  = 0.25f;

    [Header("Ground Linecast Position Tweaks")]
    [SerializeField] private float _offset = 0.02f;

    [Header("Overhead Raycast Position Tweaks")]
    [SerializeField] private float _rayDistance = 0.5f;
    [SerializeField] private float _outerFrac = 1.02f;
    [SerializeField] private float _innerFrac = 0.25f;

    private Vector2 _lineOffset => new Vector2(0, _rayDistance);

    #region ================== Accessors

    [field:SerializeField] public bool OnGround { get; private set; }

    [field:SerializeField] public bool TopLeftmost  { get; private set; }
    [field:SerializeField] public bool TopLeft      { get; private set; }
    [field:SerializeField] public bool TopRight     { get; private set; }
    [field:SerializeField] public bool TopRightmost { get; private set; }

    private Vector2 _nudge = Vector2.zero;
    [property:SerializeField] public Vector2 Nudge { get { return _nudge; } }

    #endregion

    #region ================== Methods

    void FixedUpdate()
    {
      OnGround = Physics2D.Linecast(
        getOffsetPosition(-_halfW, -_halfH - _offset),
        getOffsetPosition(+_halfW, -_halfH - _offset),
        _solidLayers);

      TopLeftmost  = Physics2D.Raycast(getOffsetPosition(-_halfW * _outerFrac, _halfH), Vector2.up, _rayDistance, _solidLayers).collider != null;
      TopLeft      = Physics2D.Raycast(getOffsetPosition(-_halfW * _innerFrac, _halfH), Vector2.up, _rayDistance, _solidLayers).collider != null;
      TopRight     = Physics2D.Raycast(getOffsetPosition(+_halfW * _innerFrac, _halfH), Vector2.up, _rayDistance, _solidLayers).collider != null;
      TopRightmost = Physics2D.Raycast(getOffsetPosition(+_halfW * _outerFrac, _halfH), Vector2.up, _rayDistance, _solidLayers).collider != null;

      if (TopLeftmost ^ TopRightmost) 
      {
        _nudge.x = (_outerFrac - _innerFrac) * _halfW * (TopLeftmost ? 1 : -1);
      }
    }

    void OnDrawGizmos()
    {
      Gizmos.color = Color.red;

      Gizmos.DrawLine(
        getOffsetPosition(-_halfW, -_halfH - _offset),
        getOffsetPosition(+_halfW, -_halfH - _offset));

      Gizmos.DrawLine(getOffsetPosition(-_halfW * _outerFrac, _halfH), getOffsetPosition(-_halfW * _outerFrac, _halfH) + _lineOffset);
      Gizmos.DrawLine(getOffsetPosition(-_halfW * _innerFrac, _halfH), getOffsetPosition(-_halfW * _innerFrac, _halfH) + _lineOffset);
      Gizmos.DrawLine(getOffsetPosition(+_halfW * _innerFrac, _halfH), getOffsetPosition(+_halfW * _innerFrac, _halfH) + _lineOffset);
      Gizmos.DrawLine(getOffsetPosition(+_halfW * _outerFrac, _halfH), getOffsetPosition(+_halfW * _outerFrac, _halfH) + _lineOffset);
    }

    #endregion

    #region ================== Helpers

    private Vector2 getOffsetPosition(float v1, float v2)
    {
      return (Vector2)transform.position + new Vector2(v1, v2);
    }

    #endregion
  }
}