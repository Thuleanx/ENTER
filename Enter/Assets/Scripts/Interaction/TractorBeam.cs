using UnityEngine;
using Enter.Utils;

namespace Enter
{
  public class TractorBeam : MonoBehaviour
  {
    public Collider2D Collider { get; private set; }

    [SerializeField, Min(0)] private float _dampingConstant   = 0.1f; // might introduce oscillations if this is higher than 1
    [SerializeField, Min(0)] private float _timeToMaxVelocity = 0.2f;
    [SerializeField, Min(0)] private float _maxSpeed          = 10;

    void Awake()
    {
      Collider = GetComponent<Collider2D>();
    }

    void OnTriggerStay2D(Collider2D other)
    {
      Rigidbody2D rb = other.attachedRigidbody;

      Vector2 orthoDirection   = transform.up;
      Vector2 tangentDirection = transform.right;

      float orthoVelocity   = Vector2.Dot(rb.velocity, orthoDirection);
      float tangentVelocity = Vector2.Dot(rb.velocity, tangentDirection);

      // In orthogonal direction: damps (by an exponential decay function) the velocity to 0
      float targetOrthoVelocity = Math.Damp(orthoVelocity, 0, _dampingConstant * Mathf.Abs(orthoVelocity), Time.fixedDeltaTime);
      float orthoDeltaV = targetOrthoVelocity - orthoVelocity;
      // rb.AddForce(rb.mass * orthoDeltaV * orthoDirection, ForceMode2D.Impulse);

      // In force direction: brings speed to _maxSpeed
      if (!Mathf.Approximately(tangentVelocity, _maxSpeed))
      {
        float tangentDeltaV = _maxSpeed - tangentVelocity;
        if (!Mathf.Approximately(0, _timeToMaxVelocity))
        {
          tangentDeltaV = Mathf.Min(
            _maxSpeed * Time.fixedDeltaTime / _timeToMaxVelocity,
            _maxSpeed - tangentVelocity);
        }
        rb.velocity = _maxSpeed * tangentDirection;
        // rb.AddForce(rb.mass * tangentDeltaV * tangentDirection, ForceMode2D.Impulse);
	    }
    }
  }
}
