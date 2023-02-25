using UnityEngine;
using UnityEngine.Events;

namespace Enter {
	public class Interactable : MonoBehaviour {
		[SerializeField] protected UnityEvent onInteract;

		protected virtual void OnInteract() { onInteract?.Invoke(); }
		
		void OnTriggerEnter2D(Collider2D other) { 
			Debug.Log(other);
			if (other.tag == "Player") OnInteract();
		}
	}
}