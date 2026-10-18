using System.Collections.Generic;
using UnityEngine;

public class PlayerBuilding : MonoBehaviour
{
	public static PlayerBuilding SP;

	public Transform transBlockPrefab;

	private Transform transBlock;

	private Transform transBlock2;

	public Transform blockToolPrefab;

	private Transform spawnBlockPos;

	private int currentBlock = -1;

	private Transform currentTool;

	private int worldSizeX;

	private int worldSizeZ;

	private int worldSizeY;

	private bool isLocalPlayer;

	private Inventory inventory;

	private float lastBuildOrConstruct;

	private float buildOrConstructRate = 0.3f;

	private void Awake()
	{
		if (SP == null)
		{
			SP = this;
		}
		inventory = Inventory.SP;
	}

	private void SetLocal()
	{
		SP = this;
		isLocalPlayer = true;
		transBlock = (Transform)Object.Instantiate(transBlockPrefab, base.transform.position, Quaternion.identity);
		spawnBlockPos = base.transform.FindChild("CameraView/1pCamera/SpawnBlock");
		base.transform.FindChild("CameraView/1pCamera").parent = null;
		SetBlock(0, realNumber: false);
	}

	private void Start()
	{
		worldSizeX = WorldData.SP.GetWorldSizeX();
		worldSizeZ = WorldData.SP.GetWorldSizeZ();
		worldSizeY = WorldData.SP.GetWorldSizeY();
	}

	public void SetBlock(int newNr, bool realNumber)
	{
		if (inventory.selectedItem < newNr)
		{
			CamViewBobber.AddForce(neg: false);
		}
		else
		{
			CamViewBobber.AddForce(neg: true);
		}
		inventory.selectedItem = newNr;
		if (!realNumber)
		{
			newNr = inventory.inventoryItems[inventory.selectedItem].blockDataID;
		}
		if (currentTool != null && currentTool.gameObject != null)
		{
			MeshFilter component = currentTool.gameObject.GetComponent<MeshFilter>();
			if (component != null && component.mesh != null)
			{
				Object.Destroy(component.mesh);
			}
			Object.Destroy(currentTool.gameObject);
		}
		currentBlock = newNr;
		BlockData blockData = WorldData.SP.GetBlockData(currentBlock);
		currentTool = WorldData.SP.SpawnLocalPlayerCube(spawnBlockPos, blockData).transform;
		currentTool.parent = spawnBlockPos;
		currentTool.localPosition = new Vector3(0f, 0f, 0f);
		currentTool.localEulerAngles = new Vector3(0f, 0f, 0f);
		currentTool.localScale = new Vector3(1f, 1f, 1f);
	}

	private bool GetBuildButton(bool pressed)
	{
		if (!pressed)
		{
			if (Input.GetButton("BuildCube"))
			{
				return true;
			}
			if (!Options.swapMouseButtons)
			{
				return Input.GetMouseButton(1);
			}
			return Input.GetMouseButton(0);
		}
		if (Input.GetButtonDown("BuildCube"))
		{
			return true;
		}
		if (!Options.swapMouseButtons)
		{
			return Input.GetMouseButtonDown(1);
		}
		return Input.GetMouseButtonDown(0);
	}

	private bool GetDestroyButton(bool pressed)
	{
		if (!pressed)
		{
			if (Input.GetButton("RemoveCube"))
			{
				return true;
			}
			if (!Options.swapMouseButtons)
			{
				return Input.GetMouseButton(0);
			}
			return Input.GetMouseButton(1);
		}
		if (Input.GetButtonDown("RemoveCube"))
		{
			return true;
		}
		if (!Options.swapMouseButtons)
		{
			return Input.GetMouseButtonDown(0);
		}
		return Input.GetMouseButtonDown(1);
	}

	private void Update()
	{
		if (!Screen.lockCursor)
		{
			return;
		}
		if (Input.GetKeyDown(KeyCode.Alpha1) && GameManager.AllowInput())
		{
			SetBlock(0, realNumber: false);
		}
		if (Input.GetKeyDown(KeyCode.Alpha2) && GameManager.AllowInput())
		{
			SetBlock(1, realNumber: false);
		}
		if (Input.GetKeyDown(KeyCode.Alpha3) && GameManager.AllowInput())
		{
			SetBlock(2, realNumber: false);
		}
		if (Input.GetKeyDown(KeyCode.Alpha4) && GameManager.AllowInput())
		{
			SetBlock(3, realNumber: false);
		}
		if (Input.GetKeyDown(KeyCode.Alpha5) && GameManager.AllowInput())
		{
			SetBlock(4, realNumber: false);
		}
		if (Input.GetKeyDown(KeyCode.Alpha6) && GameManager.AllowInput())
		{
			SetBlock(5, realNumber: false);
		}
		if (Input.GetKeyDown(KeyCode.Alpha7) && GameManager.AllowInput())
		{
			SetBlock(6, realNumber: false);
		}
		if (Input.GetKeyDown(KeyCode.Alpha8) && GameManager.AllowInput())
		{
			SetBlock(7, realNumber: false);
		}
		if (Input.GetKeyDown(KeyCode.Alpha9) && GameManager.AllowInput())
		{
			SetBlock(8, realNumber: false);
		}
		if (Input.GetKeyDown(KeyCode.Alpha0) && GameManager.AllowInput())
		{
			SetBlock(9, realNumber: false);
		}
		if (GameManager.AllowInput())
		{
			if (Input.GetAxis("Mouse ScrollWheel") < 0f || Input.GetKeyDown(KeyCode.KeypadPlus) || Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.Plus))
			{
				SetBlock((inventory.selectedItem + 1) % Inventory.SP.inventoryItems.Count, realNumber: false);
			}
			else if (Input.GetAxis("Mouse ScrollWheel") > 0f || Input.GetKeyDown(KeyCode.KeypadMinus) || Input.GetKeyDown(KeyCode.Minus))
			{
				int num = (inventory.selectedItem - 1) % Inventory.SP.inventoryItems.Count;
				if (num < 0)
				{
					num = Inventory.SP.inventoryItems.Count - 1;
				}
				SetBlock(num, realNumber: false);
			}
		}
		bool flag = false;
		Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
		_ = Vector3.zero;
		Vector3 vector = Vector3.zero;
		Vector3 zero = Vector3.zero;
		if (Physics.Raycast(ray, out var hitInfo, 5f))
		{
			flag = true;
			Vector3 point = hitInfo.point;
			vector = ray.GetPoint(hitInfo.distance - 0.001f);
			zero = ray.GetPoint(hitInfo.distance + 0.001f);
			Vector3 position = point;
			position.x = (int)Mathf.Round(zero.x);
			position.y = (int)Mathf.Round(zero.y);
			position.z = (int)Mathf.Round(zero.z);
			transBlock.position = position;
		}
		if (Mathf.Abs(transBlock.position.x) >= (float)worldSizeX || Mathf.Abs(transBlock.position.y) >= (float)worldSizeY || Mathf.Abs(transBlock.position.z) >= (float)worldSizeZ || transBlock.position.x < 0f || transBlock.position.y < 0f || transBlock.position.z < 0f)
		{
			flag = false;
		}
		if (flag)
		{
			ushort num2 = (ushort)Mathf.Round(vector.x);
			ushort num3 = (ushort)Mathf.Round(vector.y);
			ushort num4 = (ushort)Mathf.Round(vector.z);
			if (GameManager.AllowInput() && ((GetBuildButton(pressed: false) && lastBuildOrConstruct < Time.time - buildOrConstructRate) || GetBuildButton(pressed: true)))
			{
				lastBuildOrConstruct = Time.time;
				if (BuildIsPossible(num2, num3, num4) && ThisPlayerMayBuild())
				{
					if (GameMenu.SP.GetTutorialStep() == TutorialSteps.Build)
					{
						GameMenu.SP.FinishedTutorialStep();
					}
					CreateBlock(currentBlock, num2, num3, num4);
					if (Network.isClient)
					{
						base.networkView.RPC("ServerCheckConstruct", RPCMode.Server, currentBlock, (int)num2, (int)num3, (int)num4);
					}
					else if (Network.isServer)
					{
						ApplyBuild(currentBlock, num2, num3, num4, Network.player);
					}
				}
			}
			if (GameManager.AllowInput() && ((GetDestroyButton(pressed: false) && lastBuildOrConstruct < Time.time - buildOrConstructRate) || GetDestroyButton(pressed: true)))
			{
				lastBuildOrConstruct = Time.time;
				if (ThisPlayerMayBuild() && WorldData.SP.ContainsBlock((ushort)transBlock.position.x, (ushort)transBlock.position.y, (ushort)transBlock.position.z))
				{
					if (GameMenu.SP.GetTutorialStep() == TutorialSteps.Remove)
					{
						GameMenu.SP.FinishedTutorialStep();
					}
					DestroyBlock((ushort)transBlock.position.x, (ushort)transBlock.position.y, (ushort)transBlock.position.z);
					if (Network.isClient)
					{
						base.networkView.RPC("ServerCheckDestroy", RPCMode.Server, (int)transBlock.position.x, (int)transBlock.position.y, (int)transBlock.position.z);
					}
					else if (Network.isServer)
					{
						ApplyDestroy((int)transBlock.position.x, (int)transBlock.position.y, (int)transBlock.position.z, Network.player);
					}
				}
			}
			if (Input.GetButtonDown("CopyCube") && GameManager.AllowInput())
			{
				if (GameMenu.SP.GetTutorialStep() == TutorialSteps.Copy)
				{
					GameMenu.SP.FinishedTutorialStep();
				}
				BlockInstance blockInstanceQuick = WorldData.SP.GetBlockInstanceQuick((ushort)transBlock.position.x, (ushort)transBlock.position.y, (ushort)transBlock.position.z);
				if (blockInstanceQuick != null)
				{
					int blockDataID = blockInstanceQuick.blockData.blockDataID;
					SetBlock(blockDataID, realNumber: true);
				}
			}
		}
		else
		{
			transBlock.position = new Vector3(0f, -100f, 0f);
		}
	}

	[RPC]
	private void ServerCheckConstruct(int currentBlock, int buildXi, int buildYi, int buildZi, NetworkMessageInfo info)
	{
		ushort num = (ushort)buildXi;
		ushort num2 = (ushort)buildYi;
		ushort num3 = (ushort)buildZi;
		if (BuildIsPossible(num, num2, num3) && GameManager.AllowPlayerBuild(info.sender))
		{
			CreateBlock(currentBlock, num, num2, num3);
			ApplyBuild(currentBlock, num, num2, num3, info.sender);
			return;
		}
		int num4 = -1;
		if (WorldData.SP.ContainsBlock(num, num2, num3))
		{
			num4 = WorldData.SP.GetBlockData(num, num2, num3).blockDataID;
		}
		base.networkView.RPC("ServerHardReset", info.sender, num4, (int)num, (int)num2, (int)num3);
	}

	[RPC]
	private void ServerCheckDestroy(int buildXi, int buildYi, int buildZi, NetworkMessageInfo info)
	{
		ushort num = (ushort)buildXi;
		ushort num2 = (ushort)buildYi;
		ushort num3 = (ushort)buildZi;
		if (WorldData.SP.ContainsBlock(num, num2, num3) && GameManager.AllowPlayerBuild(info.sender))
		{
			DestroyBlock(num, num2, num3);
			ApplyDestroy(num, num2, num3, info.sender);
			return;
		}
		int num4 = -1;
		if (WorldData.SP.ContainsBlock(num, num2, num3))
		{
			num4 = WorldData.SP.GetBlockData(num, num2, num3).blockDataID;
		}
		base.networkView.RPC("ServerHardReset", info.sender, num4, (int)num, (int)num2, (int)num3);
	}

	[RPC]
	private void ApplyBuild(int currentBlock, int buildXi, int buildYi, int buildZi, NetworkPlayer nPlayer)
	{
		if (Network.isServer)
		{
			List<PlayerNode> serverPlayerUpdateList = WorldData.SP.GetServerPlayerUpdateList(buildXi, buildYi, buildZi);
			if (serverPlayerUpdateList == null)
			{
				return;
			}
			{
				foreach (PlayerNode item in serverPlayerUpdateList)
				{
					if (!(nPlayer == item.networkPlayer) && Network.player != item.networkPlayer)
					{
						base.networkView.RPC("ApplyBuild", item.networkPlayer, currentBlock, buildXi, buildYi, buildZi, nPlayer);
					}
				}
				return;
			}
		}
		ushort buildX = (ushort)buildXi;
		ushort buildY = (ushort)buildYi;
		ushort buildZ = (ushort)buildZi;
		if (nPlayer == Network.player)
		{
			Debug.LogError("THIS SHOULD NEVER HAPPEN");
		}
		else
		{
			CreateBlock(currentBlock, buildX, buildY, buildZ);
		}
	}

	[RPC]
	private void ApplyDestroy(int buildXi, int buildYi, int buildZi, NetworkPlayer nPlayer)
	{
		if (Network.isServer)
		{
			List<PlayerNode> serverPlayerUpdateList = WorldData.SP.GetServerPlayerUpdateList(buildXi, buildYi, buildZi);
			if (serverPlayerUpdateList == null)
			{
				return;
			}
			{
				foreach (PlayerNode item in serverPlayerUpdateList)
				{
					if (!(nPlayer == item.networkPlayer) && Network.player != item.networkPlayer)
					{
						base.networkView.RPC("ApplyDestroy", item.networkPlayer, buildXi, buildYi, buildZi, nPlayer);
					}
				}
				return;
			}
		}
		ushort buildX = (ushort)buildXi;
		ushort buildY = (ushort)buildYi;
		ushort buildZ = (ushort)buildZi;
		if (nPlayer == Network.player)
		{
			Debug.LogError("THIS SHOULD NEVER HAPPEN");
		}
		else
		{
			DestroyBlock(buildX, buildY, buildZ);
		}
	}

	[RPC]
	private void ServerHardReset(int currentBlock, int buildXi, int buildYi, int buildZi)
	{
		ushort num = (ushort)buildXi;
		ushort num2 = (ushort)buildYi;
		ushort num3 = (ushort)buildZi;
		if (WorldData.SP.ContainsBlock(num, num2, num3))
		{
			if (currentBlock != 0)
			{
				WorldData.SP.OverWriteBlock(currentBlock, num, num2, num3);
			}
			else
			{
				DestroyBlock(num, num2, num3);
			}
		}
		else if (currentBlock != -1)
		{
			CreateBlock(currentBlock, num, num2, num3);
		}
	}

	private void CreateBlock(int currentBlock, ushort buildX, ushort buildY, ushort buildZ)
	{
		Statistics.AddStat(Stats.cubesBuilt, 1);
		StartCoroutine(WorldData.SP.ConstructBlock(currentBlock, buildX, buildY, buildZ));
	}

	private void DestroyBlock(ushort buildX, ushort buildY, ushort buildZ)
	{
		Statistics.AddStat(Stats.cubesRemoved, 1);
		StartCoroutine(WorldData.SP.DestroyBlock(buildX, buildY, buildZ));
	}

	public static bool BoundsHitSomeWhere(Bounds b1, Bounds b2)
	{
		if (b1.center.x + b1.extents.x < b2.center.x - b2.extents.x || b1.center.x - b1.extents.x > b2.center.x + b2.extents.x)
		{
			return false;
		}
		if (b1.center.y + b1.extents.y < b2.center.y - b2.extents.y || b1.center.y - b1.extents.y > b2.center.y + b2.extents.y)
		{
			return false;
		}
		if (b1.center.z + b1.extents.z < b2.center.z - b2.extents.z || b1.center.z - b1.extents.z > b2.center.z + b2.extents.z)
		{
			return false;
		}
		return true;
	}

	private bool BuildIsPossible(ushort x, ushort y, ushort z)
	{
		if (isLocalPlayer)
		{
			foreach (PlayerNode player in GameManager.SP.GetPlayerList())
			{
				if (player.transform != null && BoundsHitSomeWhere(player.transform.collider.bounds, new Bounds(new Vector3((int)x, (int)y, (int)z), new Vector3(1f, 1f, 1f))))
				{
					return false;
				}
			}
		}
		if (WorldData.SP.ContainsBlock(x, y, z))
		{
			return false;
		}
		if (WorldData.SP.ContainsBlock(x, (ushort)(y - 1), z))
		{
			return true;
		}
		if (WorldData.SP.ContainsBlock(x, (ushort)(y + 1), z))
		{
			return true;
		}
		if (WorldData.SP.ContainsBlock((ushort)(x - 1), y, z))
		{
			return true;
		}
		if (WorldData.SP.ContainsBlock((ushort)(x + 1), y, z))
		{
			return true;
		}
		if (WorldData.SP.ContainsBlock(x, y, (ushort)(z - 1)))
		{
			return true;
		}
		if (WorldData.SP.ContainsBlock(x, y, (ushort)(z + 1)))
		{
			return true;
		}
		if (y == 0)
		{
			return true;
		}
		return false;
	}

	private bool ThisPlayerMayBuild()
	{
		if (!GameManager.AllowPlayerBuild(GameManager.SP.localPlayerNode))
		{
			Chat.ConsoleMessage("Sorry, you need building rights on this server, ask a moderator for permission.");
			return false;
		}
		return true;
	}
}
