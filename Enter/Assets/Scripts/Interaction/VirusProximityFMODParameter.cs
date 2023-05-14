using UnityEngine;
using FMODUnity;

namespace Enter {
    [RequireComponent(typeof(FMODUnity.StudioEventEmitter))]
    public class VirusProximityFMODParameter : MonoBehaviour {
        FMODUnity.StudioEventEmitter emitter;

        void Awake() {
            emitter = GetComponent<FMODUnity.StudioEventEmitter>();
        }

        void Update() {
            if (VirusBoxProximityManager.Instance && emitter) emitter.SetParameter("distanceToClosestVirus", VirusBoxProximityManager.Instance.GetClosestVirus(transform.position));
        }
    }
}
