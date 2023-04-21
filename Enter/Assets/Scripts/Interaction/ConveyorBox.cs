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

    public Vector2 NextInChainOffset => -2 * CurrentConveyorBeam.ConveyorBeamVelocity.normalized;
    
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
          DownstreamConveyorBox.Rigidbody2D.position + NextInChainOffset);
        
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
        RigidbodyConstraints2D.FreezeAll :
        RigidbodyConstraints2D.FreezeRotation;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
      Collider2D collider = collision.collider;

      // Ignore if collision is upstream
      if (!colliderIsDownstream(collider)) return;

      // Collision with RCBox which is downstream
      if (colliderIsRCBox(collider))
      {
        if (DownstreamConveyorBox)
        {
          DownstreamConveyorBox.UpstreamConveyorBox = null; // Must overwrite to break chain
          DownstreamConveyorBox = null;                     // Must overwrite to break chain
        }
        DownstreamRCBoxObject = collider.gameObject;
        CollidedRCBoxPosition = collider.transform.position;
      }

      // Collision with conveyor box which is downstream
      if (colliderIsConveyorBox(collider))
      {
        DownstreamConveyorBox = collider.gameObject.GetComponent<ConveyorBox>();
        DownstreamConveyorBox.UpstreamConveyorBox = this;
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
      if (colliderIsConveyorBeam(otherCollider))
      {
        // Set position to be in line with beam by using projection
        Vector2 offsetFromBeamObject  = _rb.position - (Vector2) CurrentConveyorBeam.transform.position;
        Vector2 beamObjectUpDirection = CurrentConveyorBeam.transform.up;
        _rb.position = _rb.position - Vector2.Dot(offsetFromBeamObject, beamObjectUpDirection) * (Vector2) beamObjectUpDirection;
      }
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
      if (UpstreamConveyorBox) UpstreamConveyorBox.SetVelocityAndPositionInChain(vel, pos + NextInChainOffset);
    }

    #endregion
    
    #region ================== Helpers

    private bool colliderIsRCBox(Collider2D collider)
    {
      return LayerManager.Instance.IsInLayerMask(LayerManager.Instance.RCBoxLayer, collider.gameObject);
    }
    
    private bool colliderIsConveyorBeam(Collider2D collider)
    {
      return LayerManager.Instance.IsInLayerMask(LayerManager.Instance.ConveyorBeamLayer, collider.gameObject);
    }

    private bool colliderIsConveyorBox(Collider2D collider)
    {
      return LayerManager.Instance.IsInLayerMask(LayerManager.Instance.ConveyorBoxLayer, collider.gameObject);
    }
    
    private bool colliderIsDownstream(Collider2D collider)
    {
      Vector2 blockerPosition = collider.transform.position;
      Vector2 thisToBlocker   = (blockerPosition - _rb.position).normalized;
      Vector2 travelDirection = CurrentConveyorBeam.ConveyorBeamVelocity.normalized;
      return Vector2.Dot(thisToBlocker, travelDirection) > 0.7071067812;
    }

    #endregion
  }
}