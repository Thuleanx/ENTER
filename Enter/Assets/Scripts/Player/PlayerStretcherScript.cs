using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;

using Enter.Utils;

namespace Enter
{
  [DisallowMultipleComponent]
  public class PlayerStretcherScript : MonoBehaviour
  {
    private Transform _spriteTransform;

    [SerializeField, Tooltip("How much the sprite stretches (scales) when jumping")]
    private Vector3 _maxJumpStretch;

    [SerializeField, Tooltip("How much the sprite squishes (scales) when landing")]
    private Vector3 _maxLandSquash;

    private Vector3 _initialestScale;
    private Vector3 _initialScale;
    private Vector3 _targetScale;
    private Vector3 _currentScale
    {
      get { return _spriteTransform.localScale; }
      set { _spriteTransform.localScale = value; }
    }

    private InputData _in;
    private bool _isCrouching;

    #region ================== Methods

    void Start()
    {
      _spriteTransform = PlayerManager.SpriteRenderer.transform;
      _in = InputManager.Instance.Data;

      Assert.IsNotNull(_spriteTransform, "PlayerStretcherScript must have a reference to the player's sprite's Transform.");
      Assert.IsNotNull(_spriteTransform, "PlayerStretcherScript must have a reference to the player's sprite's Transform.");

      _initialestScale = _currentScale;
      _initialScale    = _currentScale;
      _targetScale     = _currentScale;
    }

    void FixedUpdate() {
      handleCrouchAnimation();
    }

    void LateUpdate()
    {
      float yVelocityLerpPoint = (PlayerManager.Rigidbody.velocity.y > 0) ?
        PlayerManager.Rigidbody.velocity.y / PlayerManager.MaxJumpSpeed :
        PlayerManager.Rigidbody.velocity.y / PlayerManager.MaxFallSpeed;

      _targetScale = Vector3.Lerp(_initialScale, _maxJumpStretch, yVelocityLerpPoint);

      _currentScale = _targetScale;
    }

    public void PlayLandingSquash()
    {
      StopAllCoroutines();
      StartCoroutine(landStretch());
    }

    #endregion

    #region ================== Helpers

    private void handleCrouchAnimation() {
      float inY = _in.Move.y;
      if(inY < 0 && !_isCrouching) {
        _isCrouching = true;
        _initialScale = _maxLandSquash;
      }
      if(inY >= 0 && _isCrouching || _in.Move.x != 0) {
        _isCrouching = false;
        _initialScale = _initialestScale;
      }
    }

    private IEnumerator landStretch()
    {
      _initialScale = _maxLandSquash;
      // _targetScale = _maxLandSquash;

      yield return new WaitForSeconds(0.125f); // fixme @ Sebastian

      if(!_isCrouching) {
        _initialScale = _initialestScale;
      }
      // _targetScale = _maxLandSquash;
    }

    #endregion
  }
}
