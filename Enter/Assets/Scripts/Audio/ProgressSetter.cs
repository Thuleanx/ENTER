using UnityEngine;
using FMODUnity;

namespace Enter {
    public class ProgressSetter : MonoBehaviour {
        [SerializeField] int progress;

        void Start() {
            FMODUnity.RuntimeManager.StudioSystem.setParameterByName("progress", progress);
        }
    }
}
