using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Enter
{
  public class RightClickArea: MonoBehaviour
  {
    private InputData _in;
    [SerializeField] private LayerMask _rcAreaLayer;
    private Collider2D col;


    void Start()
    {
      _in = InputManager.Instance.Data;
      col = this.gameObject.GetComponent<Collider2D>();
    }

    void Update(){
        if (_in.RDown){
            if (col.OverlapPoint(_in.Mouse)){
                    Debug.Log("Clicked, bapey...");
                }
                
            else{
                Debug.Log(ClosestPoint(col, _in.Mouse));
            }
        }
    } 

    float ClosestPoint(Collider2D col, Vector2 point)
	{
		GameObject go = new GameObject("tempCollider");
		go.transform.position = point;
		CircleCollider2D c = go.AddComponent<CircleCollider2D>();
		c.radius = 0.1f;
		ColliderDistance2D dist = col.Distance(c);
		Object.Destroy(go);
		return dist.distance;
	}
  }
}


