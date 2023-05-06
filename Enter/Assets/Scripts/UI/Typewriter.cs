using System;
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

        public IEnumerator WaitForTypeWrite(float _linesPerMinute) {
            float startTime = Time.unscaledTime;

            string[] allLines = _finishedText.Split(new[]{ Environment.NewLine }, StringSplitOptions.None);

            string currentDisplay = "";
            int lineToDisplay = 0;
            int currentlyDisplayedLine = 0;
            while (lineToDisplay < allLines.Length) {
                if (currentlyDisplayedLine < ((int) lineToDisplay)) {
                    // only update when neccessary
                    while (currentlyDisplayedLine < lineToDisplay) {

                        if (currentlyDisplayedLine > 0) currentDisplay += "\n";
                        currentDisplay += allLines[currentlyDisplayedLine];

                        currentlyDisplayedLine++;
                    }
                    textMesh.text = currentDisplay + "_";

                    LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
                    if (parentRecTransform) LayoutRebuilder.ForceRebuildLayoutImmediate(parentRecTransform);
                }
                yield return null;
                float elapsedTime = Time.unscaledTime - startTime;
                lineToDisplay = (int) (elapsedTime * _linesPerMinute / 60.0f);
            }

            textMesh.text = _finishedText;
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            if (parentRecTransform) LayoutRebuilder.ForceRebuildLayoutImmediate(parentRecTransform);
        }
    }
}
