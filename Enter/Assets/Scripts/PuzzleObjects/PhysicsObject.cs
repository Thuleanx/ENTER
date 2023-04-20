using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Enter
{
  public class PhysicsObject : MonoBehaviour
  {
    [SerializeField, Tooltip("Lifetime in seconds, before entering a tractor beam. -1 = Infinite.")]
    public float PreTractorLifetime = 5;

    [SerializeField, Tooltip("Lifetime in seconds, after leaving all tractor beams. -1 = Infinite.")]
    public float PostTractorLifetime = 5;

    private Rigidbody2D _rb;
    private float _initialGravity;
    private int _numTractorsColliding = 0;

    void OnEnable()
    {
      _rb = GetComponent<Rigidbody2D>();
      _initialGravity = _rb.gravityScale;
      //_rb.gravityScale = 0;
      StartCoroutine(disableAfterTime(PreTractorLifetime));
    }

    void OnDisable()
    {
      StopAllCoroutines();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
      if (LayerManager.Instance.IsInLayerMask(LayerManager.Instance.PhysicsBeamLayer, other.gameObject))
      {
        if (_numTractorsColliding++ == 0)
        {
          _rb.gravityScale = 0;
          StopAllCoroutines();
        }
      }
    }

    void OnTriggerExit2D(Collider2D other)
    {
      if (LayerManager.Instance.IsInLayerMask(LayerManager.Instance.PhysicsBeamLayer, other.gameObject))
      {
        if (--_numTractorsColliding == 0)
        {
          _rb.gravityScale = _initialGravity;

          // Must check self and all parents are active, as OnTriggerExit2D can be called on disabling any of them
          if (gameObject.activeInHierarchy) StartCoroutine(disableAfterTime(PostTractorLifetime));
        }
      }
    }

    private IEnumerator disableAfterTime(float t)
    {
      if (!Mathf.Approximately(0, t) && t < 0) yield break;

      yield return new WaitForSeconds(t);
      gameObject.SetActive(false);
    }
  }
}