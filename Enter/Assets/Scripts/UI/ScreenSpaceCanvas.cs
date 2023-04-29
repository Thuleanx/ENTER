using UnityEngine;
using UnityEngine.UI;

namespace Enter {
    [RequireComponent(typeof(Canvas))]
    public class ScreenSpaceCanvas : MonoBehaviour {
        Canvas canvas;

        void Awake() {
            canvas = GetComponent<Canvas>();
        }

        void LateUpdate() {
            canvas.worldCamera = Camera.main;
        }
    }
}
