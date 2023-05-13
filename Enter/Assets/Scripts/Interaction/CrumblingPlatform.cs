using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Assertions;
using System.Collections;
using System.Collections.Generic;

namespace Enter
{
  [ExecuteAlways]
  public class CrumblingPlatform : MonoBehaviour
  {
    [SerializeField, Tooltip("The SpriteRenderer. Must have rendering type set to tiled.")]
    private SpriteRenderer _sr;

    [SerializeField, Tooltip("The BoxCollider2D. This will prevent the player from colliding into the platform.")]
    private BoxCollider2D  _bc;

    [SerializeField, Tooltip("The EdgeCollider2D. THis will detect if the player has landed on the platform.")]
    private EdgeCollider2D _ec;

    Color originalColor  = Color.white;
    Color crumblingColor = Color.red;

    [SerializeField, Tooltip("Original Sprite - not crumbling nor fading/falling.")]
    private Sprite _spriteOriginal;
    [SerializeField, Tooltip("Crumbling Sprite")]
    private Sprite _spriteCrumbling;
    [SerializeField, Tooltip("Falling Sprite")]
    private Sprite _spriteFalling;

    [SerializeField, Tooltip("Time between when player lands and when the platform is no longer interactable")] 
    private float _preCrumblingDuration = 1f;
    
    [SerializeField, Tooltip("Time for animating the crumbling")] 
    private float _crumblingDuration = .5f;

    [SerializeField, Tooltip("Time between the crumbling animation ends and when the platform regenerates")] 
    private float _postCrumblingDuration = 2f;

    [SerializeField, Tooltip("X offset of top-surface edge collider from sides of platform."), OnValueChanged("readjustLength")]
    private float _ecXOffset = 0.05f;

    [SerializeField, Tooltip("Y offset of top-surface edge collider from top of platform."), OnValueChanged("readjustLength")]
    private float _ecYOffset = 0.05f;

    private Vector2 _topLeft_local   => (Vector2.up + Vector2.left)  / 2.0f ;
    private Vector2 _topRight_local  => (Vector2.up + Vector2.right) / 2.0f ;
    private Vector2 _topLeft_global  => transform.TransformPoint(_topLeft_local);
    private Vector2 _topRight_global => transform.TransformPoint(_topRight_local);

    [SerializeField] private UnityEvent OnCrumble;

    bool crumbling;

    void Awake()
    {
      Assert.IsNotNull(_sr, "CrumblingPlatform must have a reference to its SpriteRenderer.");
      Assert.IsNotNull(_bc, "CrumblingPlatform must have a reference to its BoxCollider2D.");
      Assert.IsNotNull(_ec, "CrumblingPlatform must have a reference to its EdgeCollider2D.");
    }

    void Update()
    {
      // In play mode, you can freely change the scale and the sprite resizes to match
      if (!Application.isPlaying) ReadjustForLength();
    }

    void ReadjustForLength()
    {
      SetupSprite();
      SetupEdgeColliderPoints();
    }

    void SetupSprite()
    {
      // Scale sprite inverse proportionally to the platform's scale
      // and instead modify the sprite renderer's width so it tiles, instead of stretches, the sprite
      _sr.transform.localScale = new Vector2(1 / transform.localScale.x, _sr.transform.localScale.y);
      _sr.size = new Vector2(transform.localScale.x, _sr.size.y);
      _sr.sprite = _spriteOriginal;
    }

    void SetupEdgeColliderPoints()
    {
      Vector2 ecTopLeft_global  = _topLeft_global;
      Vector2 ecTopRight_global = _topRight_global;

      ecTopLeft_global.y  += _ecYOffset;
      ecTopRight_global.y += _ecYOffset;
      
      ecTopLeft_global.x  += _ecXOffset;
      ecTopRight_global.x -= _ecXOffset;

      _ec.points = new Vector2[]{
        transform.InverseTransformPoint(ecTopLeft_global),
        transform.InverseTransformPoint(ecTopRight_global)
      };
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
      // If player touches edge collider trigger
      if (other.gameObject.tag == "Player" && !crumbling)
      {
        crumbling = true;
        StartCoroutine(_crumble());
      }
    }

    private IEnumerator _crumble()
    {
      float originalYPos = _sr.transform.position.y;

      // shake the sprite a little --- over the pre-crumbling duration
      // we shake inversely proportionally to scale so both x and y shakes the same amount
      // the last 3 arguments are vibrato, strength, and fade.
      // we don't want fade because we want the shake to abruptly stop
      // and this moment where it stops is what player can use to time their jumps
      // if they wanna jump at the last moment
      _sr.sprite = _spriteCrumbling;
      Tween shakeSprite = _sr.transform.DOShakePosition(
        _preCrumblingDuration, 
        new Vector3(1 / transform.localScale.x, 1 / transform.localScale.y, 0) * .1f,
        10,
        50,
        false);
      shakeSprite.SetEase(Ease.Unset).Play();

      yield return new WaitForSeconds(_preCrumblingDuration);

      // platform turns un-interactable

      // disable collision and set the sprite to fall and fade
      _sr.sprite = _spriteFalling;
      _bc.enabled = false;
      _ec.enabled = false;

      // Feel free to play with the easing functions of the following two tweens
      // falling down to specified Y position. Here, it's -1 from the original position
      OnCrumble?.Invoke();
      Tween fallingPlatform = _sr.transform.DOMoveY(originalYPos - 1, _crumblingDuration);
      fallingPlatform.SetEase(Ease.OutCubic).Play();

      // Fade to 0 alpha
      Tween fadePlatformAnimation = _sr.DOFade(0.0f, _crumblingDuration);
      fadePlatformAnimation.SetEase(Ease.OutQuint).Play();

      yield return new WaitForSeconds(_crumblingDuration);

      // Here is where the platform is no longer visible and interactable

      yield return new WaitForSeconds(_postCrumblingDuration);

      // Remember to reset colors (and also alpha) as well as Y position
      _sr.color = Color.white;
      _sr.sprite = _spriteOriginal;
      _sr.transform.position = new Vector3(
        _sr.transform.position.x, 
        originalYPos, 
        _sr.transform.position.z
      );

      // Make interactivity return
      _bc.enabled = true;
      _ec.enabled = true; // What if platform regenerates before then?

      crumbling = false;
    }
  }
}
