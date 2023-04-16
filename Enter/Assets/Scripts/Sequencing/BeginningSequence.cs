using UnityEngine; 
using System.Collections;
using System.Collections.Generic;

namespace Enter {
    public class BeginningSequence : MonoBehaviour {
        [SerializeField] List<Typewriter> _sequenceTexts;
        [SerializeField] float _charactersPerMinute = 80;

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
