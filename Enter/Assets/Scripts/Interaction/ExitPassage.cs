using UnityEngine;

using Enter.Utils;

namespace Enter
{
  public class ExitPassage : Interactable
  {
    [SerializeField] private SceneReference _nextSceneReference;

    protected override void OnInteract()
    {
      base.OnInteract();

      SceneTransitioner.Instance.TransitionTo(this, _nextSceneReference);
      gameObject.SetActive(false);
    }
  }
}