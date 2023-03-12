using UnityEngine;
using System;

namespace Enter
{
  [RequireComponent(typeof(BoxCollider2D))]
  public class PlayerColliderScript : MonoBehaviour
  {
    [SerializeField] private ParticleSystem dust;

    private BoxCollider2D _collider;

    const int NUM_GROUND_RAY_CAST = 2; // don't go beneath 2, otherwise you get NANs

    [Header("Layers")]
    [SerializeField] private LayerMask _solidLayers;
    [SerializeField] private LayerMask _rcBoxLayer;

    [Header("Ground Linecast Position Tweaks")]
    [SerializeField] private float _groundRayDistance = 0.02f;

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

    private Vector2 _nudge = Vector2.zero;
    [property: SerializeField] public Vector2 Nudge { get { return _nudge; } }

    private float _lastGroundedTime;

    #endregion

    #region ================== Methods

    void Awake()
    {
      _collider = GetComponent<BoxCollider2D>();
      _lastGroundedTime = Time.time;
    }

    void FixedUpdate()
    {
      OnGround = GroundCheckHelper(_solidLayers);
      OnRCBox = GroundCheckHelper(_rcBoxLayer);

      if (OnGround)
      {
        if (Time.time - _lastGroundedTime > 0.65)
        {
          dust.Play();
        }
        _lastGroundedTime = Time.time;
      }

      Bounds bound = _collider.bounds;

      Vector2 topCenter = (Vector2)bound.center + Vector2.up * bound.size.y / 2;

      Func<Vector2, bool> overheadCast = (src) =>
      {
        return Physics2D.Raycast(src, Vector2.up, _overheadRayDistance, _solidLayers);
      };

      TopLeftmost = overheadCast(GetOverheadPoint(-_outerFrac, bound));
      TopLeft = overheadCast(GetOverheadPoint(-_innerFrac, bound));
      TopRight = overheadCast(GetOverheadPoint(_innerFrac, bound));
      TopRightmost = overheadCast(GetOverheadPoint(_outerFrac, bound));

      if (TopLeftmost ^ TopRightmost)
      {
        _nudge.x = (_outerFrac - _innerFrac) * bound.size.x / 2 * (TopLeftmost ? 1 : -1);
      }
    }

    void OnDrawGizmos()
    {
      Bounds bound = _collider.bounds;

      Action<Vector2, bool> overheadGizmoDraw = (src, isHitting) =>
      {
        Gizmos.color = isHitting ? Color.green : Color.red; // red if not hitting, green otherwise
        Gizmos.DrawLine(src, src + Vector2.up * _overheadRayOffset);
      };

      overheadGizmoDraw(GetOverheadPoint(-_outerFrac, bound), TopLeftmost);
      overheadGizmoDraw(GetOverheadPoint(-_innerFrac, bound), TopLeft);
      overheadGizmoDraw(GetOverheadPoint(_innerFrac, bound), TopRight);
      overheadGizmoDraw(GetOverheadPoint(_outerFrac, bound), TopRightmost);

      Gizmos.color = OnGround || OnRCBox ? Color.green : Color.red;
      Action<Vector2> groundGizmoDraw = (src) =>
      {
        Gizmos.DrawLine(src, src + Vector2.down * _groundRayDistance);
      };

      Vector2 bottomLeft = (Vector2)bound.min;
      for (int i = 0; i < NUM_GROUND_RAY_CAST; i++)
      {
        float t = i / (NUM_GROUND_RAY_CAST - 1);
        Vector2 src = bottomLeft + t * bound.size.x * Vector2.right; // usually you want to add a skin width up if the engine doesn't 
        groundGizmoDraw(src);
      }
    }

    #endregion

    #region ================== Helpers

    private Vector2 GetOverheadPoint(float offsetFromCenterTop, Bounds bound)
    {
      return (Vector2)bound.center + Vector2.up * bound.size.y / 2 + Vector2.right * offsetFromCenterTop * bound.size.x / 2;
    }

    private bool GroundCheckHelper(LayerMask layers)
    {
      bool collidingWithGround = false;

      Bounds bound = Collider.bounds;

      Vector2 bottomLeft = (Vector2)bound.min;

      for (int i = 0; i < NUM_GROUND_RAY_CAST; i++)
      {
        float t = i / (NUM_GROUND_RAY_CAST - 1);
        Vector2 src = bottomLeft + t * bound.size.x * Vector2.right; // usually you want to add a skin width up if the engine doesn't 
        collidingWithGround |= Physics2D.Raycast(src, Vector2.down, _groundRayDistance, layers);
        if (collidingWithGround) break;
      }

      return collidingWithGround;
    }

    #endregion
  }
}