using Enter.Utils;
using NaughtyAttributes;
using System;
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
    
    [SerializeField, Tooltip("The speed that boxes will be set to."), Min(0.01f)]
    private float _beltSpeed = 2.5f;

    [SerializeField, Tooltip("If set to true, will spawn boxes on enable, and initial wait time becomes nothing more than a phase offset.")]
    private bool _prewarm = true;

    [SerializeField, Tooltip("Delay before spawning the first box at the spawn location. If prewarm is true, this becomes nothing more than a phase offset.")]
    private float _initialWaitTime = 0;

    [SerializeField, Tooltip("Time between spawning boxes at the spawn location"), Min(0.01f)]
    private float _spawnWaitTime = 2;

    [SerializeField, Tooltip("The radius of the circle to check for whether or not a new box will be spawned")]
    private float _spawnCheckRadius = 0.5f;

    [SerializeField, Tooltip("Whether or not to spawn each box flush with the previous one. Will ignore wait time if true."), OnValueChanged("onSpawnFlushChange")]
    private bool _spawnFlush = false;
    private ConveyorBox _previousFlushConveyorBox;
    // We'll make a dangerous assumption that the conveyor beam will never get backed up in _spawnFlush mode.
    // To keep in sync with the despawning, we will just spawn a new box when one despawns, instead of waiting.

    [SerializeField, Tooltip("Sprite of the conveyor beam. Used by the beam for scrolling at the same speed as the beam.")]
    private SpriteRenderer _sprite;

    private float _spacing => _spawnFlush ? 2 : _beltSpeed * _spawnWaitTime;

    private bool _isSpawning = true;
    private HashSet<ConveyorBox> _conveyorBoxes = new HashSet<ConveyorBox>();

    #region ================== Accessors

    public Vector2 ConveyorBeamVelocity => _beltSpeed * transform.right;

    public bool IsSpawning
    {
      get { return _isSpawning; }
      set
      {
        if (value) throw new Exception("Shouldn't happen. Who let you start spawning, lol?");
        _isSpawning = false;
      }
    }

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
      DoStartupThings(SceneManager.GetActiveScene());
      SceneTransitioner.Instance.OnSceneLoad.AddListener(DoStartupThings);
      SceneTransitioner.Instance.OnTransitionBefore.AddListener(DoCleanupThings);
      SceneTransitioner.Instance.OnReloadBefore.AddListener(DoCleanupThings);
    }

    void OnDisable()
    {
      DoCleanupThings(SceneManager.GetActiveScene());
      SceneTransitioner.Instance.OnSceneLoad.RemoveListener(DoStartupThings);
      SceneTransitioner.Instance.OnTransitionBefore.RemoveListener(DoCleanupThings);
      SceneTransitioner.Instance.OnReloadBefore.RemoveListener(DoCleanupThings);
    }

    void LateUpdate()
    {
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

    public void OnBoxExit(ConveyorBox cb)
    {
      if (_isSpawning && _spawnFlush) spawnBoxAt(transform.position);
      if (cb.CurrentConveyorBeam == this) _conveyorBoxes.Remove(cb);
    }

    public void DoStartupThings(Scene scene)
    {
      if (scene != gameObject.scene) return;
      if (_prewarm) prewarm();
      StartCoroutine(spawnBoxes());
    }

    public void DoCleanupThings(Scene scene)
    {
      if (scene != gameObject.scene) return;
      StopAllCoroutines();
      foreach (ConveyorBox cb in _conveyorBoxes) cb.gameObject.SetActive(false);
    }

    #endregion
    
    #region ================== Helpers


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

      while (_isSpawning && !_spawnFlush)
      {
        // Check if any conveyor box is overlapping with the spawn point
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
      // Only spawn new boxes if no overlapping
      GameObject obj = BubbleManager.Instance.Borrow(
        gameObject.scene,
        _boxPrefab,
        inputPosition,
        Quaternion.identity);

      // IMPORTANT: Toggle behaviour accordingly
      obj.GetComponent<Box>().IsPhysicsBox = false;

      ConveyorBox cb = obj.GetComponent<ConveyorBox>();
      cb.CurrentConveyorBeam = this;
      _conveyorBoxes.Add(cb);

      // Handle flush-spawning
      if (_spawnFlush)
      {
        if (_previousFlushConveyorBox != null) _previousFlushConveyorBox.UpstreamConveyorBox = cb;

        cb.DownstreamConveyorBox = _previousFlushConveyorBox;
        _previousFlushConveyorBox = cb;
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

    private void onSpawnFlushChange()
    {
      if (!_spawnFlush) _previousFlushConveyorBox = null;
    }

    #endregion
  }
}
