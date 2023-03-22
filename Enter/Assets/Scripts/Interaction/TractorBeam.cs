using UnityEngine;
using Enter.Utils;

namespace Enter
{
  public class TractorBeam : MonoBehaviour
  {
    public Collider2D Collider { get; private set; }

    public enum Direction
    {
      UP,
      RIGHT
    };

    [SerializeField, Min(0)] private float _dampingConstant = 0.1f; // might introduce oscillations if this is higher than 1
    [SerializeField, Min(0.1f)] private float _timeToMaxVelocity = 0.2f;
    [SerializeField] private float _maxSpeed = 30;
    [SerializeField] private Direction direction = Direction.RIGHT;

    void Awake()
    {
      Collider = GetComponent<Collider2D>();
    }

    void OnTriggerStay2D(Collider2D other)
    {
      // Resist force orthogonal to 
      Rigidbody2D rigidBody = other.attachedRigidbody;

      Vector2 orthoDirection = transform.up;
      Vector2 forceDirection = transform.right;

      if (direction == Direction.UP)
      {
        // Swap the two axis
        (orthoDirection, forceDirection) = (forceDirection, orthoDirection);
      }

      float orthoVelocity = Vector2.Dot(rigidBody.velocity, orthoDirection);
      // damps (by an exponential decay function) the velocity to 0 in the orthogonal direction. 
      float currentDesiredVelocity = Math.Damp(orthoVelocity, 0, _dampingConstant * Mathf.Abs(orthoVelocity), Time.fixedDeltaTime);

	//   if (Mathf.Abs(orthoVelocity) < 0.5) {
	// 	rigidBody.velocity -= orthoDirection * orthoVelocity;
	//   } else {
      	rigidBody.AddForce((currentDesiredVelocity - orthoVelocity) * orthoDirection * rigidBody.mass, ForceMode2D.Impulse);
	//   }

      float currentVelocityAlongTractor = Vector2.Dot(rigidBody.velocity, forceDirection);
      if (currentVelocityAlongTractor < _maxSpeed)
        rigidBody.AddForce(forceDirection * (_maxSpeed / _timeToMaxVelocity) * rigidBody.mass * Time.fixedDeltaTime, ForceMode2D.Impulse);
    }
  }
}