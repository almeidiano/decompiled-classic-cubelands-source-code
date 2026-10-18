using System.Collections;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
	private PlayerNode playerNode;

	private bool isLocalPlayer;

	private Transform myTrans;

	private MouseLook mouseLookX;

	private MouseLook mouseLookY;

	public AudioClip cameraSound;

	public float animspeed = 2.1f;

	private Animation playerAnimation;

	private Vector3 latestPos;

	private bool isMoving;

	private Vector3 spawnPoint = Vector3.zero;

	private float spawnRot;

	private float lastShare;

	private Vector3 lastSharePos;

	private void Awake()
	{
		myTrans = base.transform;
		MouseLook[] componentsInChildren = GetComponentsInChildren<MouseLook>();
		mouseLookX = componentsInChildren[0];
		mouseLookY = componentsInChildren[1];
	}

	[RPC]
	private void PlayCameraSound()
	{
		base.audio.PlayOneShot(cameraSound);
	}

	public void SetPlayerNode(PlayerNode node)
	{
		playerNode = node;
		isLocalPlayer = playerNode.isLocal;
		if (isLocalPlayer)
		{
			ActivateLocalPlayer();
		}
		else
		{
			ActivateOtherPlayer();
		}
	}

	private void ActivateLocalPlayer()
	{
		GetComponent<FPSWalker>().enabled = true;
		GetComponent<PlayerBuilding>().enabled = true;
		base.gameObject.BroadcastMessage("SetLocal");
		Object.Destroy(base.transform.FindChild("Graphics").gameObject);
	}

	private void ActivateOtherPlayer()
	{
		Object.Destroy(base.transform.FindChild("CameraView/Main Camera").gameObject);
		Object.Destroy(base.transform.FindChild("CameraView/1pCamera").gameObject);
		StartCoroutine(DownloadSkin());
		playerAnimation = base.transform.FindChild("Graphics").FindChild("Character").animation;
		playerAnimation["Walk"].speed = animspeed;
		base.networkView.RPC("ReplyLatestPosRot", RPCMode.Server);
	}

	private IEnumerator DownloadSkin()
	{
		if (!playerNode.boughtGame)
		{
			yield break;
		}
		Debug.Log(playerNode.userID);
		WWW www = new WWW("https://i.imgur.com/m3Sh1lT.png");
		yield return www;
		if (www.error == null && www.size > 0)
		{
			Renderer[] componentsInChildren = GetComponentsInChildren<Renderer>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].material.mainTexture = www.texture;
			}
		}
	}

	[RPC]
	private void ReplyLatestPosRot(NetworkMessageInfo info)
	{
		base.networkView.RPC("SetInitPosRot", info.sender, myTrans.position, mouseLookX.GetRotX());
	}

	[RPC]
	private void SetInitPosRot(Vector3 pos, float rotX)
	{
		myTrans.position = (latestPos = pos);
		mouseLookX.SetRotX(rotX);
	}

	[RPC]
	public void SetYView(float rotY)
	{
		mouseLookY.SetY(rotY);
	}

	private void Update()
	{
		if (!isLocalPlayer)
		{
			myTrans.position = Vector3.Lerp(myTrans.position, latestPos, 25f * Time.deltaTime);
			bool flag = isMoving;
			isMoving = Vector3.Distance(myTrans.position, latestPos) >= 0.01f;
			if (isMoving && !flag)
			{
				playerAnimation.CrossFade("Walk");
			}
			else if (!isMoving && flag)
			{
				playerAnimation.Stop();
			}
		}
	}

	public void SetSpawn()
	{
		spawnPoint = myTrans.position;
		spawnRot = mouseLookX.GetRotX();
	}

	public void Respawn()
	{
		if (spawnPoint == Vector3.zero)
		{
			spawnPoint = WorldData.SP.GetSpawnpoint();
		}
		GetComponent<FPSWalker>().Reset();
		myTrans.position = spawnPoint;
		mouseLookX.SetRotX(spawnRot);
	}

	private void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
	{
		if (stream.isWriting)
		{
			if (Vector3.Distance(lastSharePos, myTrans.position) >= 0.08f && lastShare < Time.time - 0.05f)
			{
				lastSharePos = myTrans.position;
				lastShare = Time.time;
				stream.Serialize(ref lastSharePos);
			}
		}
		else
		{
			stream.Serialize(ref latestPos);
		}
	}
}
