using UnityEngine;
using Enter.Utils;

namespace Enter {
	public class TractorBeam : MonoBehaviour {
		public Collider2D Collider {get; private set; }

		public enum Direction {
			UP,
			RIGHT
		};

		[SerializeField, Min(0)] float _dampingConstant = 0.1f; // might introduce oscillations if this is higher than 1
		[SerializeField] float _tractorAcceleration = 5;
		[SerializeField] float _tractorMaxSpeed = 30;
		[SerializeField] Direction direction = Direction.RIGHT;

		void Awake() {
			Collider = GetComponent<Collider2D>();
		}

		void OnTriggerStay2D(Collider2D other) {
			// resist force orthogonal to 
			Rigidbody2D rigidBody = other.attachedRigidbody;

			Vector2 orthoDirection = transform.up;
			Vector2 forceDirection = transform.right;
			if (direction == Direction.UP) 
				(orthoDirection, forceDirection) = (forceDirection, orthoDirection); // swap the two axis

			float orthoVelocity = Vector2.Dot(rigidBody.velocity, orthoDirection);
			// damps (by an exponential decay function) the velocity to 0 in the orthogonal direction. 
			float currentDesiredVelocity = Math.Damp(orthoVelocity, 0, _dampingConstant, Time.fixedDeltaTime);

			rigidBody.AddForce((currentDesiredVelocity - orthoVelocity) * orthoDirection * rigidBody.mass, ForceMode2D.Impulse);

			float currentVelocityAlongTractor = Vector2.Dot(rigidBody.velocity, forceDirection);
			if (currentVelocityAlongTractor < _tractorMaxSpeed)
				rigidBody.AddForce(forceDirection * _tractorAcceleration * rigidBody.mass * Time.fixedDeltaTime, ForceMode2D.Impulse);
		}
	}
}