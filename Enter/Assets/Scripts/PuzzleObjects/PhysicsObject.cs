using UnityEngine;

namespace Enter
{
  public class PhysicsObject : MonoBehaviour
  {
    public Rigidbody2D Body { get; private set; }

    [SerializeField] LayerMask _tractorBeamLayer;

    float _initialGravity;
    int _numTractorsColliding = 0;

    void Awake()
    {
      Body = GetComponent<Rigidbody2D>();
      _initialGravity = Body.gravityScale;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
      if ((_tractorBeamLayer & (1 << other.gameObject.layer)) != 0)
        if (_numTractorsColliding++ == 0)
          Body.gravityScale = 0;
    }

    void OnTriggerExit2D(Collider2D other)
    {
      if ((_tractorBeamLayer & (1 << other.gameObject.layer)) != 0)
        if (--_numTractorsColliding == 0)
          Body.gravityScale = _initialGravity;
    }
  }
}