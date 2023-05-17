using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Assertions;
using NaughtyAttributes;
using FMODUnity;

namespace Enter
{
  [RequireComponent(typeof(LineRenderer))]
  public class LaserObstacle : MonoBehaviour
  {
    [SerializeField, Tooltip("Duration in seconds for 'on' phase of laser cycle.")]
    private float _onDuration;

    [SerializeField, Tooltip("Duration in seconds for 'off' phase of laser cycle.")]
    private float _offDuration;

    [SerializeField, Tooltip("Duration in seconds before first 'on' phase of laser cycle.")]
    private float _delayDuration;

    [SerializeField, Tooltip("Number of raycasts to use to check for hits / blocks.")]
    private int _numRaycasts;

    [SerializeField, Tooltip("Width of laser beam.")]
    private float _width = 4.0f / 16.0f;

    [SerializeField, Tooltip("Whether the laser is permanently firing."), OnValueChanged("onPermanentlyOnChanged")]
    private bool _permanentlyOn;

    [SerializeField, ReadOnly, Tooltip("Whether the laser is currently firing.")]
    private bool _currentlyOn;

    private const float _skinWidth = 0.001f;

    private LineRenderer _lineRenderer;

    [SerializeField] private List<ParticleSystem> _chargingParticles;
    [SerializeField] private List<ParticleSystem> _attackParticles;
    [SerializeField, Required] private GameObject laserEnd;

    private const float _maxRaycastDistance = 100;
    [SerializeField] private LayerMask _gizmosBlockLaserLayer;

    [SerializeField] private StudioEventEmitter _laserSFX;
    [SerializeField] private StudioEventEmitter _laserChargeSFX;

    #region ================== Methods

    void Awake()
    {
      _lineRenderer = GetComponent<LineRenderer>();
      Assert.IsNotNull(_lineRenderer, "LaserObstacle must have a reference to its LineRenderer.");
    }

    void OnEnable()
    {
      if (_permanentlyOn)
      {
        turnOnLaser();
        return;
      }

      StartCoroutine(laserToggler());
    }

    void OnDisable()
    {
      StopAllCoroutines();
    }

    void Update()
    {
      if (_permanentlyOn || _currentlyOn) fireAndRenderLaser();
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
      float minHitDist_global = multiRaycastHelper(_maxRaycastDistance, _gizmosBlockLaserLayer);

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
#endif

    #endregion

    #region ================== Helpers

    private Vector2 _lineStart_local       => Vector2.zero + Vector2.down / 2.0f;
    private Vector2 _laserDirection_local  => Vector2.down;

    private Vector2 _lineStart_global      => transform.TransformPoint(_lineStart_local);
    private Vector2 _laserDirection_global => transform.TransformDirection(_laserDirection_local);

    private Vector2 getLaserStartPoint(int i)
    {
      float t = (float) i / (_numRaycasts - 1);

      float bufferedWidth = _width - _skinWidth * 2;

      Vector2 laserStart_local = new Vector2((t - 0.5f) * bufferedWidth, -0.5f - _skinWidth);

      Vector2 laserStart_global = transform.TransformPoint(laserStart_local);

      return laserStart_global;
    }

    private IEnumerator laserToggler()
    {
      yield return new WaitForSeconds(_delayDuration);
      while (true)
      {
        turnOnLaser();
        yield return new WaitForSeconds(_onDuration);
        turnOffLaser();
        yield return new WaitForSeconds(_offDuration);
      }
    }

    private void turnOnLaser()
    {
      _laserSFX?.Play();
      _laserSFX?.SetParameter("laser_off", 0);
      _currentlyOn = true;
      _lineRenderer.enabled = true;
      foreach (ParticleSystem attackParticleSystem in _attackParticles)
      {
        attackParticleSystem?.Play();
      }
      foreach (ParticleSystem particleSystem in _chargingParticles)
      {
        particleSystem?.Clear();
        particleSystem?.Stop();
      }
    }

    private void turnOffLaser()
    {
      _laserSFX?.SetParameter("laser_off", 1);
      _laserChargeSFX?.Play();
      _currentlyOn = false;
      _lineRenderer.enabled = false;
      foreach (ParticleSystem attackParticleSystem in _attackParticles)
      {
        attackParticleSystem?.Stop();
      }
      foreach (ParticleSystem particleSystem in _chargingParticles)
      {
        particleSystem?.Play();
      }
    }

    private void onPermanentlyOnChanged()
    {
      if (_permanentlyOn)
      {
        StopAllCoroutines();
        turnOnLaser();
      }
      else
      {
        StartCoroutine(laserToggler());
      }
    }

    private void fireAndRenderLaser()
    {
      // For firing the laser

      float minHitDist_global = multiRaycastHelper(_maxRaycastDistance, LayerManager.Instance.BlockLaserLayer);
	    multiRaycastHelper(minHitDist_global, LayerManager.Instance.PlayerLayer); // for killing the player :>

      // For rendering the laser

      Vector2 lineEnd_global = _lineStart_global + minHitDist_global * _laserDirection_global;
      Vector2 lineEnd_local  = transform.InverseTransformPoint(lineEnd_global);

      _lineRenderer.SetPosition(0, _lineStart_local);
      _lineRenderer.SetPosition(1, lineEnd_local);
      _lineRenderer.startWidth = _width;
      _lineRenderer.endWidth   = _width;
      laserEnd.transform.position = lineEnd_global;
    }

    private float multiRaycastHelper(float maxRaycastDistance, LayerMask layers)
    {
      float minHitDist_global = maxRaycastDistance;

      for (int i = 0; i < _numRaycasts; i++)
      {
        Vector2 laserStart_global = getLaserStartPoint(i);

        float thisHitDist_global = raycastHelper(laserStart_global, _laserDirection_global, layers, minHitDist_global);
        minHitDist_global = Mathf.Min(minHitDist_global, thisHitDist_global);
      }

      return minHitDist_global;
    }

    private float raycastHelper(Vector2 origin, Vector2 direction, LayerMask layers, float maxDistance = _maxRaycastDistance)
    {
      RaycastHit2D hit = Physics2D.Raycast(origin, direction, maxDistance, layers);

      if (!hit) return float.MaxValue;

      if (hit.collider.gameObject.tag == "Player") PlayerManager.PlayerScript.Die();

      return hit.distance;
    }
    
    #endregion
  }
}
