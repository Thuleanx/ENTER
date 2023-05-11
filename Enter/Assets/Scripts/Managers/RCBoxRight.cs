using UnityEngine;
using Enter.Utils;

namespace Enter
{
  [DisallowMultipleComponent]
  [RequireComponent(typeof(BoxCollider2D))]
  public class RCBoxRight : MonoBehaviour
  {
    // Warning: there is a really sneaky hack on this game object!
    // OnMouseEnter/OnMouseExit implicitly rely on raycasts from the camera.
    // However, the RCBox's own box collider can block those raycasts.
    // To get around this, so that we can use these functions, we moved
    // this game object slightly closer to the camera than the RCBox's collider.

    void OnMouseEnter()
    {
      RCBoxManager.Instance.MouseIsOverRight = true;
    }

    void OnMouseExit() 
    {
      RCBoxManager.Instance.MouseIsOverRight = false;
      CursorManager.Instance.SetCursor(CursorType.Pointer);
    }
  }
}
