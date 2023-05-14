using UnityEngine;
using FMODUnity;

namespace Enter {
    public class MusicSetter : MonoBehaviour {
        [SerializeField] EventReference music;
        [SerializeField] bool playOnStart = true;

        void Start() { 
            if (playOnStart) SetMusic();
        }

        public void SetMusic() { MainMusicManager.Instance?.StartMusic( music ); }
    }
}
