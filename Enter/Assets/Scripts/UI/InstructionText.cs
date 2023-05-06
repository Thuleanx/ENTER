using UnityEngine;
using NaughtyAttributes;
using DG.Tweening;

namespace Enter {
    public class InstructionText : MonoBehaviour {
        [SerializeField] bool autoAppear;
        CanvasGroup _group;
        bool _appeared = false;
        Vector2 _originalPos;

        void Awake() {
            _group = GetComponent<CanvasGroup>();
        }

        void Start() {
            _originalPos = transform.position;
            if (autoAppear) Appear();
        }

        void OnEnable() {
            _appeared = false;
            _group.alpha = 0;
        }

        [Button]
        public void Appear() {
            /* Camera.main.ViewportToWorldPoint(new Vector2(0.5f, 0)); */
            float appearanceDuration = 1;
            float YOffset = 2;
            if (!_appeared) {
                _appeared = true;
                transform.position = _originalPos + YOffset * Vector2.up;

                transform.DOMoveY(_originalPos.y, appearanceDuration);
                _group.DOFade(1, appearanceDuration);
            }
        }
    }
}
