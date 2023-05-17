using UnityEngine;
using UnityEngine.Events;

namespace Enter
{
  [RequireComponent(typeof(Collider2D))]
  public class Trigger : MonoBehaviour
  {
    [SerializeField] private UnityEvent _onTrigger;
    [SerializeField] private bool _onlyOnce = true;

    void OnTriggerEnter2D(Collider2D collider)
    {
      _onTrigger?.Invoke();
      if (_onlyOnce) gameObject.SetActive(false);
    }
  }
}