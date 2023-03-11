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
        Vector2 furthestHitPoint;

        private SpriteRenderer spriteRenderer;
        private BoxCollider2D boxCollider;
        public ParticleSystem particleSystem;
        private LineRenderer lineRenderer;

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

            // Determine how high the floor is
            RaycastHit2D hit = Physics2D.Raycast(transform.position - Vector3.up*transform.localScale.y, -transform.up);
            if (hit.collider != null) 
            {
                floorHeight = hit.point.y;
                furthestHitPoint = hit.point;
            }

        }

        Vector2 shootRaycast(Vector3 pos, out bool playerHit) 
        {
            playerHit = false;

            RaycastHit2D hit = Physics2D.Raycast(pos - Vector3.up*transform.localScale.y, -transform.up);
            if (hit.collider != null) 
            {
                // Check if laser hit the player
                if (on && hit.collider.gameObject.CompareTag("Player")) {
                    playerHit = true;
                }
                return hit.point;
            }
            else {
                return furthestHitPoint;
            }
        }

        // Update is called once per frame
        void Update()
        {
            lineRenderer.SetPosition(0, new Vector2(0,0));
            float highestHit = -1000;
            float nearestHitDist = 1000;
            Vector2 nearestHitPoint = furthestHitPoint;
            bool detectedPlayerHit = false;
            float playerHitHeight = 0;
            float playerHitDist = 0;

            // Perform raycasts to determine the nearest object hit
            for (int i = 0; i < numRaycasters + 1; i++) 
            {
                float offset = -transform.localScale.x / 2;
                offset += ((float)i/numRaycasters) * transform.localScale.x;
                bool playerHit = false;
                Vector2 newHitPoint = shootRaycast(transform.position + Vector3.right * offset, out playerHit);
                float newHitDist = Vector2.Distance(transform.position, newHitPoint);
                if (newHitDist < nearestHitDist) {
                    nearestHitDist = newHitDist;
                    nearestHitPoint = newHitPoint;
                }
                
                if (playerHit) {
                    playerHitDist = newHitDist;
                    detectedPlayerHit = true;
                } 
            }

            Debug.Log(nearestHitDist);
            nearestHitPoint = -transform.up * nearestHitDist + transform.position;

            lineRenderer.SetPosition(1, nearestHitPoint - new Vector2(transform.position.x, transform.position.y));
            // If the player is the first object hit, it has been hit by the laser, and make the laser go through the player
            if (detectedPlayerHit && nearestHitDist == playerHitDist) 
            {
                lineRenderer.SetPosition(1, furthestHitPoint - new Vector2(transform.position.x, transform.position.y));
                PlayerManager.Instance.PlayerScript.Die();
            }

            if (on) 
            {
                onTimer -= Time.deltaTime;
                // Turn the laser off when on time ends
                if (onTimer <= 0) 
                {
                    on = false;
                    offTimer = offDuration;
                    lineRenderer.enabled = false;
                    particleSystem.Play();
                }
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
