using UnityEngine;
using FMODUnity;

namespace Enter {
    public class MainMusicManager : MonoBehaviour {
        public static MainMusicManager Instance {get; private set; }

        FMOD.Studio.EventInstance music;

        void Awake() {
            Instance = this;
        }

        public void StartMusic(EventReference eventReference) {
            if (music.isValid()) {
                music.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                music.release();
            }
            music = FMODUnity.RuntimeManager.CreateInstance(eventReference);
            music.start();
        }
    }
}
