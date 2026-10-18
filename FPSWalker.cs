using UnityEngine;

public class FPSWalker : MonoBehaviour
{
	public float speed = 3.5f;

	public float jumpSpeed = 8f;

	public float gravity = 20f;

	private Vector3 moveDirection = Vector3.zero;

	private bool grounded;

	private Transform transCache;

	private CharacterController controller;

	private PlayerScript playerScript;

	private bool hardMovementLock;

	private float lastFootStep;

	private float horInput;

	private float verInput;

	private float lastJump;

	public void SetMovementLock(bool value)
	{
		hardMovementLock = value;
	}

	private void Awake()
	{
		transCache = base.transform;
		controller = GetComponent<CharacterController>();
		playerScript = GetComponent<PlayerScript>();
	}

	private void LateUpdate()
	{
		bool flag = false;
		if (hardMovementLock)
		{
			return;
		}
		if (grounded)
		{
			if (GameManager.AllowInput())
			{
				horInput = Input.GetAxisRaw("Horizontal");
				verInput = Input.GetAxisRaw("Vertical");
			}
			else
			{
				horInput = (verInput = 0f);
			}
			moveDirection.y = 0f;
			Vector3 direction = new Vector3(horInput, 0f, verInput);
			if (flag && Input.GetKey(KeyCode.LeftShift))
			{
				direction *= 6f;
			}
			else
			{
				direction *= speed;
			}
			direction = base.transform.TransformDirection(direction);
			if (direction.magnitude > moveDirection.magnitude)
			{
				moveDirection = Vector3.Lerp(moveDirection, direction, Time.deltaTime * 10f);
			}
			else
			{
				moveDirection = Vector3.Lerp(moveDirection, direction, Time.deltaTime * 40f);
			}
			if (lastJump < Time.realtimeSinceStartup - 0.5f && Input.GetButton("Jump") && GameManager.AllowInput())
			{
				lastJump = Time.realtimeSinceStartup;
				moveDirection.y = jumpSpeed;
				Statistics.AddStat(Stats.jumps, 1);
			}
		}
		else if (GameManager.AllowInput())
		{
			horInput = Input.GetAxisRaw("Horizontal");
			verInput = Input.GetAxisRaw("Vertical");
			if (horInput == 0f && verInput == 0f)
			{
				moveDirection = Vector3.Lerp(to: new Vector3(0f, moveDirection.y, 0f), from: moveDirection, t: Time.deltaTime * 2f);
			}
		}
		moveDirection.y -= gravity * Time.deltaTime;
		CollisionFlags collisionFlags = controller.Move(moveDirection * Time.deltaTime);
		grounded = (collisionFlags & CollisionFlags.Below) != 0;
	}

	private void Update()
	{
		if (hardMovementLock)
		{
			return;
		}
		if (lastFootStep + 0.4f < Time.time && !base.audio.isPlaying && Mathf.Abs(verInput) + Mathf.Abs(horInput) > 0f)
		{
			Vector3 position = transCache.position;
			if (WorldData.SP.ContainsBlock((ushort)position.x, (ushort)(position.y - 1f), (ushort)position.z))
			{
				lastFootStep = Time.time;
				BlockData blockData = WorldData.SP.GetBlockData((ushort)position.x, (ushort)(position.y - 1f), (ushort)position.z);
				base.audio.clip = WorldData.SP.GetRandomAudioClip(blockData.walkSoundsType);
				base.audio.pitch = Random.Range(0.9f, 1.1f);
				base.audio.Play();
			}
		}
		if (transCache.position.y < -100f)
		{
			Died();
		}
		if (Input.GetButtonDown("SetSpawnpoint") && GameManager.AllowInput())
		{
			playerScript.SetSpawn();
		}
		if (Input.GetButtonDown("Respawn") && GameManager.AllowInput())
		{
			playerScript.Respawn();
		}
	}

	public void Died()
	{
		transCache.position = WorldData.SP.GetSpawnpoint();
		Reset();
		Statistics.AddStat(Stats.deaths, 1);
	}

	public void Reset()
	{
		moveDirection = Vector3.zero;
	}
}
