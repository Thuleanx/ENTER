using UnityEngine;
using UnityEngine.Events;

namespace Enter
{
  public class Interactable : MonoBehaviour
  {
    [SerializeField] protected UnityEvent onInteractFunctions;

    protected virtual void OnInteract()
    { 
      onInteractFunctions?.Invoke();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
      if (other.tag == "Player") OnInteract();
    }
  }
}