using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Enter
{
    public class ConveyorBox : MonoBehaviour
    {
        [SerializeField]
        private bool        _isBlocked;
        private Rigidbody2D _blockingRigidbody;
        [SerializeField]
        private bool        _blockingIsConveyorBox;

        private ConveyorBeam _currentConveyorBeam; // Pretend this never changes after being set
        
        private Rigidbody2D  _rb;
        
        [SerializeField] private LayerMask _conveyorLayer;
        [SerializeField] private LayerMask _conveyorBoxLayer;

        #region ================== Methods

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            Assert.IsNotNull(_rb, "ConveyorBox must have a reference to its own Rigidbody2D.");
        }

        void FixedUpdate()
        {
            updateBlockedness();
            if (_isBlocked)
            {
                _rb.velocity = _blockingIsConveyorBox ? _blockingRigidbody.velocity : Vector2.zero;
            }
            else
            {
                _rb.velocity = _currentConveyorBeam ? _currentConveyorBeam.ConveyorBeamVelocity : Vector2.zero;
            }
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            Debug.Log("OnCollisionEnter2D");
            if (!colliderIsOnBlockingSide(collision)) return;

            // Set stuff
            _isBlocked = true;
            _blockingRigidbody = collision.collider.gameObject.GetComponent<Rigidbody2D>();
            _blockingIsConveyorBox = collision.collider.gameObject.layer == _conveyorBoxLayer;

            Assert.IsNotNull(_blockingRigidbody, "ConveyorBox must have a reference to its blocker's Rigidbody2D.");

            // Maybe set position to be perfectly in line with blocking object
            Debug.Log("Yo");
            // _rb.position = _blockingRigidbody.position + new Vector2(1, 0);
        }

        void OnTriggerEnter2D(Collider2D otherCollider)
        {
            Debug.Log("OnTriggerEnter2D");
            if (!colliderIsConveyorBeamsCollider(otherCollider)) return;

            _currentConveyorBeam = otherCollider.GetComponent<ConveyorBeam>();
            _rb.gravityScale = 0;

            Assert.IsNotNull(_currentConveyorBeam, "ConveyorBox must have a reference to its current ConveyorBeam.");
        }

        void OnTriggerStay2D(Collider2D otherCollider)
        {
            // Set position to be in line with beam by using projection
            Vector2 offsetFromBeamObject  = _rb.position - (Vector2) _currentConveyorBeam.transform.position;
            Vector2 beamObjectUpDirection = _currentConveyorBeam.transform.up;
            _rb.position = _rb.position - Vector2.Dot(offsetFromBeamObject, beamObjectUpDirection) * (Vector2) beamObjectUpDirection;
        }

        void OnTriggerExit2D(Collider2D otherCollider)
        {
            gameObject.SetActive(false);
        }
        
        #endregion
        
        #region ================== Helpers

        private bool colliderIsConveyorBeamsCollider(Collider2D otherCollider)
        {
            // Todo
            return true;
        }

        private void updateBlockedness()
        {
            // todo
            // set these three things:
            // _isBlocked
            // _blockingObject
            // _blockingIsConveyorBox
        }

        private bool colliderIsOnBlockingSide(Collision2D collision)
        {
            // Fixme: can be other directions
            Debug.Log(collision);
            Debug.Log(collision.collider);
            Debug.Log(collision.collider.transform);
            Vector3 blockerPosition = collision.collider.transform.position;
            Debug.Log("AHAHA");
            Debug.Log(blockerPosition);
            Debug.Log(_rb.position);
            bool isToTheLeft = blockerPosition.x < _rb.position.x;
            return isToTheLeft;
        }

        #endregion
    }
}