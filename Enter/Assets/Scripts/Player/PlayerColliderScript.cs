using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Assertions;
using System;
using NaughtyAttributes;

namespace Enter
{
  [DisallowMultipleComponent]
  public class PlayerColliderScript : MonoBehaviour
  {
    private PlayerStretcherScript _stretcher;
    private BoxCollider2D         _collider;
    private Rigidbody2D           _rigidbody;

    private const float _skinWidth = 0.001f;

    #region ================== Variables

    [Header("Layers")]
    [SerializeField] private LayerMask _staticLayer;
    [SerializeField] private LayerMask _movingLayer;
    [SerializeField] private LayerMask _rcBoxLayer;

    private LayerMask _allGroundLayers => _staticLayer | _movingLayer | _rcBoxLayer;

    [Header("Ground Raycast Position Tweaks")]
    [SerializeField] private float _groundRayDistance = 0.02f;
    [SerializeField, Min(2)] private int _numGroundRays = 2;

    [Header("Overhead Raycast Position Tweaks")]
    [SerializeField] private float _overheadRayDistance = 0.5f;
    [SerializeField] private float _outerFrac = 1.02f;
    [SerializeField] private float _innerFrac = 0.25f;

    private Vector2 _groundRayOffset   => new Vector2(0, -_groundRayDistance);
    private Vector2 _overheadRayOffset => new Vector2(0, +_overheadRayDistance);

    [Header("Checking For 'Landing' Effects")]
    [SerializeField] private float _minTimeBetweenLandingEffects = 0.25f;

    private float _lastGroundedTime = -Mathf.Infinity;

    #endregion

    #region ================== Accessors

    [field: SerializeField] public bool OnGround { get; private set; }
    [field: SerializeField] public bool OnRCBox  { get; private set; }

    [field: SerializeField] public bool TopLeftmost  { get; private set; }
    [field: SerializeField] public bool TopLeft      { get; private set; }
    [field: SerializeField] public bool TopRight     { get; private set; }
    [field: SerializeField] public bool TopRightmost { get; private set; }

    [field: SerializeField, ReadOnly] public Rigidbody2D CarryingRigidbody { get; private set; }

    private Vector2 _nudge = Vector2.zero;
    public Vector2 Nudge { get { return _nudge; } }

    public UnityEvent OnLand;

    #endregion

    #region ================== Methods
    
    void Start()
    {
      _stretcher = PlayerManager.PlayerStretcherScript;
      _collider  = PlayerManager.BoxCollider;
      _rigidbody = GetComponent<Rigidbody2D>();

      Assert.IsNotNull(_stretcher, "PlayerColliderScript must have a reference to a PlayerStretcherScript.");
      Assert.IsNotNull(_collider,  "PlayerColliderScript must have a reference to a BoxCollider2D.");
    }

    void FixedUpdate()
    {
      handleDownwardsChecks();
      handleUpwardsChecks();

      // Play particles
      if (OnGround)
      {
        if (Time.time - _lastGroundedTime > _minTimeBetweenLandingEffects)
        {
          OnLand?.Invoke();
          _stretcher.PlayLandingSquash();
        }

        _lastGroundedTime = Time.time;
      }
    }

#if UNITY_EDITOR
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
#endif

    #endregion

    #region ================== Helpers

    private void handleDownwardsChecks()
    {
      // This prevents moving boxes "in-line" with the ground from moving you:
      // If all raycasts hit static ground or RC box, use the first non-moving collider found's rigidbody (prefer RC box)
      // If the above is not true,                    use the first     moving collider found's rigidbody, if any
      // 
      // This sets variables required by PlayerScript:
      // If any raycast hits static ground, moving ground, or RC box, we are grounded
      // If any raycast hits RC box,                                  we are on RC box

      Rigidbody2D nonMovingRb = null;
      Rigidbody2D movingRb    = null;
      bool allGroundOrRC = true; // Starts true due to "and" logic
      bool anyHit        = false;
      bool anyRCHit      = false;

      for (int i = 0; i < _numGroundRays; i++)
      {    
        RaycastHit2D staticHit = Physics2D.Raycast(getGroundPoint(i), Vector2.down, _groundRayDistance, _staticLayer);
        RaycastHit2D movingHit = Physics2D.Raycast(getGroundPoint(i), Vector2.down, _groundRayDistance, _movingLayer);
        RaycastHit2D rcBoxHit  = Physics2D.Raycast(getGroundPoint(i), Vector2.down, _groundRayDistance, _rcBoxLayer);

        if (!nonMovingRb) nonMovingRb = rcBoxHit.collider?.GetComponent<Rigidbody2D>() ?? staticHit.collider?.GetComponent<Rigidbody2D>();
        if (!movingRb)    movingRb    = movingHit.collider?.GetComponent<Rigidbody2D>();
        allGroundOrRC = allGroundOrRC && (staticHit || rcBoxHit);
        anyHit   = anyHit || staticHit || movingHit || rcBoxHit;
        anyRCHit = anyRCHit || rcBoxHit;
      }

      OnGround = anyHit;
      OnRCBox  = anyRCHit;
      CarryingRigidbody = allGroundOrRC ? nonMovingRb : movingRb;
    }

    private void handleUpwardsChecks()
    {
      Bounds bounds = _collider.bounds;

      Vector2 topCenter = (Vector2)bounds.center + Vector2.up * bounds.size.y / 2;

      Func<Vector2, bool> overheadCast = (origin) =>
      {
        return Physics2D.Raycast(origin, Vector2.up, _overheadRayDistance, _allGroundLayers);
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

    private Vector2 getOverheadPoint(float offsetFromCenterTop)
    {
      Vector2 topCenter = (Vector2)_collider.bounds.center + Vector2.up * _collider.bounds.size.y / 2;
      
      return topCenter +
        Vector2.right * offsetFromCenterTop * _collider.bounds.size.x / 2 +
        Vector2.down * _skinWidth;
    }

    private Vector2 getGroundPoint(int i)
    {
      Vector2 bottomLeft = (Vector2)_collider.bounds.min;
      float t = (float) i / (_numGroundRays - 1);

      return bottomLeft +
        Vector2.right * t * _collider.bounds.size.x +
        Vector2.up * _skinWidth;
    }

    #endregion
  }
}
