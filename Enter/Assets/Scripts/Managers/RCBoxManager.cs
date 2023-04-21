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

    private InputData _in;

    [SerializeField] private GameObject _rc;

    [SerializeField] private float _lastRCTime = -Mathf.Infinity;
    [SerializeField] private float _minRCInterval = 1;

    public GameObject SelectedObject = null;
    private RigidbodyConstraints2D SelectedObjectInitialConstraints;

    public GameObject CutObject = null;
    private RigidbodyConstraints2D CutObjectInitialConstraints;

    #region ================== Methods

    void Awake()
    {
      Instance = this;
      Assert.IsNotNull(_rc, "RCBoxManager must have a reference to GameObject RCBox.");
    }

    void Start()
    {
      _in = InputManager.Instance.Data;
    }

    void FixedUpdate()
    {
      if (_in.LDown)
      {
        bool shouldCountLeftClick =
          _rc.activeSelf &&
          Physics2D.OverlapPoint((Vector2) _in.MouseWorld, LayerManager.Instance.RCBoxLayer);

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

        if (shouldCountRightClick)
        {
          _in.RDown = false;
          rightClick();
        }
      }
    }

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

      _rc.SetActive(false);
    }

    private void paste()
    {
      // Prevent pasting if nothing was cut
      if (CutObject == null)
      {
        Debug.Log("Nothing to paste.");
        return;
      }

      Debug.Log("Successfully pasted: " + CutObject.name);
      CutObject.transform.SetPositionAndRotation(_rc.transform.position, Quaternion.identity);
      CutObject.GetComponent<Rigidbody2D>().constraints = CutObjectInitialConstraints;
      CutObject.SetActive(true);
      CutObject = null;

      _rc.SetActive(false);
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

      _rc.SetActive(true);
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