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

    [SerializeField, Tooltip("VERY TODO DO NOT LOOK TOO HARD HERE.")]
    private GameObject _boxPrefab;

    private InputData _in;

    [SerializeField] private GameObject _rc;

    [SerializeField] private LayerMask _rcAreaLayer;
    [SerializeField] private LayerMask _rcBoxLayer;
    [SerializeField] private LayerMask _physicsBoxLayer;

    [SerializeField] private float _lastRCTime = -Mathf.Infinity;
    [SerializeField] private float _minRCInterval = 1;
    [SerializeField] private float _rcBoxFadeoutTime = 0;

    private bool hasCut = false;

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
        // VERY TODO DOES NOT REPRESENT ACTUAL LOGIC:
        // Check if box should be spawned
        Vector2 i = _in.MouseWorld;
        bool yes = Physics2D.OverlapPoint((Vector2) i, _rcBoxLayer);
        if (yes)  
        {
          _in.LDown = false;
          BubbleManager.Instance.Borrow(
            gameObject.scene,
            _boxPrefab,
            _rc.transform.position,
            Quaternion.identity);
           // bool y = Physics2D.OverlapPoint((Vector2) _in.MouseWorld, _physicsBoxLayer);
            //if(y) {
            //  StartCoroutine(cut());
           // }
        }
        // END VERY TODO
        _in.LDown = false;
        StartCoroutine(leftClick(i));
      }

      // RCBox appears on right click
      if (_in.RDown && Time.time - _minRCInterval > _lastRCTime)
      {
        // Check that RCBox is appearing in valid area
        bool yes = Physics2D.OverlapPoint((Vector2) _in.MouseWorld, _rcAreaLayer);
        if (yes)
        {
          _in.RDown = false;
          StartCoroutine(rightClick());
        }
      }
    }

    public void DespawnRCBox()
    {
      StopAllCoroutines();
      StartCoroutine(fadeout());
    }

    #endregion

    #region ================== Helpers

    private IEnumerator leftClick(Vector2 i)
    {
      // Make the RCBox disappear
      disambiguate(i);
      yield return fadeout();
    }

    private IEnumerator rightClick()
    {
      // If the RCBox is already present, make it disappear
      if (_rc.activeSelf) yield return fadeout();

      // Spawn in at this location
      _rc.transform.position = getRCBoxPosition();
      _rc.SetActive(true);
    }

    //let me know if i should iEnumerate these for seconds
    private void disambiguate(Vector2 i){
      if(transform.position.x > i.x){
        cut();
      }
      else{
        paste();
      }
    }

    private void cut()
    {
      //cut
      hasCut = true;
      Debug.Log("Cut");
    }

    private void paste()
    {
      //paste
      hasCut = false;
      Debug.Log("Paste");
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
      Vector2 closest = FindObjectOfType<RCAreaScript>().FindClosestValidPoint(_in.MouseWorld);
      return new Vector3(closest.x, closest.y, 0);
    }

    #endregion
  }
}