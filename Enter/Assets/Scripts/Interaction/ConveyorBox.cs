using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Enter
{
  public class ConveyorBox : MonoBehaviour
  {
    private Rigidbody2D  _rb;
    
    public ConveyorBeam CurrentConveyorBeam; // In all our use cases, this never changes after being set
    public ConveyorBox  DownstreamConveyorBox;
    public ConveyorBox  UpstreamConveyorBox;
    public GameObject   DownstreamRCBoxObject;
    public Vector3      CollidedRCBoxPosition;

    public Rigidbody2D Rigidbody2D => _rb;

    public bool IsBlockedByCB    => DownstreamConveyorBox && DownstreamConveyorBox.gameObject.activeInHierarchy;
    public bool IsBlockedByRCBox => DownstreamRCBoxObject && DownstreamRCBoxObject.activeInHierarchy && DownstreamRCBoxObject.transform.position == CollidedRCBoxPosition;

    #region ================== Methods

    void Awake()
    {
      _rb = GetComponent<Rigidbody2D>();
      Assert.IsNotNull(_rb, "ConveyorBox must have a reference to its own Rigidbody2D.");
      _rb.gravityScale = 0;
    }

    void OnEnable()
    {
      CurrentConveyorBeam   = null;
      DownstreamConveyorBox = null;
      UpstreamConveyorBox   = null;
      DownstreamRCBoxObject = null;
      CollidedRCBoxPosition = Vector3.positiveInfinity;
    }

    void FixedUpdate()
    {
      _rb.velocity = CurrentConveyorBeam ? CurrentConveyorBeam.ConveyorBeamVelocity : Vector2.zero;

      if (IsBlockedByCB)
      {
        SetVelocityAndPositionInChain(
          DownstreamConveyorBox.Rigidbody2D.velocity,
          DownstreamConveyorBox.Rigidbody2D.position + new Vector2(2, 0));
        
      } else {
        if (DownstreamConveyorBox)
        {
          DownstreamConveyorBox.UpstreamConveyorBox = null;
          DownstreamConveyorBox = null;
        }
      }

      if (IsBlockedByRCBox)
      {
        _rb.velocity = Vector2.zero;
      } else {
        DownstreamRCBoxObject = null;
        CollidedRCBoxPosition = Vector3.positiveInfinity;
      }
      
      _rb.constraints = (_rb.velocity == Vector2.zero) ?
        RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePosition :
        RigidbodyConstraints2D.FreezeRotation;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
      Collider2D collider = collision.collider;

      // Collision with RCBox
      if (colliderIsRCBox(collider))
      {
        // Downstream
        if (colliderIsUpOrDownstream(collider, true)) 
        {
          if (DownstreamConveyorBox)
          {
            DownstreamConveyorBox.UpstreamConveyorBox = null; // Must overwrite to break chain
            DownstreamConveyorBox = null;                     // Must overwrite to break chain
          }
          DownstreamRCBoxObject = collider.gameObject;
          CollidedRCBoxPosition = collider.transform.position;
        }
        return;
      }

      // Collision with conveyor box
      if (colliderIsConveyorBox(collider))
      {
        // Downstream
        if (colliderIsUpOrDownstream(collision.collider, true))
        {
          DownstreamConveyorBox = collider.gameObject.GetComponent<ConveyorBox>();
          DownstreamConveyorBox.UpstreamConveyorBox = this;
        }
        return;
      }
    }

    void OnTriggerEnter2D(Collider2D otherCollider)
    {
      if (colliderIsConveyorBeam(otherCollider))
      {
        CurrentConveyorBeam = otherCollider.GetComponent<ConveyorBeam>();
        Assert.IsNotNull(CurrentConveyorBeam, "ConveyorBox must have a reference to its current ConveyorBeam.");
      }
    }

    void OnTriggerStay2D(Collider2D otherCollider)
    {
      // if (colliderIsConveyorBeam(otherCollider))
      // {
      //   // Set position to be in line with beam by using projection
      //   Vector2 offsetFromBeamObject  = _rb.position - (Vector2) CurrentConveyorBeam.transform.position;
      //   Vector2 beamObjectUpDirection = CurrentConveyorBeam.transform.up;
      //   _rb.position = _rb.position - Vector2.Dot(offsetFromBeamObject, beamObjectUpDirection) * (Vector2) beamObjectUpDirection;
      // }
    }

    void OnTriggerExit2D(Collider2D otherCollider)
    {
      if (colliderIsConveyorBeam(otherCollider))
      {
        gameObject.SetActive(false);
        if (UpstreamConveyorBox != null)
        {
          UpstreamConveyorBox.DownstreamConveyorBox = null;
          UpstreamConveyorBox = null;
        }
      }
    }
  
    public void SetVelocityAndPositionInChain(Vector2 vel, Vector2 pos)
    {
      _rb.velocity = vel;
      _rb.position = pos;
      if (UpstreamConveyorBox) UpstreamConveyorBox.SetVelocityAndPositionInChain(vel, pos + new Vector2(2, 0));
    }

    #endregion
    
    #region ================== Helpers

    private bool colliderIsRCBox(Collider2D collider)
    {
      return LayerManager.Instance.IsInLayerMask(LayerManager.Instance.RCBoxForOthersLayer, collider.gameObject);
    }
    
    private bool colliderIsConveyorBeam(Collider2D collider)
    {
      return LayerManager.Instance.IsInLayerMask(LayerManager.Instance.ConveyorBeamLayer, collider.gameObject);
    }

    private bool colliderIsConveyorBox(Collider2D collider)
    {
      return LayerManager.Instance.IsInLayerMask(LayerManager.Instance.ConveyorBoxLayer, collider.gameObject);
    }
    
    private bool colliderIsUpOrDownstream(Collider2D collider, bool downstream)
    {
      // Check downstream/upstream
      Vector2 blockerPosition = collider.transform.position;
      Vector2 thisToBlocker = (blockerPosition - _rb.position).normalized;
      Vector2 travelDirection = CurrentConveyorBeam.ConveyorBeamVelocity.normalized;

      if (downstream) return Vector2.Dot(thisToBlocker, travelDirection) > +0.5;
      else            return Vector2.Dot(thisToBlocker, travelDirection) < -0.5;
    }

    #endregion
  }
}