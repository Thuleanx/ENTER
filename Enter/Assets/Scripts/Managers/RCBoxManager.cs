using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

namespace Enter
{
  [System.Serializable]
  public struct TileInfo
  {
    public bool       valid;
    public TileBase   tileBase;
    public Tilemap    tilemap;
    public Vector3Int indices;
  }

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
    public TileInfo TileInfoForDelete = new TileInfo();

    private Vector2 _pasteTLOffset = new Vector2(-0.95f, 0.95f);

    private bool _canCutPaste   => SceneTransitioner.Instance.CanCutPaste;
    private bool _canRCAnywhere => SceneTransitioner.Instance.CanRCAnywhere;
    private bool _canDelete     => SceneTransitioner.Instance.CanDelete;

    public Sprite RCSprite { set { _rcSpriteRenderer.sprite = value; } }

    [SerializeField] public UnityEvent<Vector2> OnRightClick;
    [SerializeField] public UnityEvent<Vector2> OnLeftClick;
    [SerializeField] public UnityEvent<Vector2> OnCut;
    [SerializeField] public UnityEvent<Vector2> OnPaste;
    [SerializeField] public UnityEvent<Vector2> OnDelete;
    
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

      if (_canCutPaste && _rc.activeSelf) findAndFreezeSelectedObject();
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

    private void updateRCBoxSprite()
    {
      bool isOnRCBox = !PauseManager.Instance.IsPaused &&
        Physics2D.OverlapPoint((Vector2) _in.MouseWorld, LayerManager.Instance.RCBoxLayer);

      bool mouseIsOver      = isOnRCBox;
      bool mouseIsOverLeft  = isOnRCBox && _in.MouseWorld.x <  _rc.transform.position.x;
      bool mouseIsOverRight = isOnRCBox && _in.MouseWorld.x >= _rc.transform.position.x;

      CursorManager.Instance.HoveringEntities.Remove(gameObject);

      // Can delete
      if (_canDelete)
      {
        // Nothing to delete
        if (!TileInfoForDelete.valid)
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

    private void leftClick()
    {
      bool isOnRCBox = Physics2D.OverlapPoint((Vector2) _in.MouseWorld, LayerManager.Instance.RCBoxLayer);
      OnLeftClick?.Invoke(_in.MouseWorld);

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
      Vector2 targetPosition = decideRCBoxSpawnPosition();
      if (_rc.activeSelf && targetPosition == (Vector2) _rc.transform.position) return;
      OnRightClick?.Invoke(_in.MouseWorld);

      // If the RCBox is already present, make it disappear
      if (_rc.activeSelf) disableRCBox();

      // Spawn in at new location
      enableRCBox(targetPosition);
    }

    #region ================== Helpers: Deleting

    private void delete()
    {
      // Prevent deleting nothing
      if (!TileInfoForDelete.valid)
      {
        Debug.Log("Nothing to delete.");
        return;
      }

      OnDelete?.Invoke(_in.MouseWorld);

      // Flood-delete tiles (todo)
      recursivelyDeleteTiles(TileInfoForDelete.tilemap, TileInfoForDelete.indices);
      TileInfoForDelete.valid = false;

      disableRCBox();
    }

    private void recursivelyDeleteTiles(Tilemap tilemap, Vector3Int indices)
    {
      Queue<Vector3Int> toDelete = new Queue<Vector3Int>();
      toDelete.Enqueue(indices);

      while (toDelete.Count > 0)
      {
        Vector3Int currentIndices = toDelete.Dequeue();
        tilemap.SetTile(currentIndices, null);

        bool up    = (tilemap.GetTile(currentIndices + Vector3Int.up)    != null);
        bool down  = (tilemap.GetTile(currentIndices + Vector3Int.down)  != null);
        bool left  = (tilemap.GetTile(currentIndices + Vector3Int.left)  != null);
        bool right = (tilemap.GetTile(currentIndices + Vector3Int.right) != null);

        if (up)    toDelete.Enqueue(currentIndices + Vector3Int.up);
        if (down)  toDelete.Enqueue(currentIndices + Vector3Int.down);
        if (left)  toDelete.Enqueue(currentIndices + Vector3Int.left);
        if (right) toDelete.Enqueue(currentIndices + Vector3Int.right);
      }
    }

    #endregion

    #region ================== Helpers: Cutting and Pasting

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

      OnCut?.Invoke(_in.MouseWorld);
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
      OnPaste.Invoke(_in.MouseWorld);

      disableRCBox();
    }

    #endregion

    #region ================== Helpers: Spawning/Despawning the RCBox

    private Vector3 decideRCBoxSpawnPosition()
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
      if (_canCutPaste) findAndFreezeSelectedObject();
      if (_canDelete)   findTileInfoForDelete();

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

      if (TileInfoForDelete.valid) TileInfoForDelete.valid = false;
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

    private void findTileInfoForDelete()
    {
      if (TileInfoForDelete.valid) return;

      TileInfo          found = getTileInfoAtRCBoxOffset(new Vector2(-0.5f, +0.5f));
      if (!found.valid) found = getTileInfoAtRCBoxOffset(new Vector2(+0.5f, +0.5f));
      if (!found.valid) found = getTileInfoAtRCBoxOffset(new Vector2(-0.5f, -0.5f));
      if (!found.valid) found = getTileInfoAtRCBoxOffset(new Vector2(+0.5f, -0.5f));
        
      if (found.valid) TileInfoForDelete = found;
    }

    private TileInfo getTileInfoAtRCBoxOffset(Vector2 offset)
    {
      TileInfo tileInfo = new TileInfo();

      Vector2 searchLocation = (Vector2) _rc.transform.position + offset;
      Collider2D collider = Physics2D.OverlapPoint(searchLocation, LayerManager.Instance.CorruptedLayer);

      if (!collider) return tileInfo;

      tileInfo.valid    = true;
      tileInfo.tilemap  = collider.GetComponent<Tilemap>();
      tileInfo.indices  = tileInfo.tilemap.layoutGrid.WorldToCell(searchLocation);
      tileInfo.tileBase = tileInfo.tilemap.GetTile(tileInfo.indices);

      return tileInfo;
    }

    #endregion

    #endregion
  }
}
