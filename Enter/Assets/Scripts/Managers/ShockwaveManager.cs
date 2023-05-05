using UnityEngine;
using NaughtyAttributes;

namespace Enter {

[ExecuteAlways]
public class ShockwaveManager : MonoBehaviour {
    public static ShockwaveManager Instance;

    [SerializeField]
    private Material material;

    void Awake() {
        Instance = this;
    }

    [Button]
    public void Spawn() {
        material.SetFloat("_TimeOffset", Time.time);
    }

    public void SpawnAtPos(Vector2 posWS) {
        material.SetFloat("_TimeOffset", Time.time);
        material.SetVector("_FocalPoint", posWS);
    }

    void LateUpdate() {
        material.SetMatrix("_CameraMatrix", Camera.main.projectionMatrix * Camera.main.worldToCameraMatrix);
    }
}

}
