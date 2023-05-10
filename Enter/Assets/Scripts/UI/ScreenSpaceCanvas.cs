using UnityEngine;
using UnityEngine.UI;

namespace Enter
{
  [ExecuteAlways]
  [RequireComponent(typeof(Canvas))]
  public class ScreenSpaceCanvas : MonoBehaviour
  {
    private Canvas _canvas;

    void Awake()
    {
      _canvas = GetComponent<Canvas>();
      _canvas.worldCamera = Camera.main;
    }

    void LateUpdate()
    {
      _canvas.worldCamera = Camera.main;
    }
  }
}
