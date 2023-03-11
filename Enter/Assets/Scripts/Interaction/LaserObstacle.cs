using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Enter
{
    public class LaserObstacle : Interactable
    {
        [SerializeField]
        private float onDuration;
        [SerializeField]
        private float offDuration;
        [SerializeField]
        private bool on;
        [SerializeField]
        private int numRaycasters;
        private float onTimer;
        private float offTimer;
        private float laserLowest;
        private float floorHeight;

        private SpriteRenderer spriteRenderer;
        private BoxCollider2D boxCollider;
        private LineRenderer lineRenderer;
        public ParticleSystem particleSystem;

		const float maxRaycastDistance = 100;
		[SerializeField] LayerMask groundMask; 
		[SerializeField] LayerMask playerMask;

        // Start is called before the first frame update
        void Start()
        {
            on = true;
            onTimer = onDuration;
            spriteRenderer = GetComponent<SpriteRenderer>();
            boxCollider = GetComponent<BoxCollider2D>();
            lineRenderer = GetComponent<LineRenderer>();
            particleSystem.Clear();
            particleSystem.Stop();
        }

		float shootRaycast(Vector2 pos, LayerMask layerMask) {
            RaycastHit2D hit = Physics2D.Raycast(pos, -transform.up, maxRaycastDistance, layerMask);
			if (hit) {
				if (hit.collider.gameObject.tag == "Player")
					hit.collider.GetComponent<PlayerScript>()?.Die();
				return hit.distance;
			}
			return Mathf.Infinity;
		}

        // Update is called once per frame
        void Update()
        {
            lineRenderer.SetPosition(0, new Vector2(0,0));
			float nearestHitDist = 100;


            if (on) 
            {
                onTimer -= Time.deltaTime;

				// Perform raycasts to determine the nearest object hit
				for (int i = 0; i < numRaycasters + 1; i++) 
				{
					float offset = -transform.localScale.x / 2;
					offset += ((float)i/numRaycasters) * transform.localScale.x;

					Vector2 src = transform.position + transform.right * offset;
					float newHitDist = shootRaycast(src, groundMask);
					shootRaycast(src, playerMask); // for killing the player
					if (newHitDist < nearestHitDist)
						nearestHitDist = newHitDist;
				}

				Debug.Log(nearestHitDist);
                // Turn the laser off when on time ends
                if (onTimer <= 0) 
                {
                    on = false;
                    offTimer = offDuration;
                    lineRenderer.enabled = false;
                    particleSystem.Play();
                }

            	lineRenderer.SetPosition(1, nearestHitDist* Vector2.down);
            }

            else 
            {
                offTimer -= Time.deltaTime;
                // Turn the laser back on when off time ends
                if (offTimer <= 0) 
                {
                    on = true;
                    onTimer = onDuration;
                    lineRenderer.enabled = true;
                    particleSystem.Clear();
                    particleSystem.Stop();
                }
            }
        }
    }
}
