using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using Cinemachine;

namespace Enter {
	public class PixelCameraAdjuster : MonoBehaviour {
		CinemachineVirtualCamera virtualCamera;

		void Awake() {
			virtualCamera = GetComponent<CinemachineVirtualCamera>();
		}

		void LateUpdate() {
			UnityEngine.Experimental.Rendering.Universal.PixelPerfectCamera ppCamera = Camera.main.GetComponent<UnityEngine.Experimental.Rendering.Universal.PixelPerfectCamera>();
			float height = virtualCamera.m_Lens.OrthographicSize * ppCamera.assetsPPU * 2;
			float width = height * 16/9;

			ppCamera.refResolutionX = (int) Mathf.Floor(width / 16) * 16;
			ppCamera.refResolutionY = (int) Mathf.Floor(height / 9) * 9;
		}
	}
}