using UnityEngine;
using System.Collections.Generic;
using NaughtyAttributes;

namespace Enter {
    public class CursorManager : MonoBehaviour {
        public static CursorManager Instance;

        enum CursorType {
            Pointer,
            Hover
        }

        enum Mode {
            Light,
            Dark
        }

        [System.Serializable]
        public struct CursorParams {
            public Texture2D texture;
            public Vector2 hotspot;
        }

        [SerializeField] CursorParams _pointer;
        [SerializeField] CursorParams _pointerDark;
        [SerializeField] CursorParams _hover;
        [SerializeField] CursorParams _hoverDark;
        [ReadOnly, SerializeField] CursorType currentType = CursorType.Pointer;
        Mode mode = Mode.Light;

        public HashSet<int> HoveringEntities = new HashSet<int>();

        void Awake() {
            Instance = this;
            SetCursor(currentType);
            /* Cursor.lockState = CursorLockMode.Confined; */
        }

        void SetCursor(CursorType type) {
            var argument = (mode, type);
            CursorParams param = argument switch {
                (Mode.Light, CursorType.Pointer) => _pointer,
                (Mode.Light, CursorType.Hover) => _hover,
                (Mode.Dark, CursorType.Pointer) => _pointerDark,
                (Mode.Dark, CursorType.Hover) => _hoverDark,
                _ => _pointer
            };
            Debug.Log(param.texture.width + " " + param.texture.height);
            Cursor.SetCursor(param.texture, param.hotspot, CursorMode.Auto);
        }

        void LateUpdate() {
            CursorType desiredCursor = HoveringEntities.Count == 0 ? CursorType.Pointer : CursorType.Hover;

            if (desiredCursor != currentType) {
                SetCursor(desiredCursor);
                currentType = desiredCursor;
            }
        }
    }
}
