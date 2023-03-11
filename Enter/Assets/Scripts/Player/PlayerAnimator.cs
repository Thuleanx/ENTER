using System.Collections;
using UnityEngine;
using Enter.Utils;

namespace Enter
{
  public class PlayerAnimator : MonoBehaviour
  {
    [SerializeField, Tooltip("How much the sprite scales when jumping")]
    private Vector3 _maxJumpScale = new Vector3(1, 1.5f, 1);

    [SerializeField, Tooltip("Vertical speed at which the max jump scale is attained")]
    private float _maxJumpSpeed = 1;

    [SerializeField, Tooltip("How much the sprite scales when landing")]
    private Vector3 _landStretch = new Vector3(1.5f, 0.7f, 1);

    [SerializeField, Tooltip("The transition speed to lerp between current and target scales.")]
    private float _transitionSpeed = 50f;

    private Vector3 _initialScale;
    private Vector3 _targetScale;
    private Vector3 _currentScale { 
      get { return PlayerManager.Instance.Player.transform.localScale; }
      set { PlayerManager.Instance.Player.transform.localScale = value; }
    }

    // How close to being on 
    private float _progressToFullJumpStretch = 0;

    private float _landSpeed;

    #region ================== Methods

    void Start()
    {
      _initialScale = _currentScale;
      _targetScale  = _currentScale;
    }

    // Update is called once per frame
    void LateUpdate()
    {
      _currentScale = Math.Damp(
        _currentScale,
        _targetScale,
        _transitionSpeed,
        Time.deltaTime
      );
    }

    public void setMaxJumpVelocity(float max)
    {
      _maxJumpSpeed = max;
    }

    public void setJumpProgressFromYVelocity(Vector3 velocity)
    {
      _progressToFullJumpStretch = velocityToStretchConstant(velocity.y);
      _targetScale = Vector3.Lerp(_initialScale, _maxJumpScale, _progressToFullJumpStretch);
    }

    public void land()
    {
        StartCoroutine(landStretch());
    }

    #endregion

    #region ================== Helpers

    float velocityToStretchConstant(float vel)
    {
        return vel / _maxJumpSpeed;
    }

    private IEnumerator landStretch()
    {
        _targetScale = _landStretch;

        yield return new WaitForSeconds(_landSpeed);

        _targetScale = _initialScale;
    }

    #endregion
  }
}