using System.Collections;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;
using UnityEngine;

[DisallowMultipleComponent]
public class RCBoxManager : MonoBehaviour
{
  public static RCBoxManager Instance;

  private InputData  _in;

  [SerializeField] private GameObject _rc;

  private float _lastRCTime = -Mathf.Infinity;

  [SerializeField] private float _minRCInterval = 1;
  [SerializeField] private float _rcBoxFadeoutTime = 0;

  // ================== Methods

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
    if (_in.LDown && _rc.activeSelf)
    {
      _in.LDown = false;
      StartCoroutine("disappearRC");
    }

    if (_in.RDown && Time.time - _minRCInterval > _lastRCTime)
    {
      _in.RDown = false;
      StartCoroutine("appearRC");
    }
  }

  // ================== Helpers

  private IEnumerator disappearRC()
  {
    yield return fadeout();

    _rc.SetActive(false);
  }

  private IEnumerator fadeout()
  {
    // Todo: actually fade it out
    
    yield return new WaitForSeconds(_rcBoxFadeoutTime);
  }

  private IEnumerator appearRC()
  {
    if (_rc.activeSelf) yield return fadeout();

    _rc.transform.position = getRCBoxPosition();
    _rc.SetActive(true);
  }

  private Vector3 getRCBoxPosition()
  {
    // Todo: account for RCBox's own size via an offset

    return Camera.main.ScreenToWorldPoint(new Vector3(
      _in.Mouse.x,
      _in.Mouse.y,
      Camera.main.nearClipPlane));
  }
}