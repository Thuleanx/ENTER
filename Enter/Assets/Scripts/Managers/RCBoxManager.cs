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

    [SerializeField] private float _lastRCTime = -Mathf.Infinity;
    [SerializeField] private float _minRCInterval = 0.1f;

    public GameObject SelectedObject = null;
    private RigidbodyConstraints2D SelectedObjectInitialConstraints;
    public GameObject CutObject = null;
    private RigidbodyConstraints2D CutObjectInitialConstraints;

    private Vector2 _pasteTLOffset = new Vector2(-0.95f, 0.95f);

    public bool CanCutPaste => SceneTransitioner.Instance.CurrSpawnPoint.CanCutPaste;

    public Sprite RCSprite { set { _rcSpriteRenderer.sprite = value; } }

    public bool MouseIsOverLeft;
    public bool MouseIsOverRight;
    
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
      updateCutPasteVisuals();
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
        bool shouldCountRightClick =
          (Time.time - _minRCInterval > _lastRCTime) &&
          Physics2D.OverlapPoint((Vector2) _in.MouseWorld, LayerManager.Instance.RCAreaLayer);

        if (shouldCountRightClick)
        {
          _in.RDown = false;
          rightClick();
        }
      }

      if (_rc.activeSelf) findAndFreezeSelectedObject();
    }

    void OnDisable()
    {
      CursorManager.Instance.HoveringEntities.Remove(GetInstanceID());
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
      // Left-clicking elsewhere disables the RCBox
      bool isOnRCBox = Physics2D.OverlapPoint((Vector2) _in.MouseWorld, LayerManager.Instance.RCBoxLayer);
      if (!isOnRCBox)
      {
        disableRCBox();
        return;
      }

      // Prevent cutting/pasting if not enabled yet
      if (!CanCutPaste) return;

      // Else, cut/paste based on position
      if (_rc.transform.position.x > _in.MouseWorld.x) cut();
      else                                             paste();
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

      CursorManager.Instance.SetCursor(CursorType.Pointer);
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

      CursorManager.Instance.SetCursor(CursorType.Pointer);
      disableRCBox();
    }

    private Vector3 getRCBoxPosition()
    {
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
      
      Collider2D collider = Physics2D.OverlapCircle((Vector2) _rc.transform.position, 0.05f, LayerManager.Instance.CuttableLayer);
      if (collider != null)
      {
        SelectedObject = collider.gameObject;
        SelectedObjectInitialConstraints = SelectedObject.GetComponent<Rigidbody2D>().constraints;
        SelectedObject.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
      }
    }

    private void updateCutPasteVisuals()
    {
      if (!CanCutPaste)
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
        RCSprite = MouseIsOverLeft ? _rcSprite20 : _rcSprite10;
        if (MouseIsOverLeft) CursorManager.Instance.SetCursor(CursorType.Hover);
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
        RCSprite = MouseIsOverRight ? _rcSprite02 : _rcSprite01;
        if (MouseIsOverRight) CursorManager.Instance.SetCursor(CursorType.Hover);
        return;
      }
    }

    #endregion
  }
}
