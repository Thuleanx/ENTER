using UnityEngine; 
using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using DG.Tweening;
using Enter.Utils;

namespace Enter {
    public class BeginningSequence : MonoBehaviour {
        [SerializeField] float _charactersPerMinute = 80;
        [SerializeField, Range(0, 5)] float _scrollDelay = 2;
        [SerializeField] CinemachineVirtualCamera _fallingCamera;
        [SerializeField] Transform _fallBottomText;
        List<Typewriter> _sequenceTexts;

        void Awake() {
            _sequenceTexts = new List<Typewriter>(GetComponentsInChildren<Typewriter>());
        }

        void Start() {
            StartCoroutine(_RunSequence());
        }

        IEnumerator _RunSequence() {
            if (_sequenceTexts != null) {
                // we wait a frame so that the typewriters can erase all their texts
				yield return null;

                foreach (Typewriter typewriter in _sequenceTexts)
                    yield return typewriter.WaitForTypeWrite(_charactersPerMinute);
            }
        }
    }
}
