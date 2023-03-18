using Cinemachine;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using NaughtyAttributes;

namespace Enter {
	[ExecuteAlways]
	[RequireComponent(typeof(CinemachineVirtualCamera))]
	public class PixelPerfectCameraAdjuster : MonoBehaviour {
		[SerializeField, ReadOnly] CinemachineVirtualCamera virtualCamera;
		[SerializeField, ReadOnly] float aspectRatioWidth = 16;
		[SerializeField, ReadOnly] float aspectRatioHeight = 9;

		void Awake() {
			virtualCamera = GetComponent<CinemachineVirtualCamera>();
		}

		void Update() {
			UnityEngine.Experimental.Rendering.Universal.PixelPerfectCamera ppuCamera = Camera.main.GetComponent<UnityEngine.Experimental.Rendering.Universal.PixelPerfectCamera>();

			float height = virtualCamera.m_Lens.OrthographicSize * ppuCamera.assetsPPU *2;
			float width = height * aspectRatioWidth / aspectRatioHeight + 0.5f;

			ppuCamera.refResolutionX = (int) width;
			ppuCamera.refResolutionY = (int) height;
		}
	}
}