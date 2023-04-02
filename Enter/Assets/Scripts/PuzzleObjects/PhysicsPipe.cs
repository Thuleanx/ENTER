using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

namespace Enter
{
  public class PhysicsPipe : MonoBehaviour
  {
    [SerializeField, ShowAssetPreview] private GameObject _prefab;
    
    [SerializeField] private UnityEvent _onSpawn;

    [SerializeField] private float _initialWaitTime = 0;
    [SerializeField] private float _spawnWaitTime   = 2;

    [SerializeField] private Vector2 _initialVelocity_global = Vector2.zero;

    void Awake()
    {
      Assert.IsNotNull(_prefab, "PhysicsPipe must have a reference to GameObject to spawn.");
    }

    void OnEnable()
    {
      StartCoroutine(keepSpawning());
    }

    void OnDisable()
    {
      StopAllCoroutines();
    }

    private IEnumerator keepSpawning()
    {
      yield return new WaitForSeconds(_initialWaitTime);

      while (true)
      {
        GameObject obj = BubbleManager.Instance.Borrow(gameObject.scene, _prefab, transform.position, Quaternion.identity);
        Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
        if (rb) rb.velocity = _initialVelocity_global;

        _onSpawn?.Invoke();

        yield return new WaitForSeconds(_spawnWaitTime);
      }
    }
  }
}
