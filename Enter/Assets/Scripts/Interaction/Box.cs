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
    int _numClickableAreaIntersecting = 0;

    [SerializeField, ColorUsage(true, true)] Color clickableEmissionColor;
    
    void Awake()
    {
      Assert.IsNotNull(_conveyorBoxSpriteObj, "Box must have a reference to the game object containing the conveyor box's sprite.");
      Assert.IsNotNull(_physicsBoxSpriteObj,  "Box must have a reference to the game object containing the physics box's sprite.");

      _conveyorBoxMat = InitializeInstancedMaterial(_conveyorBoxSpriteObj);
      _physicsBoxMat = InitializeInstancedMaterial(_physicsBoxSpriteObj);
    }

    Material InitializeInstancedMaterial(GameObject obj) {
        SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
        if (!renderer) return null;
        return renderer.material = new Material(renderer.material);
    }

    void LateUpdate() {
        Color colorIfInteractible = Color.Lerp(Color.black, 
                clickableEmissionColor, (1+Mathf.Sin(Time.time))/2);
        Color emissionColor = _numClickableAreaIntersecting == 0 ? 
            Color.black : colorIfInteractible;
        _conveyorBoxMat.SetColor("_GlowColor", emissionColor);
        _physicsBoxMat.SetColor("_GlowColor", emissionColor);
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

    void OnTriggerEnter2D(Collider2D collider) {
        Debug.Log(collider.gameObject.layer);
        bool isRCArea = LayerManager.IsInLayerMask(LayerManager.Instance.RCAreaLayer, collider.gameObject);
        if (isRCArea) _numClickableAreaIntersecting++;
    }

    void OnTriggerExit2D(Collider2D collider) {
        bool isRCArea = LayerManager.IsInLayerMask(LayerManager.Instance.RCAreaLayer, collider.gameObject);
        if (isRCArea) _numClickableAreaIntersecting--;
    }
  }
}
