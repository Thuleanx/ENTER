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

    private static Color _goodColor = new Color(0.25f, 1, 0.25f, 1);
    private static Color _baseColor;

    private InputData _in => InputManager.Instance.Data;

    [SerializeField] private GameObject _rc;
    [SerializeField] private SpriteRenderer _rcLeftRenderer;
    [SerializeField] private SpriteRenderer _rcRightRenderer;

    [SerializeField] private float _lastRCTime = -Mathf.Infinity;
    [SerializeField] private float _minRCInterval = 0.1f;

    public GameObject SelectedObject = null;
    private RigidbodyConstraints2D SelectedObjectInitialConstraints;

    public GameObject CutObject = null;
    private RigidbodyConstraints2D CutObjectInitialConstraints;

    private Vector2 _pasteTLOffset = new Vector2(-0.95f, 0.95f);

    #region ================== Methods

    void Awake()
    {
      Instance = this;
      Assert.IsNotNull(_rc, "RCBoxManager must have a reference to GameObject RCBox.");
      Assert.IsNotNull(_rcLeftRenderer,  "RCBoxManager must have a reference to RCBox's left SpriteRenderer.");
      Assert.IsNotNull(_rcRightRenderer, "RCBoxManager must have a reference to RCBox's right SpriteRenderer.");
      
      _baseColor = _rcLeftRenderer.color;
    }

    void LateUpdate() {
        bool shouldCountRightClick =
          Physics2D.OverlapPoint((Vector2) _in.MouseWorld, LayerManager.Instance.RCAreaLayer) && Time.timeScale != 0;

        if (shouldCountRightClick)  CursorManager.Instance.HoveringEntities.Add(GetInstanceID());
        else                        CursorManager.Instance.HoveringEntities.Remove(GetInstanceID());
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

      if (_in.RDown)
      {
        bool shouldCountRightClick =
          (Time.time - _minRCInterval > _lastRCTime) &&
          Physics2D.OverlapPoint((Vector2) _in.MouseWorld, LayerManager.Instance.RCAreaLayer);
        ShockwaveManager.Instance.SpawnAtPos(_in.MouseWorld);

        if (shouldCountRightClick)
        {
          _in.RDown = false;
          rightClick();
        }
      }
    }

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

    // Used as an event in SceneTransitioner
    public void DespawnRCBox()
    {
      CutObject = null;
      disableRCBox();
    }

    void OnDisable()
    {
      CursorManager.Instance.HoveringEntities.Remove(GetInstanceID());
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
      Debug.Log("Successfully cut: " + CutObject.name);

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

    private Vector3 getRCBoxPosition()
    {
      Vector2 closest = FindObjectOfType<RCAreaScript>().FindClosestValidPoint(_in.MouseWorld);
      return new Vector3(closest.x, closest.y, 0);
    }

    private void enableRCBox(Vector2 targetPosition)
    {
      _rc.transform.position = targetPosition;

      // Find and freeze currently selected object
      Collider2D collider = Physics2D.OverlapPoint((Vector2) _rc.transform.position, LayerManager.Instance.CuttableLayer);
      if (collider != null)
      {
        SelectedObject = collider.gameObject;
        SelectedObjectInitialConstraints = SelectedObject.GetComponent<Rigidbody2D>().constraints;
        SelectedObject.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
      }

      // Color RCBox
      _rcLeftRenderer.color  = (CutObject == null && SelectedObject != null) ? _goodColor : _baseColor;
      _rcRightRenderer.color = (CutObject != null && SelectedObject == null) ? _goodColor : _baseColor;
      
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

    #endregion
  }
}
