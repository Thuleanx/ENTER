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
    [SerializeField] private float _rcBoxFadeoutTime = 0;

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
      // RCBox disappears on left click
      if (_in.LDown && _rc.activeSelf)
      {
        _in.LDown = false;
        StartCoroutine("disappearRC");
      }

      // RCBox appears on right click
      if (_in.RDown && Time.time - _minRCInterval > _lastRCTime)
      {
        _in.RDown = false;
        StartCoroutine("appearRC");
      }
    }

    #endregion

    #region ================== Helpers

    private IEnumerator disappearRC()
    {
      // Make the RCBox disappear
      yield return fadeout();
    }

    private IEnumerator appearRC()
    {
      // If the RCBox is already present, make it disappear
      if (_rc.activeSelf) yield return fadeout();

      // Spawn in at this location
      _rc.transform.position = getRCBoxPosition();
      _rc.SetActive(true);
    }

    private IEnumerator fadeout()
    {
      // Optional:
      // we can use this to implement a delay to the 
      // RCBox disappearing, or for other effects
      
      yield return new WaitForSeconds(_rcBoxFadeoutTime);
      _rc.SetActive(false);
    }

    private Vector3 getRCBoxPosition()
    {
      // Todo:
      // account for RCBox's own size via an offset,
      // either here or in its parent-child transforms

      return Camera.main.ScreenToWorldPoint(new Vector3(
        _in.Mouse.x,
        _in.Mouse.y,
        Camera.main.nearClipPlane));
    }

    #endregion
  }
}