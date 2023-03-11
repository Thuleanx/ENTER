using UnityEngine;
using Enter.Utils;

namespace Enter {
	public class TractorBeam : MonoBehaviour {
		public Collider2D Collider {get; private set; }
		[SerializeField, Min(0)] float _dampingConstant = 0.1f; // might introduce oscillations if this is higher than 1
		[SerializeField] float _tractorAcceleration = 5;

		void Awake() {
			Collider = GetComponent<Collider2D>();
		}

		void OnTriggerStay2D(Collider2D other) {
			// resist force orthogonal to 
			Rigidbody2D rigidBody = other.attachedRigidbody;

			float orthoVelocity = Vector2.Dot(rigidBody.velocity, transform.up);
			float currentDesiredVelocity = Math.Damp(orthoVelocity, 0, _dampingConstant, Time.fixedDeltaTime);

			rigidBody.AddForce((currentDesiredVelocity - orthoVelocity) * transform.up * rigidBody.mass, ForceMode2D.Impulse);

			rigidBody.AddForce(transform.right * _tractorAcceleration * rigidBody.mass * Time.fixedDeltaTime, ForceMode2D.Impulse);
		}
	}
}