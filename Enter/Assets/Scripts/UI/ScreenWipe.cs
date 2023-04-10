using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NaughtyAttributes;
using DG.Tweening;

namespace Enter {
    public class ScreenWipe : MonoBehaviour {
        enum State {
            BLOCKED,
            BLOCKING,
            UNBLOCKED,
            UNBLOCKING
        }

        [Header("State Information")]
        [SerializeField] private State _startingState = State.BLOCKED;
        [SerializeField, ReadOnly] private State _state;


        // Why are we animating in code?
        // That's because animation on Canvas
        // is quite slow using the Animator due to how the whole 
        // screen refreshes when a single thing moves
        [HorizontalLine(color:EColor.Red)]
        [Header("Components for Animations")]
        [SerializeField, Range(0,3)] private float _blockingDuration;
        [SerializeField] private Ease _blockingEase = Ease.Linear;
        [SerializeField, Range(0,3)] private float _unblockingDuration;
        [SerializeField] private Ease _unblockingEase = Ease.Linear;
        [SerializeField] private RawImage _coverImage;

        public Coroutine Block() {
            if (_state != State.UNBLOCKED) return null;
            // we ddo this before the coroutine, because we don't know exactly when 
            // the coroutine might start execution
            _state = State.BLOCKING;
            return StartCoroutine(_Block());
        }


        public Coroutine Unblock() {
            if (_state != State.BLOCKED)  return null;
            _state = State.UNBLOCKING;
            return StartCoroutine(_Unblock());
        }
        
        IEnumerator _Block() {
            // fade the cover image's alpha to 1
            _coverImage.DOFade(1, _blockingDuration).SetEase(_blockingEase);
            yield return new WaitForSeconds(_blockingDuration);
            _state = State.BLOCKED;
        }

        IEnumerator _Unblock() {
            // fade the cover image's alpha to 0
            _coverImage.DOFade(0, _unblockingDuration).SetEase(_unblockingEase);
            yield return new WaitForSeconds(_unblockingDuration);
            _state = State.UNBLOCKED;
        }
    }
}
