using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using NaughtyAttributes;

namespace Enter {

    [ExecuteAlways]
    public class CrumblingPlatform : MonoBehaviour
    {
        [field:SerializeField, Tooltip("Should be the crumbling platform sprite. Must have rendering type set to tiled. If not set, will search for the first sprite renderer in child")]
        public SpriteRenderer Sprite {get; private set; }

        Color originalColor = Color.white;
        Color crumblingColor = Color.red;

        [SerializeField, Range(0,2), Tooltip("Time between when player lands and when the platform is no longer interactible")] 
        private float preCrumblingDuration = 0.5f;
        [SerializeField, Range(0,2), Tooltip("Time for animating the crumbling")] 
        private float crumblingDuration = 3.0f;
        [SerializeField, Range(0,5), Tooltip("Time between the crumbling animation ends and when the platform regenerates")] 
        private float postCrumblingDuration = 1f;
        [SerializeField] private float edgeColliderYOffset = 0.05f;

        private Vector2 _topLeft_local        => Vector2.zero + Vector2.up / 2.0f + Vector2.left * .45f;
        private Vector2 _topRight_local       => Vector2.zero + Vector2.up / 2.0f + Vector2.right * .45f;
        private Vector2 _topLeft_global       => transform.TransformPoint(_topLeft_local);
        private Vector2 _topRight_global      => transform.TransformPoint(_topRight_local);

        bool crumbling;

        void Awake() {
            Sprite = GetComponentInChildren<SpriteRenderer>();
        }

        void Start() {
            SetupEdgeColliderPoints();
        }

        void Update() {
            // In play mode, you can freely change the scale and the sprite resizes to match
            if (!Application.isPlaying) ReadjustForLength();
        }

        void ReadjustForLength() {
            SetupSprite();
        }

        void SetupEdgeColliderPoints()
        {
            // setup the edge collider at a fixed offset based on edgeColliderYOffset
            EdgeCollider2D edgeCollider = GetComponent<EdgeCollider2D>();

            Vector2 edgeCollliderTopLeft_global   = _topLeft_global;
            Vector2 edgeCollliderTopRight_global  = _topRight_global;

            edgeCollliderTopLeft_global.y += edgeColliderYOffset;
            edgeCollliderTopRight_global.y += edgeColliderYOffset;

            List<Vector2> edgeColliderPoints = new List<Vector2>();
            edgeColliderPoints.Add(transform.InverseTransformPoint(edgeCollliderTopLeft_global));
            edgeColliderPoints.Add(transform.InverseTransformPoint(edgeCollliderTopRight_global));

            edgeCollider.points = edgeColliderPoints.ToArray();
        }

        void SetupSprite() {
            // scale sprite inverse porpotional to the platform's scale
            // and instead scale the sprite renderer's width so it tiles instead of stretch the sprite
            Sprite.transform.localScale = new Vector2(1 / transform.localScale.x, Sprite.transform.localScale.y);
            Sprite.size = new Vector2(transform.localScale.x, Sprite.size.y);
        }


        private void OnTriggerEnter2D(Collider2D other)
        {
            // If player touches edge collider trigger
            if (other.gameObject.tag == "Player" && !crumbling) {
                crumbling = true;
                StartCoroutine(this.Crumble());
            }
        }

        IEnumerator Crumble()
        {
            float originalYPos = Sprite.transform.position.y;

            // shake the sprite a little --- over the precrumbling duration
            // we shake inversely porpotional to scale so both x and y shakes the same amount
            // the last 3 arguments are vibrato, strength, and fade. We don't want fade because we want the shake to abruptedly stop
            // and this moment where it stops is what player can use to time their jumps
            // if they wanna jump at the last moment
            Tween shakeSprite = Sprite.transform.DOShakePosition(preCrumblingDuration, 
                new Vector3(1/transform.localScale.x, 1/transform.localScale.y, 0) * .1f, 10, 50, false);
            shakeSprite.SetEase(Ease.Unset).Play();
            yield return new WaitForSeconds(this.preCrumblingDuration);
            // platform turns uninteratable
            Debug.Log("Platform dies.");

            // disable collision and set the sprite to fall and fade
            Sprite.color = crumblingColor;
            GetComponent<BoxCollider2D>().enabled = false;
            GetComponent<EdgeCollider2D>().enabled = false;

            // feel free to play with the easing functions of the following two tweens
            // falling down to specified Y position. Here, it's -1 from the original position
            Tween fallingPlatform = Sprite.transform.DOMoveY(originalYPos - 1, crumblingDuration);
            fallingPlatform.SetEase(Ease.OutCubic).Play();
            // fade to 0 alpha
            Tween fadePlatformAnimation = Sprite.DOFade(0.0f, crumblingDuration);
            fadePlatformAnimation.SetEase(Ease.OutQuint).Play();

            yield return new WaitForSeconds(this.crumblingDuration);

            // here is where the platform is no longer visible and interactible

            yield return new WaitForSeconds(this.postCrumblingDuration);
            Debug.Log("Platform returns.");

            // remember to reset colors (and also alpha) as well as Y position
            Sprite.color = originalColor;
            Sprite.transform.position = new Vector3(
                Sprite.transform.position.x, 
                originalYPos, 
                Sprite.transform.position.z
            );

            // make interactivity return
            GetComponent<BoxCollider2D>().enabled = true;
            GetComponent<EdgeCollider2D>().enabled = true; //what if platform regenerates before then?
            Debug.Log("Fixed.");

            crumbling = false;
        }
    }
}
