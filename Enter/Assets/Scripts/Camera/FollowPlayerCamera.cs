using UnityEngine;
using Cinemachine;

namespace Enter {
	[RequireComponent(typeof(CinemachineVirtualCamera))]
	public class FollowPlayerCamera : MonoBehaviour {
		CinemachineVirtualCamera cam;
		void Awake() {
			cam = GetComponent<CinemachineVirtualCamera>();
		}
		void Update() {
			// this should only run once per scene
			if (!cam.Follow) cam.Follow = PlayerScript.Instance.transform;
		}
	}
}