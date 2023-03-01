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
    [SerializeField] private float _groundRayDistance = 0.02f;

    [Header("Overhead Raycast Position Tweaks")]
    [SerializeField] private float _overheadRayDistance = 0.5f;
    [SerializeField] private float _outerFrac = 1.02f;
    [SerializeField] private float _innerFrac = 0.25f;

    private Vector2 _groundRayOffset   => new Vector2(0, -_groundRayDistance);
    private Vector2 _overheadRayOffset => new Vector2(0, +_overheadRayDistance);

    #region ================== Accessors

    [field:SerializeField] public bool OnGround { get; private set; }
    [field:SerializeField] public bool OnRCBox  { get; private set; }

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
      OnGround = groundCheckHelper(_solidLayers);
      OnRCBox  = groundCheckHelper(_rcBoxLayer);

      TopLeftmost  = raycastHelper(-_halfW * _outerFrac, _halfH, Vector2.up, _overheadRayDistance, _solidLayers);
      TopLeft      = raycastHelper(-_halfW * _innerFrac, _halfH, Vector2.up, _overheadRayDistance, _solidLayers);
      TopRight     = raycastHelper(+_halfW * _innerFrac, _halfH, Vector2.up, _overheadRayDistance, _solidLayers);
      TopRightmost = raycastHelper(+_halfW * _outerFrac, _halfH, Vector2.up, _overheadRayDistance, _solidLayers);

      if (TopLeftmost ^ TopRightmost) 
      {
        _nudge.x = (_outerFrac - _innerFrac) * _halfW * (TopLeftmost ? 1 : -1);
      }
    }

    void OnDrawGizmos()
    {
      Gizmos.color = Color.red;

      drawLineHelper(-_halfW, -_halfH, _groundRayOffset);
      drawLineHelper(+_halfW, -_halfH, _groundRayOffset);

      drawLineHelper(-_halfW * _outerFrac, +_halfH, _overheadRayOffset);
      drawLineHelper(-_halfW * _innerFrac, +_halfH, _overheadRayOffset);
      drawLineHelper(+_halfW * _innerFrac, +_halfH, _overheadRayOffset);
      drawLineHelper(+_halfW * _outerFrac, +_halfH, _overheadRayOffset);
    }

    #endregion

    #region ================== Helpers

    private Vector2 getOffsetPosition(float v1, float v2)
    {
      return (Vector2)transform.position + new Vector2(v1, v2);
    }

    private bool raycastHelper(float localX, float localY, Vector2 direction, float distance, LayerMask layers)
    {
      return Physics2D.Raycast(getOffsetPosition(localX, localY), direction, distance, layers).collider != null;
    }

    private bool groundCheckHelper(LayerMask layers)
    {
      bool a = raycastHelper(-_halfW, -_halfH, -Vector2.up, _groundRayDistance, layers);
      bool b = raycastHelper(+_halfW, -_halfH, -Vector2.up, _groundRayDistance, layers);
      
      return a || b;
    }

    private void drawLineHelper(float localX, float localY, Vector2 offset)
    {
      Vector2 start = getOffsetPosition(localX, localY);
      Gizmos.DrawLine(start, start + offset);
    }

    #endregion
  }
}