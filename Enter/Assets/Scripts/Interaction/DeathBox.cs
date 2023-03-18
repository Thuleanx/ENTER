using UnityEngine;

namespace Enter
{
  public class DeathBox : Interactable
  {
    protected override void OnInteract()
    {
      base.OnInteract();
      PlayerManager.PlayerScript.Die();
    }
  }
}
