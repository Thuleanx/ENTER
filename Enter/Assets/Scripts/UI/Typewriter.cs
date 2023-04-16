using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using NaughtyAttributes;

namespace Enter
{
    [RequireComponent(typeof(RectTransform))]
    public class Typewriter : MonoBehaviour {
        RectTransform rectTransform;
        RectTransform parentRecTransform;

        TMP_Text textMesh;
        string _finishedText;

        public void Awake() {
            textMesh = GetComponentInChildren<TMP_Text>();
            rectTransform = GetComponent<RectTransform>();
            parentRecTransform = transform.parent?.GetComponent<RectTransform>();
            _finishedText = textMesh.text;
        }

        void OnEnable() {
            textMesh.text = "";
        }

        public IEnumerator WaitForTypeWrite(float _charactersPerMinute) {
            for (int i = 0; i < _finishedText.Length; i++) {
                textMesh.text = _finishedText.Substring(0, i) + "_";
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
                if (parentRecTransform) LayoutRebuilder.ForceRebuildLayoutImmediate(parentRecTransform);
                yield return new WaitForSecondsRealtime(60.0f /_charactersPerMinute);
            }
        }
    }
}
