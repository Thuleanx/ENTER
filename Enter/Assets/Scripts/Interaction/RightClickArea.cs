using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Enter
{
  public class RightClickArea: MonoBehaviour
  {
    private InputData _in;

    [SerializeField] private LayerMask _rcAreaLayer;
    private Collider2D _col;


    void Start()
    {
      _in = InputManager.Instance.Data;
      _col = this.gameObject.GetComponent<Collider2D>();
    }

    void Update(){
        if (!_in.RDown) return;

        bool yes = _col.OverlapPoint((Vector2) getMouseWorldPosition());

        if (yes) Debug.Log("Clicked, bapey...");
      
    } 

    private Vector3 getMouseWorldPosition()
    {
      // Todo:
      // account for RCBox's own size via an offset,
      // either here or in its parent-child transforms

      return Camera.main.ScreenToWorldPoint(new Vector3(
        _in.Mouse.x,
        _in.Mouse.y,
        Camera.main.nearClipPlane));
    }
  }
}


