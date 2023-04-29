using UnityEngine;
using NaughtyAttributes;

namespace Enter {
    [RequireComponent(typeof(Camera))]
    public class MaskRenderTextureManager : MonoBehaviour {
        Camera mainCamera => Camera.main;
        [SerializeField] RenderTexture defaultTexture;
        [SerializeField] Material renderMaterial;
        [field:SerializeField, Required] public Camera maskCamera {get; private set; }

        Vector2Int cameraPixelDimension;

        void Awake() {
            maskCamera = GetComponent<Camera>();
            cameraPixelDimension = new Vector2Int(mainCamera.pixelWidth, mainCamera.pixelHeight);
            ObtainTextureToFitCamera();
        }

        void ObtainTextureToFitCamera() {
            // this should not run too often
            /* defaultTexture.Release(); */
            /* defaultTexture.width = mainCamera.pixelWidth; */
            /* defaultTexture.height = mainCamera.pixelHeight; */
            /* defaultTexture.Create(); */
            /* renderMaterial.SetTexture("_CorruptedArea", defaultTexture); */
        }

        void LateUpdate() {
            // make sure the mask camera has the same ortho size as the main camera
            maskCamera.orthographicSize = mainCamera.orthographicSize;
            // test if camera pixel data has changed
            Vector2Int currentPixelDimension = new Vector2Int(mainCamera.pixelWidth, mainCamera.pixelHeight);
            if (currentPixelDimension != cameraPixelDimension) {
                cameraPixelDimension = currentPixelDimension;
                ObtainTextureToFitCamera();
            }
        }
    }
}
