using UnityEngine; 
using UnityEngine.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using DG.Tweening;
using Enter.Utils;

namespace Enter
{
  public class BeginningSequence : MonoBehaviour
  {
    [SerializeField] float _linesPerMinute = 30;
    [SerializeField, Range(0, 10)] float _scrollDelay = 2;
    [SerializeField] float _delayScaleBetweenSpawn;
    [SerializeField] CinemachineVirtualCamera _fallingCamera;
    [SerializeField] GameObject _corruptedBox;
    [SerializeField] GameObject _environment;
    [SerializeField] Transform _virusPosition;
    [SerializeField] List<CanvasGroup> _backgroundCanvases;
    [SerializeField] UnityEvent OnBegin;
    [SerializeField] UnityEvent OnVirusExit;

    List<Typewriter> _sequenceTexts;

    void Awake()
    {
      _sequenceTexts = new List<Typewriter>(GetComponentsInChildren<Typewriter>());
      foreach (var bg in _backgroundCanvases) 
        bg.alpha = 0;
      InputManager.Instance.OverrideInput = true;
    }

    public void RunSequence()
    {
      StartCoroutine(_RunSequence());
    }

    IEnumerator _RunSequence()
    {
      InputManager.Instance.OverrideInput = true;

      if (_sequenceTexts != null)
      {
        // We wait a frame so that the typewriters can erase all their texts
        yield return null;

        Coroutine typewriting = StartCoroutine(_Typewrite());

        OnBegin?.Invoke();

        yield return new WaitForSecondsRealtime(_scrollDelay);


        // Spawn some squares
        List<GameObject> corruptedBoxes = new List<GameObject>();
        int numCorruptedBoxes = 100;

        Ease easingForWait = Ease.OutExpo;

        int skip = 0;

        RectTransform rectTransform = GetComponent<RectTransform>();
        float lastHeight = rectTransform.rect.height;

        for (int i = 0; i < numCorruptedBoxes; i++)
        {
          float screenX = UnityEngine.Random.Range(0.0f,1.0f);
          float screenY = UnityEngine.Random.Range(0.0f, i>3 ? 1.0f : .5f);

          Vector2 pos = new Vector2(screenX, screenY);
          pos = Camera.main.ViewportToWorldPoint(pos);

          Vector2 size = new Vector2(
              UnityEngine.Random.Range(0.05f,0.2f),
              UnityEngine.Random.Range(0.05f,0.2f)
          );
          size = Camera.main.ViewportToWorldPoint(size) - Camera.main.ViewportToWorldPoint(Vector2.zero);

          GameObject corruptedBox = BubbleManager.Instance.Borrow(gameObject.scene, _corruptedBox, pos);
          SpriteRenderer renderer = corruptedBox.GetComponentInChildren<SpriteRenderer>();
          renderer.size = size;
          corruptedBoxes.Add(corruptedBox);

          corruptedBox.transform.localScale = Vector2.one * 1.5f;
          corruptedBox.transform.DOScale(Vector2.one, 0.2f).SetEase(Ease.InCirc);

          float wait = DOVirtual.EasedValue(4f * _delayScaleBetweenSpawn, 0f, ((float)i) / numCorruptedBoxes, easingForWait);
          if (i > 0) wait = Mathf.Min(wait, 2f/i);

          renderer.transform.DOShakePosition( 50, 0.2f).Play();

          if (skip == 0)
          {
            skip += (i+4)/5;

            Timer waiting = wait;
            while (waiting) {
              float deltaHeight = rectTransform.rect.height - lastHeight;
              if (deltaHeight > 0.1f)
              {
                foreach (GameObject box in corruptedBoxes) 
                    box.transform.position += deltaHeight * Vector3.up;
                lastHeight = rectTransform.rect.height;
              }
              yield return null;
            }
          } else skip--;
        }

        float convergenceDuration = 3.0f;
        DOTween.KillAll();
        StopCoroutine(typewriting);
        // Fade all texts
        GetComponent<CanvasGroup>()?.DOFade(0, 1);

        Tween moveDown = _environment.transform.DOMoveY(_virusPosition.transform.position.y, 2);

        foreach (GameObject corruptedBox in corruptedBoxes) 
          corruptedBox.transform.localScale = Vector2.one;

        // Make the boxes approach 0
        foreach (GameObject corruptedBox in corruptedBoxes)
        {
          float distanceToVirus = UnityEngine.Random.Range(0.0f, 3.0f);

          Vector2 interpolatedPoint = (transform.position - _virusPosition.transform.position).normalized * distanceToVirus;
          interpolatedPoint = interpolatedPoint + (Vector2) _virusPosition.transform.position;

          float actualConvergenceDuration = UnityEngine.Random.Range(0.8f * convergenceDuration, 1.25f * convergenceDuration);

          float delay = actualConvergenceDuration * 0.3f;

          corruptedBox.GetComponentInChildren<SpriteRenderer>().transform.DOShakePosition( 50, 1f, 10, 90, false ).Play().SetDelay(delay);
          corruptedBox.transform.DOMove( interpolatedPoint, actualConvergenceDuration ).SetEase(Ease.InElastic);
        }

        yield return new WaitForSecondsRealtime(convergenceDuration * 1.25f);

        // All boxes circles around virus position and moves with it

        List<Vector2> displacementFromVirus = new List<Vector2>();

        foreach (GameObject corruptedBox in corruptedBoxes) 
          displacementFromVirus.Add(corruptedBox.transform.position - _virusPosition.transform.position);

        float movingDuration = 2;
        float screenEdge = Camera.main.ViewportToWorldPoint(new Vector2(1.0f, 0.0f)).x;

        while (moveDown.IsPlaying())
          yield return null;

        Tween tween = _virusPosition.transform.DOMoveX(screenEdge + 50, movingDuration).SetEase(Ease.OutCirc).Play().SetDelay(2.0f);
        while (tween.IsPlaying())
        {
          for (int i = 0; i < corruptedBoxes.Count; i++)
          {
            GameObject corruptedBox = corruptedBoxes[i];
            corruptedBox.transform.position = (Vector2) _virusPosition.transform.position + displacementFromVirus[i];
          }
          yield return null;
        }
        OnVirusExit?.Invoke();
        foreach (var bg in _backgroundCanvases)
        {
          bg.DOFade(1, 2);
        }

        if (ColorUtility.TryParseHtmlString("#77A52C", out Color backgroundColor))
        {
          Camera.main.DOColor(backgroundColor, 2);
        }

        // We have player walk in
        InputManager.Instance.OverriddenInputData.Move = Vector2.right;
        // Right to the very right edge of the screen lol.

        // While (PlayerManager.Player.transform.position.x < -15)
        while (true)
        {
          yield return null;
        }
      }

      InputManager.Instance.OverrideInput = false;
    }

    void OnDisable()
    {
      InputManager.Instance.OverrideInput = false;
    }

    IEnumerator _Typewrite()
    {
      if (_sequenceTexts != null)
      {
        foreach (Typewriter typewriter in _sequenceTexts)
          yield return typewriter.WaitForTypeWrite(_linesPerMinute);
      }
    }
  }
}
