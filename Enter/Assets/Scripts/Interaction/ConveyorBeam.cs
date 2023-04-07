using Enter.Utils;
using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Enter
{
  public class ConveyorBeam : MonoBehaviour
  {
    [SerializeField, Tooltip("The conveyor beam box collider.")]
    private BoxCollider2D _co;

    [SerializeField, Tooltip("The conveyor beam area sprite. Must have rendering type set to tiled.")]
    private SpriteRenderer _sr;

    [SerializeField, Tooltip("The conveyor box to be spawned.")]
    private GameObject _boxPrefab;

    [SerializeField, Tooltip("The total length in tiles of the conveyor beam."),  Min(1), OnValueChanged("onBeltLengthChanged")]
    private float _beltLength = 5;
    
    [SerializeField, Tooltip("The speed that boxes will be set to."), Min(0)]
    private float _beltSpeed = 2.5f;

    [SerializeField, Tooltip("If set to true, will spawn boxes on enable, and initial wait time becomes nothing more than a phase offset.")]
    private bool _prewarm = true;

    [SerializeField, Tooltip("Delay before spawning the first box at the spawn location. If prewarm is true, this becomes nothing more than a phase offset.")]
    private float _initialWaitTime = 0;

    [SerializeField, Tooltip("Time between spawning boxes at the spawn location")]
    private float _spawnWaitTime = 2;

    private float _spacing => _beltSpeed * _spawnWaitTime;

    #region ================== Methods

    void Awake()
    {
      Assert.IsNotNull(_co, "ConveyorBeam must have a reference to its BoxCollider2D.");
      Assert.IsNotNull(_sr, "ConveyorBeam must have a reference to its SpriteRenderer.");
      Assert.IsNotNull(_boxPrefab, "ConveyorBeam must have a reference to GameObject to spawn.");
    }

    void Start()
    {
      if (_prewarm) prewarm();
      StartCoroutine(spawnBoxes());
    }
    
    void OnDisable()
    {
      StopAllCoroutines();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
      Rigidbody2D rb = other.attachedRigidbody;

      rb.gravityScale = 0;
    }

    void OnTriggerStay2D(Collider2D other)
    {
      Rigidbody2D rb = other.attachedRigidbody;
      
      // Forces velocity to always be in line with beam
      rb.velocity = _beltSpeed * transform.right;

      // Forces position to always be in line with beam
      rb.position = rb.position - Vector2.Dot(rb.position - (Vector2) transform.position, transform.up) * (Vector2) transform.up;
    }

    void OnTriggerExit2D(Collider2D other)
    {
      GameObject obj = other.gameObject;
      obj.SetActive(false);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
      if (!_prewarm) return;

      float currX = _spacing * (1 - _initialWaitTime / _spawnWaitTime);
      while (currX < _beltLength)
      {
        Gizmos.color = new Color(1, 0, 0, 0.5f);
        Gizmos.DrawCube(
          transform.position + transform.right * currX,
          new Vector3(2, 2, 0.1f));
        currX += _spacing;
      }
    }
#endif

    #endregion
    
    #region ================== Helpers

    private void prewarm()
    {
      float currX = _spacing * (1 - _initialWaitTime / _spawnWaitTime);
      while (currX < _beltLength)
      {
        GameObject obj = BubbleManager.Instance.Borrow(
          gameObject.scene,
          _boxPrefab,
          transform.position + transform.right * currX,
          Quaternion.identity);
        currX += _spacing;
      }
    }

    private IEnumerator spawnBoxes()
    {
      yield return new WaitForSeconds(_initialWaitTime);

      while (true)
      {
        GameObject obj = BubbleManager.Instance.Borrow(
          gameObject.scene,
          _boxPrefab,
          transform.position,
          Quaternion.identity);
        yield return new WaitForSeconds(_spawnWaitTime);
      }
    }

    private void onBeltLengthChanged()
    {
      Vector2 newSize = new Vector2(_beltLength, 2);
      _co.size = newSize;
      _sr.size = newSize;

      Vector2 offset = new Vector2(_beltLength / 2 - 1, 0);
      _co.offset                  = offset;
      _sr.transform.localPosition = offset;
    }

    #endregion
  }
}





      