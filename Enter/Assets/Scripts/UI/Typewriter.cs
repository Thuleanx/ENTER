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

        public int TextLength => _finishedText.Length;

        public void Awake() {
            textMesh = GetComponentInChildren<TMP_Text>();
            rectTransform = GetComponent<RectTransform>();
            parentRecTransform = transform.parent?.GetComponent<RectTransform>();
            _finishedText = textMesh.text;
        }

        void OnEnable() {
            textMesh.text = "";
        }

        public void DisplayCharacters(int numberOfCharacters) {
            if (numberOfCharacters >= TextLength)
                textMesh.text = _finishedText;
            else textMesh.text = _finishedText.Substring(0, numberOfCharacters) + "_";

            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            if (parentRecTransform) LayoutRebuilder.ForceRebuildLayoutImmediate(parentRecTransform);
        }

        public IEnumerator WaitForTypeWrite(float _charactersPerMinute) {
            float startTime = Time.unscaledTime;

            int charactersToDisplay = 0;
            int i = -1;
            do {
                if (i != ((int) charactersToDisplay)) {
                    // only update when neccessary
                    i = (int) charactersToDisplay;
                    DisplayCharacters(i);
                }
                yield return null;
                float elapsedTime = Time.unscaledTime - startTime;
                charactersToDisplay = (int) (elapsedTime * _charactersPerMinute / 60.0f);
            } while (charactersToDisplay < _finishedText.Length);
            DisplayCharacters(TextLength);
        }
    }
}
