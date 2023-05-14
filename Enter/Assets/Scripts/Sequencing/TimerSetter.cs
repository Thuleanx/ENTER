using UnityEngine;
using FMODUnity;

namespace Enter {
    public class TimerSetter : MonoBehaviour {
        [SerializeField] bool paused;

        void Start() {
            TimerManager.Instance.Paused = paused;
        }
    }
}
