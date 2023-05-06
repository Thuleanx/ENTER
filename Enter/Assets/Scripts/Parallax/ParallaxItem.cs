using UnityEngine;

namespace Enter {
    public class ParallaxItem : MonoBehaviour {
        [SerializeField, Range(-1,1)] public float parallaxValue;
        [SerializeField, Range(-1,1)] public float parallaxValueX;
        [SerializeField, Range(-1,1)] public float parallaxValueY;
    }
}
