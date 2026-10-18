using UnityEngine;

public class PlayerNode
{
	public NetworkPlayer networkPlayer;

	public string name;

	public int userID;

	public Transform transform;

	public bool isLocal;

	public PlayerScript playerScript;

	public bool authenticated;

	public bool boughtGame;

	public bool isAdmin;

	public bool isMod;

	public bool isBuilder;

	public string userSkinID;
}
