using UnityEngine;

using Enter.Utils;

namespace Enter
{
  public class ExitPassage : Interactable
  {
    [field:SerializeField, Tooltip("Reference to scene to load, after interacting with this object.")]
    public SceneReference NextSceneReference { get; private set; }

    protected override void OnInteract()
    {
      base.OnInteract();

      SceneTransitioner.Instance.Transition(this);
      gameObject.SetActive(false);
    }
  }
}