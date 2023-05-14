using UnityEngine;
using System.Collections.Generic;

namespace Enter {
    public class BoxGlowOnTrigger : MonoBehaviour {
        [System.Serializable]
        struct GlowInfo {
            public SpriteRenderer renderer;
            [ColorUsage(true,true)] public Color color;
        };
        [SerializeField] List<GlowInfo> glowInfo;
        int _numTriggered = 0;

        void Awake() {
            foreach (var info in glowInfo)
                info.renderer.material = new Material(info.renderer.material);
        }

        Material InitializeInstancedMaterial(GameObject obj) {
            SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
            if (!renderer) return null;
            return renderer.material = new Material(renderer.material);
        }

        void LateUpdate() {
            foreach (var info in glowInfo) {
                Color colorIfInteractible = Color.Lerp(info.color / 10, 
                        info.color, (1+Mathf.Sin(Time.time*3))/2);
                Color emissionColor = _numTriggered == 0 ? 
                    Color.black : colorIfInteractible;
                info.renderer.material.SetColor("_GlowColor", emissionColor);
            }
        }

        void OnTriggerEnter2D(Collider2D collider) {
            bool isRCArea = LayerManager.IsInLayerMask(LayerManager.Instance.RCAreaLayer, collider.gameObject);
            if (isRCArea) _numTriggered++;
        }

        void OnTriggerExit2D(Collider2D collider) {
            bool isRCArea = LayerManager.IsInLayerMask(LayerManager.Instance.RCAreaLayer, collider.gameObject);
            if (isRCArea) _numTriggered--;
        }
    }
}
