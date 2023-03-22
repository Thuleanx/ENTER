using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace Enter
{
  [RequireComponent(typeof(LineRenderer))]
  public class LaserObstacle : MonoBehaviour
  {
    [SerializeField] private float _onDuration;
    [SerializeField] private float _offDuration;
    [SerializeField] private bool  _on;
    [SerializeField] private int   _numRaycasts;

    private float _onTimer;
    private float _offTimer;

    private LineRenderer _lineRenderer;

    [SerializeField] private ParticleSystem _particleSystem;

    private const float _maxRaycastDistance = 100;
    [SerializeField] LayerMask _groundMask;
    [SerializeField] LayerMask _playerMask;

    #region ================== Methods

    void Awake()
    {
      _lineRenderer = GetComponent<LineRenderer>();
    }

    void Start()
    {
      StartCoroutine(laserToggler());
    }

    void Update()
    {
      if (_on) fireAndRenderLaser();
    }

    void OnDrawGizmos()
    {
      float minHitDist_global = multiRaycastHelper();

      Gizmos.color = Color.red;
      Action<Vector2> laserGizmoDraw = (laserStart_global) =>
      {
        Gizmos.DrawLine(
          laserStart_global,
          laserStart_global + _laserDirection_global * minHitDist_global);
      };

      for (int i = 0; i < _numRaycasts; i++)
      {
        laserGizmoDraw(getLaserStartPoint(i));
      }
    }

    #endregion

    #region ================== Helpers

    private Vector2 _lineStart_local       => Vector2.zero + Vector2.down / 2.0f;
    private Vector2 _laserDirection_local  => Vector2.down;

    private Vector2 _lineStart_global      => transform.TransformPoint(_lineStart_local);
    private Vector2 _laserDirection_global => transform.TransformDirection(_laserDirection_local);

    private Vector2 getLaserStartPoint(int i)
    {
      float t = (float) i / (_numRaycasts - 1);

      Vector2 laserStart_global = transform.TransformPoint(new Vector2(-0.5f + t, -0.5f -Mathf.Epsilon));

      return laserStart_global;
    }

    private IEnumerator laserToggler()
    {
      while (true)
      {
        _on = true;
        _lineRenderer.enabled = true;
        _particleSystem.Clear();
        _particleSystem.Stop();
        
        yield return new WaitForSeconds(_onDuration);

        _on = false;
        _lineRenderer.enabled = false;
        _particleSystem.Play();
        
        yield return new WaitForSeconds(_offDuration);
      }
    }

    private void fireAndRenderLaser()
    {
      // For firing the laser

      float minHitDist_global = multiRaycastHelper();

      // For rendering the laser

      Vector2 lineEnd_global = _lineStart_global + minHitDist_global * _laserDirection_global;
      Vector2 lineEnd_local  = transform.InverseTransformPoint(lineEnd_global);

      _lineRenderer.SetPosition(0, _lineStart_local);
      _lineRenderer.SetPosition(1, lineEnd_local);
    }

    private float multiRaycastHelper()
    {
      float minHitDist_global = _maxRaycastDistance;

      for (int i = 0; i < _numRaycasts; i++)
      {
        Vector2 laserStart_global = getLaserStartPoint(i);

        float thisHitDist_global = raycastHelper(laserStart_global, _laserDirection_global, _groundMask);
        minHitDist_global = Mathf.Min(minHitDist_global, thisHitDist_global);

        // This kills the player if hit
        raycastHelper(laserStart_global, _laserDirection_global, _playerMask);
      }

      return minHitDist_global;
    }

    private float raycastHelper(Vector2 origin, Vector2 direction, LayerMask layers)
    {
      RaycastHit2D hit = Physics2D.Raycast(origin, direction, _maxRaycastDistance, layers);

      if (!hit) return _maxRaycastDistance;

      if (hit.collider.gameObject.tag == "Player") PlayerManager.PlayerScript.Die();

      return hit.distance;
    }
    
    #endregion
  }
}
