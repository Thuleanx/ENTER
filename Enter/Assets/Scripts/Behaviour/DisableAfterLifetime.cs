using UnityEngine;

namespace Enter {
	public class DisableAfterLifetime : MonoBehaviour {
		[SerializeField, Range(0,30)] float lifetimeSeconds;
        float startTime;

		private void OnEnable() {
            startTime = Time.time;
		}

		private void Update() {
            if (startTime + lifetimeSeconds < Time.time) 
                gameObject.SetActive(false);
		}
	}
}
