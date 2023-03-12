using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Enter
{
  public class LaserObstacle : Interactable
  {
    [SerializeField] private float _onDuration;
    [SerializeField] private float _offDuration;

    [SerializeField] private bool _on;
    [SerializeField] private int _numRaycasts;

    private float _onTimer;
    private float _offTimer;
    private float _laserLowest;
    private float _floorHeight;

    private SpriteRenderer _spriteRenderer;
    private BoxCollider2D _boxCollider;
    private LineRenderer _lineRenderer;

    [SerializeField] private ParticleSystem _particleSystem;

    private const float _maxRaycastDistance = 100;
    [SerializeField] LayerMask _groundMask;
    [SerializeField] LayerMask _playerMask;

    #region ================== Methods

    void Awake()
    {
      _spriteRenderer = GetComponent<SpriteRenderer>();
      _boxCollider = GetComponent<BoxCollider2D>();
      _lineRenderer = GetComponent<LineRenderer>();
    }

    void Start()
    {
      _on = true;
      _onTimer = _onDuration;

      _particleSystem.Clear();
      _particleSystem.Stop();
    }

    void Update()
    {
      _lineRenderer.SetPosition(0, Vector2.zero);
      float nearestHitDist = Mathf.Infinity;

      if (_on)
      {
        _onTimer -= Time.deltaTime;

        // Perform raycasts to determine the nearest object hit
        for (int i = 0; i < _numRaycasts + 1; i++)
        {
          float offset = -transform.localScale.x / 2;
          offset += ((float)i / _numRaycasts) * transform.localScale.x;

          Vector2 src = transform.position + transform.right * offset;
          float newHitDist = shootRaycast(src, _groundMask);
          shootRaycast(src, _playerMask); // for killing the player
          if (newHitDist < nearestHitDist)
            nearestHitDist = newHitDist;
        }

        Debug.Log(nearestHitDist);

        // Turn the laser off when on time ends
        if (_onTimer <= 0)
        {
          _on = false;
          _offTimer = _offDuration;
          _lineRenderer.enabled = false;
          _particleSystem.Play();
        }

        _lineRenderer.SetPosition(1, nearestHitDist * -((Vector2)transform.up));
      }
      else
      {
        _offTimer -= Time.deltaTime;
        // Turn the laser back on when off time ends
        if (_offTimer <= 0)
        {
          _on = true;
          _onTimer = _onDuration;
          _lineRenderer.enabled = true;
          _particleSystem.Clear();
          _particleSystem.Stop();
        }
      }
    }

    #endregion

    #region ================== Helpers

    private float shootRaycast(Vector2 origin, LayerMask layerMask)
    {
      RaycastHit2D hit = Physics2D.Raycast(origin, -transform.up, _maxRaycastDistance, layerMask);

      if (!hit) return Mathf.Infinity;

      if (hit.collider.gameObject.tag == "Player")
      {
        PlayerManager.PlayerScript.Die();
      }

      return hit.distance;
    }

    #endregion
  }
}
