using UnityEngine.InputSystem;
using UnityEngine;
using Enter.Utils;

namespace Enter
{
  public class InputData
  {
    public Vector2 Move;
    public Timer   Jump;
    public bool    JumpHeld;
    public Vector2 Mouse;
    public bool    LDown;
    public bool    RDown;
    
    public Vector3 MouseWorld => Camera.main.ScreenToWorldPoint(new Vector3(Mouse.x, Mouse.y, Camera.main.nearClipPlane));
  }

  [DisallowMultipleComponent]
  [RequireComponent(typeof(PlayerInput))]
  public class InputManager : MonoBehaviour
  {
    public static InputManager Instance;

    public InputData Data { get; private set; } = new InputData();

    [SerializeField] public float inputBufferTime = 0.2f;

    #region ================== Methods

    void Awake()
    {
      Instance = this;
    }

    public void OnMove      (InputAction.CallbackContext c) => Data.Move = c.ReadValue<Vector2>();
    public void OnMouse     (InputAction.CallbackContext c) => Data.Mouse = c.ReadValue<Vector2>();
    public void OnLeftClick (InputAction.CallbackContext c) => Data.LDown = (c.started || c.canceled) ? c.started : Data.LDown;
    public void OnRightClick(InputAction.CallbackContext c) => Data.RDown = (c.started || c.canceled) ? c.started : Data.RDown;
    public void OnJump      (InputAction.CallbackContext c) { 
        if (c.started) Data.Jump = inputBufferTime; // you can assign a float to a timer
        Data.JumpHeld = c.started || c.canceled ? c.started : Data.JumpHeld;
    }

    #endregion
  }
}
