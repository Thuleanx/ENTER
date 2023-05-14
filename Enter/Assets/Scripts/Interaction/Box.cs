using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Enter
{
  public class Box : MonoBehaviour
  {
    [SerializeField, Tooltip("Game object containing conveyor box's sprite.")]
    private GameObject _conveyorBoxSpriteObj;
    
    [SerializeField, Tooltip("Game object containing physics box's sprite.")]
    private GameObject  _physicsBoxSpriteObj;

    [SerializeField, OnValueChanged("onIsPhysicsBoxChanged")]
    private bool _isPhysicsBox = false;

    public bool IsPhysicsBox
    {
      get { return _isPhysicsBox; }
      set
      {
        _isPhysicsBox = value;
        onIsPhysicsBoxChanged();
      }
    }

    private Material _conveyorBoxMat;
    private Material _physicsBoxMat;

    
    void Awake()
    {
      Assert.IsNotNull(_conveyorBoxSpriteObj, "Box must have a reference to the game object containing the conveyor box's sprite.");
      Assert.IsNotNull(_physicsBoxSpriteObj,  "Box must have a reference to the game object containing the physics box's sprite.");
    }

    private void onIsPhysicsBoxChanged()
    {
      if (_isPhysicsBox)
      {
        GetComponent<Rigidbody2D>().gravityScale = 1;
        GetComponent<ConveyorBox>().enabled = false;
        gameObject.layer = LayerMask.NameToLayer("PhysicsBox");
        _conveyorBoxSpriteObj.SetActive(false);
        _physicsBoxSpriteObj.SetActive(true);
      }
      else
      {
        GetComponent<Rigidbody2D>().gravityScale = 0;
        GetComponent<ConveyorBox>().enabled = true;
        gameObject.layer = LayerMask.NameToLayer("ConveyorBox");
        _conveyorBoxSpriteObj.SetActive(true);
        _physicsBoxSpriteObj.SetActive(false);
      }
    }
  }
}
