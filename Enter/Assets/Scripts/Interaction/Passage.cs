using UnityEngine;
using Enter.Utils;

namespace Enter {
	public class Passage : Interactable {
		[SerializeField] SceneReference targetScene;

		protected override void OnInteract() { 
			base.OnInteract();
			SceneTransitioner.Instance.TransitionTo(targetScene);
			gameObject.SetActive(false);
		}
	}
}