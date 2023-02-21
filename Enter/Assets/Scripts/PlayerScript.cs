using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInputScript))]
public class PlayerScript : MonoBehaviour
{
	private PlayerInputData _inputData;
	private Rigidbody2D     _rigidbody;

  // ================== Methods

  void Awake()
	{
		_inputData = GetComponent<PlayerInputScript>().InputData;
		_rigidbody = GetComponent<Rigidbody2D>();
	}

  // FixedUpdate is called once per physics tick
  void FixedUpdate()
  {
    handleMovement();
  }

	// ================== Helpers

  private void handleMovement()
  {
    _rigidbody.velocity = _inputData.Move;
  }
}
