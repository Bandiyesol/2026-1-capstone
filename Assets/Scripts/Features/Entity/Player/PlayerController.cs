using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
	public float moveSpeed;
	public Vector2 lastDirection;

	private Vector2 moveInput;
	private Rigidbody2D rigidBody;
	private SpriteRenderer spriteRenderer;
	private Animator animator;
	private PlayerControls controls;


	void Awake()
	{
		rigidBody = GetComponent<Rigidbody2D>();
		spriteRenderer = GetComponent<SpriteRenderer>();
		animator = GetComponent<Animator>();
	}


	private void OnEnable() {
		if (controls == null) return;
		controls.Enable();
	}


	private void OnDisable()
	{
		if (controls == null) return;
		controls.Disable();
	}


	void Start()
	{
		if (KeyBindManager.Instance == null) return;

		controls = KeyBindManager.Instance.Controls;

		controls.Enable();
	}

	void Update()
	{
		if (KeyBindManager.Instance == null || KeyBindManager.Instance.Controls == null) return;

		moveInput = controls.Player.Move.ReadValue<Vector2>();

		if (moveInput.sqrMagnitude > 0) lastDirection = moveInput.normalized;

		if (moveInput.x != 0) spriteRenderer.flipX = moveInput.x < 0;

		bool isMoving = moveInput.sqrMagnitude > 0;
		animator.SetBool("isMoving", isMoving);
	}

	void FixedUpdate()
	{
		if (rigidBody == null) return;
		if (moveInput.sqrMagnitude > 0.01f) { rigidBody.linearVelocity = moveInput * moveSpeed; }
		else { rigidBody.linearVelocity = Vector2.zero; }
	}
}
