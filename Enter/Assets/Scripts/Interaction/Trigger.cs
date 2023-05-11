using UnityEngine;
using UnityEngine.Events;

namespace Enter
{
  [RequireComponent(typeof(Collider2D))]
  public class Trigger : MonoBehaviour
  {
    [SerializeField] private UnityEvent _onTrigger;

    void OnTriggerEnter2D(Collider2D collider)
    {
      Debug.Log("Triggered");
      _onTrigger?.Invoke();
    }
  }
}