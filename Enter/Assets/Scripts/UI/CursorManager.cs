using UnityEngine;
using System.Collections.Generic;
using NaughtyAttributes;

namespace Enter
{
  public enum CursorType
  {
    Pointer,
    Hover
  }

  public enum CursorTheme
  {
    Light,
    Dark
  }

  public class CursorManager : MonoBehaviour
  {
    public static CursorManager Instance;

    [System.Serializable]
    public struct CursorParams
    {
      public Texture2D texture;
      public Vector2   hotspot;
    }

    [SerializeField] private CursorParams _pointer;
    [SerializeField] private CursorParams _pointerDark;
    [SerializeField] private CursorParams _hover;
    [SerializeField] private CursorParams _hoverDark;
    [ReadOnly, SerializeField] private CursorType  _currentType  = CursorType.Pointer;
    [ReadOnly, SerializeField] private CursorTheme _currentTheme = CursorTheme.Light;

    public HashSet<GameObject> HoveringEntities { get; } = new HashSet<GameObject>();

    void Awake()
    {
      Instance = this;
      SetCursor(_currentType);
    }

    public void SetCursor(CursorType type)
    {
      var argument = (_currentTheme, type);
      CursorParams param = argument switch {
        (CursorTheme.Light, CursorType.Pointer) => _pointer,
        (CursorTheme.Light, CursorType.Hover)   => _hover,
        (CursorTheme.Dark,  CursorType.Pointer) => _pointerDark,
        (CursorTheme.Dark,  CursorType.Hover)   => _hoverDark,
        _ => _pointer
      };
      Cursor.SetCursor(param.texture, param.hotspot, CursorMode.Auto);
    }

    void LateUpdate()
    {
      CursorType desiredCursor = HoveringEntities.Count == 0 ? CursorType.Pointer : CursorType.Hover;

      SetCursor(desiredCursor);
    }
  }
}
