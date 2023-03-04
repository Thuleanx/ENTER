using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Enter
{
  public class RightClickArea: MonoBehaviour
  {
    private InputData _in;

    void Start()
    {
      _in = InputManager.Instance.Data;
    }

    void Update(){
        if (_in.RDown){
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.ScreenPointToRay(_in.Mouse), out hit)){
                if (hit.collider == GetComponent<Collider>()){
                    Debug.Log("Clicked, bapey...");
                }
                else{
                    Debug.Log(Physics.Raycast(Camera.main.ScreenPointToRay(_in.Mouse)));
                }
            }
            else{
                Debug.Log(Physics.Raycast(Camera.main.ScreenPointToRay(_in.Mouse)));
                Debug.Log("Not hit");
            }
        }
    } 
  }
}
