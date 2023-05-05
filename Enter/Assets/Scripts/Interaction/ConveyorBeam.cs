using Enter.Utils;
using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

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

    [SerializeField, Tooltip("The radius of the circle to check for whether or not a new box will be spawned")]
    private float _spawnCheckRadius = 0.5f;

    [SerializeField, Tooltip("Sprite of the conveyor beam. Used by the beam for scrolling at the same speed as the beam.")]
    private SpriteRenderer _sprite;

    private float _spacing => _beltSpeed * _spawnWaitTime;

    #region ================== Accessors

    public Vector2 ConveyorBeamVelocity => _beltSpeed * transform.right;

    #endregion

    #region ================== Methods

    void Awake()
    {
      Assert.IsNotNull(_co, "ConveyorBeam must have a reference to its BoxCollider2D.");
      Assert.IsNotNull(_sr, "ConveyorBeam must have a reference to its SpriteRenderer.");
      Assert.IsNotNull(_boxPrefab, "ConveyorBeam must have a reference to GameObject to spawn.");
    }

    void OnEnable()
    {
      SceneTransitioner.Instance.OnSceneLoad.AddListener(doStartupThings);
      SceneTransitioner.Instance.OnTransitionBefore.AddListener(doCleanupThings);
      SceneTransitioner.Instance.OnReloadBefore.AddListener(doCleanupThings);
    }

    void OnDisable()
    {
      SceneTransitioner.Instance.OnSceneLoad.RemoveListener(doStartupThings);
      SceneTransitioner.Instance.OnTransitionBefore.RemoveListener(doCleanupThings);
      SceneTransitioner.Instance.OnReloadBefore.RemoveListener(doCleanupThings);
    }

    void LateUpdate() {
        _sprite.material.SetFloat("_ScrollSpeed", _beltSpeed);
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

    private void doStartupThings(Scene scene)
    {
      if (scene != gameObject.scene) return;
      if (_prewarm) prewarm();
      StartCoroutine(spawnBoxes());
    }

    private void doCleanupThings(Scene scene)
    {
      if (scene != gameObject.scene) return;
      StopAllCoroutines();
    }

    private void prewarm()
    {
      List<float> spawnXs = new List<float>();
      float currX = _spacing * (1 - _initialWaitTime / _spawnWaitTime);
      while (currX < _beltLength)
      {
        spawnXs.Add(currX);
        currX += _spacing;
      }

      // Sneaky note: to prevent a tiny issue when blocking pre-warmed boxes,
      //              you should spawn these in reverse order (downstream first)
      //              so that they'll have their FixedUpdates called first
      
      spawnXs.Reverse();
      foreach (float x in spawnXs) spawnBoxAt(transform.position + transform.right * x);
    }

    private IEnumerator spawnBoxes()
    {
      yield return new WaitForSeconds(_initialWaitTime);

      while (true)
      {
        // check if any conveyor box is overlapping with the spawn point
        bool conveyorBoxOverlapped = Physics2D.OverlapCircle(
          transform.position, 
          _spawnCheckRadius,
          LayerManager.Instance.ConveyorBoxLayer);
        
        if (!conveyorBoxOverlapped) spawnBoxAt(transform.position);

        yield return new WaitForSeconds(_spawnWaitTime);
      }
    }

    private void spawnBoxAt(Vector3 inputPosition)
    {
      // only spawn new boxes if no overlapping
      GameObject obj = BubbleManager.Instance.Borrow(
        gameObject.scene,
        _boxPrefab,
        inputPosition,
        Quaternion.identity);

      // IMPORTANT: Set cut object's layer to ConveyorBox
      obj.layer = LayerMask.NameToLayer("ConveyorBox");
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
