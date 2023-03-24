using UnityEngine;
using System;
using NaughtyAttributes;

namespace Enter
{
  public class PlayerColliderScript : MonoBehaviour
  {
    [SerializeField] private ParticleSystem dust;
    [SerializeField] private BoxCollider2D _collider;

    [Header("Layers")]
    [SerializeField] private LayerMask _solidLayers;
    [SerializeField] private LayerMask _rcBoxLayer;

    [Header("Ground Raycast Position Tweaks")]
    [SerializeField] private float _groundRayDistance = 0.02f;
    [SerializeField, Min(2)] private int _numGroundRays = 2;

    [Header("Overhead Raycast Position Tweaks")]
    [SerializeField] private float _overheadRayDistance = 0.5f;
    [SerializeField] private float _outerFrac = 1.02f;
    [SerializeField] private float _innerFrac = 0.25f;

    private Vector2 _groundRayOffset => new Vector2(0, -_groundRayDistance);
    private Vector2 _overheadRayOffset => new Vector2(0, +_overheadRayDistance);

    #region ================== Accessors

    [field: SerializeField] public bool OnGround { get; private set; }
    [field: SerializeField] public bool OnRCBox { get; private set; }

    [field: SerializeField] public bool TopLeftmost { get; private set; }
    [field: SerializeField] public bool TopLeft { get; private set; }
    [field: SerializeField] public bool TopRight { get; private set; }
    [field: SerializeField] public bool TopRightmost { get; private set; }

    [field: SerializeField, ReadOnly] public Collider2D Carrying {get; private set; }
    [field: SerializeField, ReadOnly] public Rigidbody2D CarryingRigidBody {get; private set; }

    private Vector2 _nudge = Vector2.zero;
    [property: SerializeField] public Vector2 Nudge { get { return _nudge; } }

    private float _lastGroundedTime;

    #endregion

    #region ================== Methods

    void Awake()
    {
      _lastGroundedTime = Time.time;
    }

    void FixedUpdate()
    {
      RaycastHit2D groundHit = GroundCheckHelper(_solidLayers);
      RaycastHit2D rcBoxHit = GroundCheckHelper(_rcBoxLayer);
      OnGround = groundHit;
      OnRCBox  = rcBoxHit;

    // determine which hit correspond to object that would carry this collider
      RaycastHit2D carryingHit = groundHit;
      if (!carryingHit || (rcBoxHit && rcBoxHit.distance < groundHit.distance))
        carryingHit = rcBoxHit;

      Carrying = null;
      CarryingRigidBody = null;
      if (carryingHit) {
          Carrying = carryingHit.collider;
          CarryingRigidBody = Carrying.GetComponent<Rigidbody2D>();
      }

      if (OnGround)
      {
        if (Time.time - _lastGroundedTime > 0.65) dust.Play();

        _lastGroundedTime = Time.time;
      }

      Bounds bounds = _collider.bounds;

      Vector2 topCenter = (Vector2)bounds.center + Vector2.up * bounds.size.y / 2;

      Func<Vector2, bool> overheadCast = (origin) =>
      {
        return Physics2D.Raycast(origin, Vector2.up, _overheadRayDistance, _solidLayers);
      };

      TopLeftmost  = overheadCast(getOverheadPoint(-_outerFrac));
      TopLeft      = overheadCast(getOverheadPoint(-_innerFrac));
      TopRight     = overheadCast(getOverheadPoint(_innerFrac));
      TopRightmost = overheadCast(getOverheadPoint(_outerFrac));

      if (TopLeftmost ^ TopRightmost)
      {
        _nudge.x = (_outerFrac - _innerFrac) * bounds.size.x / 2 * (TopLeftmost ? 1 : -1);
      }
    }

    void OnDrawGizmos()
    {
      // Draw overhead gizmos
      
      Action<Vector2, bool> overheadGizmoDraw = (src, isHitting) =>
      {
        Gizmos.color = isHitting ? Color.green : Color.red; // red if not hitting, green otherwise
        Gizmos.DrawLine(src, src + Vector2.up * _overheadRayOffset);
      };

      overheadGizmoDraw(getOverheadPoint(-_outerFrac), TopLeftmost);
      overheadGizmoDraw(getOverheadPoint(-_innerFrac), TopLeft);
      overheadGizmoDraw(getOverheadPoint(_innerFrac),  TopRight);
      overheadGizmoDraw(getOverheadPoint(_outerFrac),  TopRightmost);

      // Draw ground gizmos

      Gizmos.color = OnGround || OnRCBox ? Color.green : Color.red;
      Action<Vector2> groundGizmoDraw = (src) =>
      {
        Gizmos.DrawLine(src, src + Vector2.down * _groundRayDistance);
      };

      for (int i = 0; i < _numGroundRays; i++)
      {         
        groundGizmoDraw(getGroundPoint(i));
      }
    }

    #endregion

    #region ================== Helpers

    private Vector2 getOverheadPoint(float offsetFromCenterTop)
    {
      return (Vector2)_collider.bounds.center +
        Vector2.up * _collider.bounds.size.y / 2 +
        Vector2.right * offsetFromCenterTop * _collider.bounds.size.x / 2;
    }

    private Vector2 getGroundPoint(int i)
    {
      float t = (float) i / (_numGroundRays - 1);

      // Todo? Usually you want to add a skin width up, if the engine doesn't
      return (Vector2)_collider.bounds.min +
        t * _collider.bounds.size.x * Vector2.right;
    }

    private RaycastHit2D GroundCheckHelper(LayerMask layers)
    {
      Bounds bound = _collider.bounds;
      Vector2 bottomLeft = (Vector2)bound.min;

      for (int i = 0; i < _numGroundRays; i++)
      {         
        if (Physics2D.Raycast(getGroundPoint(i), Vector2.down, _groundRayDistance, layers))
        {
          return Physics2D.Raycast(getGroundPoint(i), Vector2.down, _groundRayDistance, layers);
        };
      }

      return new RaycastHit2D();
    }

    #endregion
  }
}
