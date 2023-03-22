using UnityEngine;

using Enter.Utils;

namespace Enter
{
  public class ExitPassage : MonoBehaviour
  {
    [field:SerializeField, Tooltip("Reference to scene to load, after interacting with this object.")]
    public SceneReference NextSceneReference { get; private set; }

    void OnTriggerEnter2D(Collider2D other)
    {
      if (other.tag != "Player") return;

      SceneTransitioner.Instance.Transition(this);
      gameObject.SetActive(false);
    }
  }
}