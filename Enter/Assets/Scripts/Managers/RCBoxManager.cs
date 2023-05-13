using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

namespace Enter
{
  [DisallowMultipleComponent]
  public class RCBoxManager : MonoBehaviour
  {
    public static RCBoxManager Instance;

    private InputData _in => InputManager.Instance.Data;

    [SerializeField] private GameObject     _rc;
    [SerializeField] private SpriteRenderer _rcSpriteRenderer;

    [SerializeField] private Sprite _rcSprite00;
    [SerializeField] private Sprite _rcSprite01; // Paste
    [SerializeField] private Sprite _rcSprite10; // Cut
    [SerializeField] private Sprite _rcSprite02; // Paste hover
    [SerializeField] private Sprite _rcSprite20; // Cut   hover
    [SerializeField] private Sprite _rcSprite03; // Paste blocked
    [SerializeField] private Sprite _rcSprite30; // Cut   blocked
    [SerializeField] private Sprite _rcSpriteD1; // Delete
    [SerializeField] private Sprite _rcSpriteD2; // Delete hover
    [SerializeField] private Sprite _rcSpriteD3; // Delete blocked

    [SerializeField] private float _lastRCTime = -Mathf.Infinity;
    [SerializeField] private float _minRCInterval = 0.1f;

    public GameObject SelectedObject = null;
    private RigidbodyConstraints2D SelectedObjectInitialConstraints;
    public GameObject CutObject = null;
    private RigidbodyConstraints2D CutObjectInitialConstraints;

    private Vector2 _pasteTLOffset = new Vector2(-0.95f, 0.95f);

    private bool _canCutPaste   => SceneTransitioner.Instance.CurrSpawnPoint.CanCutPaste;
    private bool _canRCAnywhere => SceneTransitioner.Instance.CurrSpawnPoint.CanRCAnywhere;
    private bool _canDelete     => SceneTransitioner.Instance.CurrSpawnPoint.CanDelete;

    public Sprite RCSprite { set { _rcSpriteRenderer.sprite = value; } }
    
    #region ================== Methods

    void Awake()
    {
      Instance = this;
      Assert.IsNotNull(_rc,               "RCBoxManager must have a reference to GameObject RCBox.");
      Assert.IsNotNull(_rcSpriteRenderer, "RCBoxManager must have a reference to RCBox's SpriteRenderer.");
      RCSprite = _rcSprite00;
    }

    void LateUpdate()
    {
      updateRCBoxSprite();
    }

    void FixedUpdate()
    {
      if (_in.LDown)
      {
        bool shouldCountLeftClick = _rc.activeSelf;

        if (shouldCountLeftClick)
        {
          _in.LDown = false;
          leftClick();
        }
      }
      else if (_in.RDown)
      {
        bool hitArea = Physics2D.OverlapPoint((Vector2) _in.MouseWorld, LayerManager.Instance.RCAreaLayer);
        bool shouldCountRightClick = (Time.time - _minRCInterval > _lastRCTime) && (hitArea || _canRCAnywhere);

        if (shouldCountRightClick)
        {
          _in.RDown = false;
          rightClick();
        }
      }

      if (_rc.activeSelf) findAndFreezeSelectedObject();
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
      if (!_rc.activeSelf) return;

      bool isPastingIntoConveyorBeam = Physics2D.OverlapArea(
        (Vector2) _rc.transform.position + _pasteTLOffset,
        (Vector2) _rc.transform.position - _pasteTLOffset,
        LayerManager.Instance.ConveyorBeamLayer);

      Gizmos.color = isPastingIntoConveyorBeam ? Color.red : Color.green;

      Gizmos.DrawLine(
        (Vector2) _rc.transform.position + _pasteTLOffset,
        (Vector2) _rc.transform.position - _pasteTLOffset);
    }
#endif

    // Used as an event in SceneTransitioner
    public void DespawnRCBox()
    {
      CutObject = null;
      disableRCBox();
    }

    #endregion

    #region ================== Helpers

    private void leftClick()
    {
      bool isOnRCBox = Physics2D.OverlapPoint((Vector2) _in.MouseWorld, LayerManager.Instance.RCBoxLayer);

      if      (!isOnRCBox) disableRCBox();
      else if (_canDelete) delete();
      else if (_canCutPaste)
      {
        if (_in.MouseWorld.x < _rc.transform.position.x) cut();
        else                                             paste();
      }
    }

    private void rightClick()
    {
      // Do nothing if attempting to spawn at same approximate location
      Vector2 targetPosition = getRCBoxPosition();
      if (_rc.activeSelf && targetPosition == (Vector2) _rc.transform.position) return;

      // If the RCBox is already present, make it disappear
      if (_rc.activeSelf) disableRCBox();

      // Spawn in at new location
      enableRCBox(targetPosition);
    }

    private void cut()
    {
      // Prevent cutting multiple things
      if (CutObject != null)
      {
        Debug.Log("Already cut something.");
        return;
      }
      
      // Prevent cutting nothing
      if (SelectedObject == null)
      {
        Debug.Log("Nothing to cut.");
        return;
      }

      CutObject = SelectedObject;
      CutObjectInitialConstraints = SelectedObjectInitialConstraints;
      SelectedObject = null;
      CutObject.SetActive(false);

      disableRCBox();
    }

    private void paste()
    {
      // Prevent pasting if nothing was cut
      if (CutObject == null)
      {
        Debug.Log("Nothing to paste.");
        return;
      }

      // Prevent pasting if another object is in the way
      if (SelectedObject != null)
      {
        Debug.Log("Something is in the way.");
        return;
      }

      // IMPORTANT: If cut object is box, toggle behaviour accordingly
      Box boxScript = CutObject.GetComponent<Box>();
      if (boxScript != null)
      {
        bool isPastingIntoConveyorBeam = Physics2D.OverlapArea(
          (Vector2) _rc.transform.position + _pasteTLOffset,
          (Vector2) _rc.transform.position - _pasteTLOffset,
          LayerManager.Instance.ConveyorBeamLayer);
        boxScript.IsPhysicsBox = !isPastingIntoConveyorBeam;
      }
      
      CutObject.transform.SetPositionAndRotation(_rc.transform.position, Quaternion.identity);
      CutObject.GetComponent<Rigidbody2D>().constraints = CutObjectInitialConstraints;
      CutObject.SetActive(true);
      CutObject = null;

      disableRCBox();
    }

    private void delete()
    {
      // Prevent deleting nothing
      if (SelectedObject == null)
      {
        Debug.Log("Nothing to delete.");
        return;
      }

      SelectedObject.SetActive(false);
      SelectedObject = null;

      disableRCBox();
    }

    private Vector3 getRCBoxPosition()
    {
      if (_canRCAnywhere) 
      {
        return new Vector3(Mathf.Round(_in.MouseWorld.x), Mathf.Round(_in.MouseWorld.y), 0);
      }

      Vector2 closest = FindObjectOfType<RCAreaScript>().FindClosestValidPoint(_in.MouseWorld);
      return new Vector3(closest.x, closest.y, 0);
    }

    private void enableRCBox(Vector2 targetPosition)
    {
      _rc.transform.position = targetPosition;

      // Find and freeze currently selected object
      findAndFreezeSelectedObject();

      // Create shockwave
      ShockwaveManager.Instance.SpawnAtPos(targetPosition);

      _rc.SetActive(true);
      _lastRCTime = Time.time;
    }

    private void disableRCBox()
    {
      _rc.SetActive(false);

      if (SelectedObject != null)
      {
        SelectedObject.GetComponent<Rigidbody2D>().constraints = SelectedObjectInitialConstraints;
        SelectedObject = null;
      }
    }

    private void findAndFreezeSelectedObject()
    {
      if (SelectedObject != null) return;
      
      // Option 1: offset slightly down to avoid selecting things above the RCBox
      // Collider2D collider = Physics2D.OverlapPoint(
      //   (Vector2) _rc.transform.position - new Vector2(0, 0.01f),
      //   LayerManager.Instance.CuttableLayer);

      // Option 2: be more generous with selection
      Collider2D collider = Physics2D.OverlapCircle(
        (Vector2) _rc.transform.position,
        0.05f,
        LayerManager.Instance.CuttableLayer);

      if (collider != null)
      {
        SelectedObject = collider.gameObject;
        SelectedObjectInitialConstraints = SelectedObject.GetComponent<Rigidbody2D>().constraints;
        SelectedObject.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
      }
    }

    private void updateRCBoxSprite()
    {
      bool isOnRCBox = !PauseManager.Instance.IsPaused &&
        Physics2D.OverlapPoint((Vector2) _in.MouseWorld, LayerManager.Instance.RCBoxLayer);

      bool mouseIsOver      = isOnRCBox;
      bool mouseIsOverLeft  = isOnRCBox && _in.MouseWorld.x <  _rc.transform.position.x;
      bool mouseIsOverRight = isOnRCBox && _in.MouseWorld.x >= _rc.transform.position.x;

      CursorManager.Instance.HoveringEntities.Remove(gameObject);

      if (_canDelete)
      {
        // Nothing to delete
        if (SelectedObject == null)
        {
          RCSprite = _rcSpriteD3;
          return;
        }

        // Something to delete
        RCSprite = mouseIsOver ? _rcSpriteD2 : _rcSpriteD1;
        if (mouseIsOver) CursorManager.Instance.HoveringEntities.Add(gameObject);
        return;
      }

      // Cannot cut/paste
      if (!_canCutPaste)
      {
        RCSprite = _rcSprite00;
        return;
      }

      // Can cut
      if (CutObject == null)
      {
        // Nothing to cut
        if (SelectedObject == null)
        {
          RCSprite = _rcSprite30;
          return;
        }

        // Something to cut
        RCSprite = mouseIsOverLeft ? _rcSprite20 : _rcSprite10;
        if (mouseIsOverLeft) CursorManager.Instance.HoveringEntities.Add(gameObject);
        return;
      }

      // Can paste
      if (CutObject != null)
      {
        // Blocked from pasting
        if (SelectedObject != null)
        {
          RCSprite = _rcSprite03;
          return;
        }

        // Free to paste
        RCSprite = mouseIsOverRight ? _rcSprite02 : _rcSprite01;
        if (mouseIsOverRight) CursorManager.Instance.HoveringEntities.Add(gameObject);
        return;
      }
    }

    #endregion
  }
}
