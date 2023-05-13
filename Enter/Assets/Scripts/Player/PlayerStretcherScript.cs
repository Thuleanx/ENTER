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
    private Vector3 _targetStretchScale;
    private Vector3 _targetSquashScale;
    private Vector3 _currentScale
    {
      get { return _spriteTransform.localScale; }
      set { _spriteTransform.localScale = value; }
    }

    private InputData _in;
    private bool _isCrouching;
    private bool _isLanding;
    private PlayerScript _playerScript;


    #region ================== Methods

    void Awake()
    {
      _playerScript = GetComponent<PlayerScript>();
    }

    void Start()
    {
      _spriteTransform = PlayerManager.SpriteRenderer.transform;
      _in = InputManager.Instance.Data;

      Assert.IsNotNull(_spriteTransform, "PlayerStretcherScript must have a reference to the player's sprite's Transform.");
      Assert.IsNotNull(_spriteTransform, "PlayerStretcherScript must have a reference to the player's sprite's Transform.");

      _initialestScale = _currentScale;
      _initialScale    = _currentScale;
      _targetStretchScale     = _currentScale;
      _targetSquashScale      = _currentScale;
    }

    void FixedUpdate() {
      handleCrouchAnimation();
    }

    void LateUpdate()
    {
      float yVelocityLerpPoint;
      if (_playerScript.Grounded) { 
        yVelocityLerpPoint = 0; // if grounded, don't stretch
      } else if (PlayerManager.Rigidbody.velocity.y > 0) {
        yVelocityLerpPoint = PlayerManager.Rigidbody.velocity.y / PlayerManager.MaxJumpSpeed;
      } else {
        yVelocityLerpPoint = PlayerManager.Rigidbody.velocity.y / PlayerManager.MaxFallSpeed;
      }

      _targetStretchScale = Vector3.Lerp(_initialScale, _maxJumpStretch, yVelocityLerpPoint);

      if(_isCrouching || _isLanding) {
        _currentScale = Vector3.Lerp(_currentScale, _targetSquashScale, 0.1f);
      } else {
        _currentScale = _targetStretchScale;
      }
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
        _targetSquashScale = _maxLandSquash;
      }
      if(inY >= 0 && _isCrouching || _in.Move.x != 0) {
        _isCrouching = false;
        _targetSquashScale = _initialestScale;
      }
    }

    private IEnumerator landStretch()
    {
      _isLanding = true;
      _targetSquashScale = _maxLandSquash;
      // _targetStretchScale = _maxLandSquash;

      yield return new WaitForSeconds(0.125f); // fixme @ Sebastian

      if(!_isCrouching) {
        _targetSquashScale = _initialestScale;
      }
      _isLanding = false;
      // _targetStretchScale = _maxLandSquash;
    }

    #endregion
  }
}
