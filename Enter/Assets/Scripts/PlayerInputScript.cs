using UnityEngine.InputSystem;
using UnityEngine;

// A class that exposes player inputs to other classes
// Note: internal does not prevent other classes from modifying this
public class PlayerInputData {
	public Vector2 Move  { get; internal set; }
	public Vector2 Mouse { get; internal set; }
  public bool    LDown { get; internal set; }
  public bool    RDown { get; internal set; }
}

[RequireComponent(typeof(PlayerInput))]
public class PlayerInputScript : MonoBehaviour
{
	// ================== Accessors

	public PlayerInputData InputData { get; private set; } = new PlayerInputData();

	// ================== Methods

	public void OnMove(InputAction.CallbackContext context)
	{
		InputData.Move = context.ReadValue<Vector2>();
	}

  public void OnMouse(InputAction.CallbackContext context)
	{
		InputData.Mouse = context.ReadValue<Vector2>();
	}

	public void OnLeftClick(InputAction.CallbackContext context)
	{
		if (context.started)       InputData.LDown = true;
		else if (context.canceled) InputData.LDown = false;
	}

	public void OnRightClick(InputAction.CallbackContext context)
	{
		if (context.started)       InputData.RDown = true;
		else if (context.canceled) InputData.RDown = false;
	}
}
