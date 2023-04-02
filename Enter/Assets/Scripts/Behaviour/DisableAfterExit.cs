using UnityEngine;

namespace Enter
{
	public class DisableAfterExit : MonoBehaviour
  {
		private void OnTriggerExit2D(Collider2D other)
    {
			gameObject.SetActive(false);
		}
    
		private void OnCollisionExit2D(Collision2D other) => gameObject.SetActive(false);
	}
}