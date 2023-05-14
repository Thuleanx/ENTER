using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

namespace Enter {
    public class FinalVirus : MonoBehaviour {
        Collider2D col;
        [SerializeField] GameObject waypointsRoot;
        [SerializeField] float radius = 3;
        [SerializeField] Ease moveEase = Ease.Linear;
        [SerializeField] float moveDuration = 1;

        void Awake() {
            col = GetComponentInChildren<Collider2D>();
        }

        void Start() {
            StartCoroutine(_RunSequence());
        }

        IEnumerator _RunSequence() {
            if (!waypointsRoot) yield break;
            List<Transform> waypoints = new List<Transform>(); 
            foreach (Transform child in waypointsRoot.transform)
                waypoints.Add(child);
            if (waypoints.Count == 0) yield break;
            int currentWaypoint = 0;
            {
                Tween move = transform.DOMove(waypoints[0].position, moveDuration).SetEase(moveEase);
                move.Play();
                yield return move.WaitForCompletion();
            }
            transform.position = waypoints[currentWaypoint].position;

            while (currentWaypoint < waypoints.Count - 1) {
                while (Vector2.Distance(InputManager.Instance.Data.MouseWorld, transform.position) > radius)
                    yield return null;
                Tween tween = transform.DOMove(waypoints[currentWaypoint + 1].position, moveDuration).SetEase(moveEase);
                tween.Play();
                yield return tween.WaitForCompletion();
                currentWaypoint = currentWaypoint+1;
            }

            // now pressumably deletion can happen
        }

        public void TriggerDeleteEffect() {
            Debug.Log("Tried deleteing virus");
        }
    }
}
