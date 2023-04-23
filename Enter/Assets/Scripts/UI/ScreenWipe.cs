using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NaughtyAttributes;
using DG.Tweening;

namespace Enter
{
  public class ScreenWipe : MonoBehaviour
  {
    enum State
    {
      BLOCKED,
      BLOCKING,
      UNBLOCKED,
      UNBLOCKING
    }

    [Header("State Information")]
    [SerializeField]
    private State _startingState = State.BLOCKED;

    [SerializeField, ReadOnly]
    private State _state;

    // Why are we animating in code?
    // That's because animation on Canvas
    // is quite slow using the Animator due to how the whole 
    // screen refreshes when a single thing moves

    [Header("Components for Animation")]
    [SerializeField] private Image _blackOverlay;

    [SerializeField, Range(0,3)]
    private float _blockingDuration;

    [SerializeField]
    private Ease _blockingEase = Ease.Linear;
    
    [SerializeField, Range(0,3)]
    private float _unblockingDuration;
    
    [SerializeField]
    private Ease _unblockingEase = Ease.Linear;

    public Coroutine Block()
    {
      if (_state != State.UNBLOCKED) return null;
      // we do this before the coroutine, because we don't know exactly when 
      // the coroutine might start execution
      return StartCoroutine(_block());
    }

    public Coroutine Unblock()
    {
      if (_state != State.BLOCKED) return null;
      return StartCoroutine(_unblock());
    }
    
    private IEnumerator _block()
    {
      // fade the cover image's alpha to 1
      _state = State.UNBLOCKED;
      _blackOverlay.DOFade(1, _blockingDuration).SetEase(_blockingEase);
      yield return new WaitForSecondsRealtime(_blockingDuration);
      _state = State.BLOCKED;
    }

    private IEnumerator _unblock()
    {
      // fade the cover image's alpha to 0
      _state = State.BLOCKED;
      _blackOverlay.DOFade(0, _unblockingDuration).SetEase(_unblockingEase);
      yield return new WaitForSecondsRealtime(_unblockingDuration);
      _state = State.UNBLOCKED;
    }
  }
}
