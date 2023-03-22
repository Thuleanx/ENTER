using UnityEngine;

namespace Enter
{
  public class DeathBox : MonoBehaviour
  {
    void OnTriggerEnter2D(Collider2D other)
    {
      if (other.tag != "Player") return;

      PlayerManager.PlayerScript.Die();
    }

  }
}
