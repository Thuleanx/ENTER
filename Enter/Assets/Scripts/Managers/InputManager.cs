using UnityEngine.InputSystem;
using UnityEngine;
using Enter.Utils;

namespace Enter
{
  public class InputData
  {
    public Vector2 Move;
    public Timer Jump;
    public bool JumpHeld;
    public Vector2 Mouse;
    public bool LDown;
    public bool RDown;
    public Vector3 MouseWorld => Camera.main.ScreenToWorldPoint(new Vector3(Mouse.x, Mouse.y, Camera.main.nearClipPlane));
  }

  [DisallowMultipleComponent]
  [RequireComponent(typeof(PlayerInput))]
  public class InputManager : MonoBehaviour
  {
    public static InputManager Instance;

    InputData _realInputData = new InputData();
    InputData _overriddenInputData = new InputData();

    public bool OverrideInput = false;
    public InputData Data => OverrideInput ? _overriddenInputData : _realInputData;
    public InputData OverriddenInputData => _overriddenInputData;

    [SerializeField] public float inputBufferTime = 0.2f;

    #region ================== Methods

    void Awake()
    {
        Instance = this;
    }

    public void OnMove      (InputAction.CallbackContext c) => _realInputData.Move  = c.ReadValue<Vector2>();
    public void OnMouse     (InputAction.CallbackContext c) => _realInputData.Mouse = c.ReadValue<Vector2>();
    public void OnLeftClick (InputAction.CallbackContext c) => _realInputData.LDown = (c.started || c.canceled) ? c.started : _realInputData.LDown;
    public void OnRightClick(InputAction.CallbackContext c) => _realInputData.RDown = (c.started || c.canceled) ? c.started : _realInputData.RDown;
    public void OnJump      (InputAction.CallbackContext c)
    {
      if (c.started) _realInputData.Jump = inputBufferTime; // you can assign a float to a timer
      _realInputData.JumpHeld = c.started || c.canceled ? c.started : _realInputData.JumpHeld;
    }

    #endregion
  }
}
