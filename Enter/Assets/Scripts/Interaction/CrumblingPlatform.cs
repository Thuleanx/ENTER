using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace Enter {

    [ExecuteAlways]
    public class CrumblingPlatform : MonoBehaviour
    {
        [field:SerializeField, Tooltip("Should be the crumbling platform sprite. Must have rendering type set to tiled. If not set, will search for the first sprite renderer in child")]
        public SpriteRenderer Sprite {get; private set; }

        Color originalColor = Color.white;
        Color crumblingColor = Color.red;

        [SerializeField, Range(0,2)] private float preCrumblingDuration = 0.5f;
        [SerializeField, Range(0,2)] private float postCrumblingDuration = 1f;
        [SerializeField, Range(0,5)] private float crumblingDuration = 3.0f;
        [SerializeField] private float edgeColliderYOffset = 0.05f;

        private Vector2 _topLeft_local        => Vector2.zero + Vector2.up / 2.0f + Vector2.left / 2.0f;
        private Vector2 _topRight_local       => Vector2.zero + Vector2.up / 2.0f + Vector2.right / 2.0f;
        private Vector2 _topLeft_global       => transform.TransformPoint(_topLeft_local);
        private Vector2 _topRight_global      => transform.TransformPoint(_topRight_local);

        void Awake() {
            Sprite = GetComponentInChildren<SpriteRenderer>();
        }

        void Start() {
            SetupEdgeColliderPoints();
        }

        void Update() {
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
            Sprite.transform.localScale = new Vector2(1 / transform.localScale.x, Sprite.transform.localScale.y);
            Sprite.size = new Vector2(transform.localScale.x, Sprite.size.y);
        }


        private void OnTriggerEnter2D(Collider2D other)
        {
            // If player touches edge collider trigger
            if (other.gameObject.tag == "Player")
            {
                StartCoroutine(this.Crumble());
            }
        }

        IEnumerator Crumble()
        {
            // for now changing the object color into red
            yield return new WaitForSeconds(this.preCrumblingDuration);
            Sprite.color = crumblingColor;
            GetComponent<EdgeCollider2D>().enabled = false;
            yield return new WaitForSeconds(this.crumblingDuration);
            GetComponent<BoxCollider2D>().enabled = false;
            GetComponent<SpriteRenderer>().enabled = false;
            Debug.Log("Platform dies.");
            yield return new WaitForSeconds(this.postCrumblingDuration);
            Debug.Log("Platform returns.");
            Sprite.color = originalColor;
            GetComponent<BoxCollider2D>().enabled = true;
            GetComponent<SpriteRenderer>().enabled = true;
            GetComponent<EdgeCollider2D>().enabled = true; //what if platform regenerates before then?
            Debug.Log("Fixed.");
            yield return null;
        }
    }
}
