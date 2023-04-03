using System;
using UnityEngine;
using Cinemachine;

namespace Enter {
    public class CameraArea : MonoBehaviour {
        [SerializeField, Tooltip("Optional to define. If not defined, will search for a cinemachine virtual camera parent or child, in that order.")] 
        CinemachineVirtualCamera virtualCamera;

        void Awake() {
            if (!virtualCamera) 
                virtualCamera = GetComponentInParent<CinemachineVirtualCamera>();
            if (!virtualCamera)
                virtualCamera = GetComponentInChildren<CinemachineVirtualCamera>();
        }

        void OnTriggerEnter2D(Collider2D collider) {
            // if player runs into this area, we transition into the referenced camera
            if (collider.tag == "Player") {
                CinemachineBrain brain = Camera.main.GetComponent<CinemachineBrain>();
                if (brain) {
                    // we set this camera to highest priority
                    virtualCamera.Priority = brain.ActiveVirtualCamera.Priority;
                    // then push it to the top of the subqueue, effectively making it the "winner" when tiebreaking
                    // this way our priority doesn't inflate as we continue execution
                    virtualCamera.MoveToTopOfPrioritySubqueue();
                }
            }
        }
    }
}
