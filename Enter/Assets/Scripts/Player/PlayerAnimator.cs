using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Enter.Utils;

public class PlayerAnimator : MonoBehaviour
{
    private GameObject _charSprite;

    [SerializeField, Tooltip("How much the sprite scales when jumping")]
    private Vector3 _jumpVerticalStretch = new Vector3(1, 1.5f, 1);

    [SerializeField, Tooltip("How much the sprite scales when landing")]
    private Vector3 _landStretch = new Vector3(1.5f, 0.7f, 1);
    private Vector3 _defaultStretch = new Vector3(1f, 1f, 1f);

    [SerializeField, Tooltip("The time that it takes after jumping to bounce back to original scale.")]
    private float _postJumpDelay = 0.5f;

    private Vector3 _targetScale = new Vector3(1, 1, 1);
    private float _transitionSpeed;

    [SerializeField, Tooltip("The default transition speed to lerp between scales.")]
    private float _defaultTransitionSpeed = 10f;

    [SerializeField, Tooltip("The transition speed to lerp between scales when jumping.")]
    private float _jumpingTransitionSpeed = 20f;

    private float _maxJumpSpeed = 1f;
    // How close to being on 
    private float _progressToFullJumpStretch = 0;

    private float _landSpeed;
    void Start()
    {
        _charSprite = this.gameObject.transform.GetChild(0).gameObject;
        _targetScale = _defaultStretch;
        _transitionSpeed = _defaultTransitionSpeed;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        _charSprite.gameObject.transform.localScale = Math.Damp(
            _charSprite.gameObject.transform.localScale,
            _targetScale, _transitionSpeed, Time.deltaTime
        );
    }

    public void snapToDef()
    {
        _charSprite.gameObject.transform.localScale = _defaultStretch;
        _targetScale = _defaultStretch;
        _transitionSpeed = _defaultTransitionSpeed;
    }

// CURRENTLY, we are just interpolating based on y velocity
    // public void jump()
    // {
    //     // _charSprite.gameObject.transform.localScale = _jumpVerticalStretch;
    //     StartCoroutine(jumpStretch());
    // }

    // private IEnumerator jumpStretch()
    // {
    //     _targetScale = _jumpVerticalStretch;
    //     _transitionSpeed = _defaultTransitionSpeed;

    //     yield return new WaitForSeconds(_postJumpDelay);

    //     _targetScale = _defaultStretch;
    //     _transitionSpeed = _jumpingTransitionSpeed;
    // }

    // Set the max jump velocity so that we can scale the jump progress
    // accordingly
    public void setMaxJumpVelocity(float max)
    {
        _maxJumpSpeed = max;
    }

    public void setJumpProgressFromYVelocity(Vector3 velocity)
    {
        _progressToFullJumpStretch = velocityToStretchConstant(velocity.y);
        _targetScale = Vector3.Lerp(_defaultStretch, _jumpVerticalStretch, _progressToFullJumpStretch);
    }

    float velocityToStretchConstant(float vel)
    {
        return vel / _maxJumpSpeed;
    }

    public void land()
    {
        StartCoroutine(landStretch());
    }

    private IEnumerator landStretch()
    {
        _targetScale = _landStretch;
        _charSprite.gameObject.transform.localScale = _landStretch;

        yield return new WaitForSeconds(_landSpeed);

        _targetScale = _defaultStretch;
    }
}
