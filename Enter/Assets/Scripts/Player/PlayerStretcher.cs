using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;

using Enter.Utils;

namespace Enter
{
  public class PlayerStretcher : MonoBehaviour
  {
    [SerializeField] private Transform _spriteTransform;

    [SerializeField, Tooltip("How much the sprite stretches (scales) when jumping")]
    private Vector3 _maxJumpStretch;

    [SerializeField, Tooltip("How much the sprite squishes (scales) when landing")]
    private Vector3 _maxLandSquish;

    private Vector3 _initialScale;
    private Vector3 _targetScale;
    private Vector3 _currentScale
    {
      get { return _spriteTransform.localScale; }
      set { _spriteTransform.localScale = value; }
    }

    #region ================== Accessors

    public float MaxJumpSpeed { get; set; }
    
    #endregion

    #region ================== Methods

    void Awake()
    {
      Assert.IsNotNull(_spriteTransform, "PlayerStretcher must have a reference to Transform _spriteTransform.");
    }

    void Start()
    {
      _initialScale = _currentScale;
      _targetScale = _currentScale;
    }

    void LateUpdate()
    {
      _targetScale = Vector3.Lerp(
        _initialScale, 
        _maxJumpStretch, 
        PlayerManager.PlayerRigidbody.velocity.y / MaxJumpSpeed);

      _currentScale = _targetScale;
    }

    // public void land()
    // {
    //   StartCoroutine(landStretch());
    // }

    #endregion

    #region ================== Helpers

    // private IEnumerator landStretch()
    // {
    //   _targetScale = _maxLandSquish;

    //   yield return new WaitForSeconds(_landSpeed);

    //   _targetScale = _initialScale;
    // }

    #endregion
  }
}
