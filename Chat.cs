using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chat : MonoBehaviour
{
	public GUISkin skin;

	public bool showChat;

	private string inputField = string.Empty;

	private List<ChatEntry> entries = new List<ChatEntry>();

	private Vector2 scrollPosition;

	private string playerName;

	private float lastUnfocus;

	public static bool usingChat;

	public static Chat SP;

	public static bool IsUsing()
	{
		return usingChat;
	}

	private void Awake()
	{
		SP = this;
		playerName = AccountManager.GetUsername();
		if (playerName == null || playerName.Length <= 1)
		{
			playerName = "Player" + Random.Range(1, 100);
		}
	}

	public void CloseChatWindow()
	{
		showChat = false;
		inputField = string.Empty;
		entries = new List<ChatEntry>();
	}

	private void OnGUI()
	{
		if (DedicatedServer.isDedicated)
		{
			return;
		}
		GUI.skin = skin;
		ShowChatBubbles();
		if (!showChat)
		{
			if (!Network.isClient && !Network.isServer)
			{
				return;
			}
			showChat = true;
			addGameChatMessage(playerName + " joined the game");
		}
		if (!showChat)
		{
			return;
		}
		GUILayout.BeginArea(new Rect(7f, Screen.height - 300 + 5, 500f, 300f));
		GUI.BeginGroup(new Rect(0f, 0f, 500f, 300f));
		GlobalChatWindow();
		GUI.EndGroup();
		GUILayout.EndArea();
		if (Event.current.type == EventType.KeyDown && Event.current.character == '\n' && lastUnfocus + 0.25f < Time.realtimeSinceStartup)
		{
			if (!usingChat)
			{
				StartCoroutine(StartUsingChat());
			}
			else
			{
				StopUsingChat();
			}
		}
		if (usingChat && Input.GetKey(KeyCode.Escape))
		{
			StopUsingChat();
		}
	}

	private void ShowChatBubbles()
	{
		Color color = GUI.color;
		foreach (ChatEntry entry in entries)
		{
			if (entry.showBubble && entry.sender != null && entry.sender.networkPlayer != Network.player && entry.sender.transform != null)
			{
				Vector3 position = entry.sender.transform.position;
				position += new Vector3(0f, 1.2f, 0f);
				Vector3 vector = Camera.main.WorldToScreenPoint(position);
				vector.y = (float)Screen.height - vector.y;
				float num = (Time.realtimeSinceStartup - entry.timeAdded) * 10f;
				vector.y -= num;
				if (vector.z > 0f && vector.x >= 0f && vector.x <= (float)Screen.width && vector.y >= 0f && vector.y <= (float)Screen.height)
				{
					GUI.color = entry.color;
					GUI.Label(new Rect(vector.x - 75f, vector.y - 10f, 150f, 20f), entry.text, "LabelCentered");
				}
			}
		}
		GUI.color = color;
	}

	private IEnumerator StartUsingChat()
	{
		usingChat = true;
		yield return 0;
	}

	private void StopUsingChat()
	{
		inputField = string.Empty;
		lastUnfocus = Time.realtimeSinceStartup;
		usingChat = false;
	}

	private void GlobalChatWindow()
	{
		GUILayout.BeginVertical();
		GUILayout.FlexibleSpace();
		int num = 0;
		for (int i = 0; i < entries.Count; i++)
		{
			ChatEntry chatEntry = entries[i];
			if (usingChat || !(chatEntry.timeAdded < Time.realtimeSinceStartup - 7f))
			{
				if (!usingChat && num >= 5)
				{
					break;
				}
				num++;
				DrawOutline(chatEntry.text);
			}
		}
		if (Event.current.type == EventType.KeyDown && Event.current.character == '\n' && inputField.Length > 0)
		{
			inputField = inputField.Replace("\n", "\\n");
			string text = string.Empty + playerName + ":  " + inputField;
			if (inputField.Substring(0, 1) == "/")
			{
				ParseChatCommand(inputField.Substring(1));
			}
			else
			{
				base.networkView.RPC("ApplyGlobalChatText", RPCMode.All, text, false, Network.player);
			}
		}
		if (usingChat)
		{
			GUI.SetNextControlName("Chat input field");
			inputField = GUILayout.TextField(inputField, GUILayout.Height(24f));
			if (GUI.GetNameOfFocusedControl() != "Chat input field")
			{
				GUI.FocusControl("Chat input field");
			}
		}
		else
		{
			GUILayout.Space(28f);
		}
		if (GUI.changed && !usingChat)
		{
			StartCoroutine(StartUsingChat());
		}
		if (Input.GetKeyDown("mouse 0") && usingChat)
		{
			StopUsingChat();
		}
		GUILayout.Space(10f);
		GUILayout.EndVertical();
	}

	private void ParseChatCommand(string command)
	{
		string[] array = command.Split(' ');
		string text = "Error";
		text = array[0] switch
		{
			"save" => SaveCommand(command.Substring(array[0].Length)), 
			"kick" => KickCommand(command.Substring(array[0].Length)), 
			"ban" => BanCommand(command.Substring(array[0].Length)), 
			"unban" => UnbanCommand(command.Substring(array[0].Length)), 
			"mods" => "Mods: " + GameSettings.ListToString(GameSettings.modList), 
			"admins" => "Admins: " + GameSettings.ListToString(GameSettings.adminList), 
			"bans" => "Bans: " + GameSettings.ListToString(GameSettings.bannedList), 
			"builders" => "Builders: useList=" + GameSettings.useBuilderList + " - " + GameSettings.ListToString(GameSettings.builderList), 
			"setspawnpoint" => ChatCommandSetSpawnpoint(command.Substring(array[0].Length)), 
			"roll" => ChatCommandRoll(command.Substring(array[0].Length)), 
			"getspawnpoint" => "Spawnpoint= " + WorldData.SP.GetSpawnpoint().ToString(), 
			"help" => "Available commands: mods admins bans setspawnpoint getspawnpoint", 
			_ => "Unknown chat command: \"" + array[0] + "\" try help instead", 
		};
		if (text != string.Empty)
		{
			ApplyGlobalChatText(text, gameMessage: true, Network.player);
		}
	}

	private string SaveCommand(string input)
	{
		if (!GameManager.IsMod(GameManager.SP.localPlayerNode))
		{
			return "You need to be an admin or mod for this command";
		}
		if (Network.isServer)
		{
			GameManager.SP.RunManualSave();
		}
		else
		{
			base.networkView.RPC("RequestWorldSave", RPCMode.Server);
		}
		return "Starting saving world";
	}

	private string KickCommand(string input)
	{
		if (!GameManager.IsMod(GameManager.SP.localPlayerNode))
		{
			return "You need to be an admin or mod for this command";
		}
		if (input.Length >= 3)
		{
			string text = input.Substring(1);
			PlayerNode player = GameManager.SP.GetPlayer(text);
			if (player == null)
			{
				return "The user \"" + text + "\" is not on this server.";
			}
			GameManager.SP.RequestKick(player.networkPlayer);
			return "Kicked " + text;
		}
		return "Kick failed; you need to specify a username";
	}

	private string BanCommand(string input)
	{
		if (!GameManager.IsMod(GameManager.SP.localPlayerNode))
		{
			return "You need to be an admin or mod for this command";
		}
		if (input.Length >= 3)
		{
			string text = input.Substring(1);
			PlayerNode player = GameManager.SP.GetPlayer(text);
			if (player == null)
			{
				return "The user \"" + text + "\" is not on this server.";
			}
			GameManager.SP.Ban(player.networkPlayer);
			return "Banned " + text;
		}
		return "Ban failed; you need to specify a username";
	}

	private string UnbanCommand(string input)
	{
		if (!GameManager.IsMod(GameManager.SP.localPlayerNode))
		{
			return "You need to be an admin or mod for this command";
		}
		if (input.Length >= 3)
		{
			string text = input.Substring(1);
			GameManager.SP.Unban(text);
			return "Unbanned " + text;
		}
		return "Unban failed; you need to specify a username";
	}

	private string ChatCommandRoll(string input)
	{
		int num = 0;
		string[] array = input.Split(' ');
		if (array.Length == 2)
		{
			try
			{
				num = int.Parse("0" + array[1]);
			}
			catch
			{
			}
		}
		if (num <= 0)
		{
			num = 6;
		}
		if (Network.isServer)
		{
			ServerDoRoll(num, GameManager.SP.localPlayerNode.name);
		}
		else
		{
			base.networkView.RPC("AskRoll", RPCMode.Server, num);
		}
		return string.Empty;
	}

	[RPC]
	private void AskRoll(int maxNumb, NetworkMessageInfo info)
	{
		PlayerNode player = GameManager.SP.GetPlayer(info.sender);
		if (player != null)
		{
			ServerDoRoll(maxNumb, player.name);
		}
	}

	private void ServerDoRoll(int maxNumb, string name)
	{
		addGameChatMessage(name + " rolled " + Random.Range(1, maxNumb) + " (1 - " + maxNumb + ")");
	}

	private string ChatCommandSetSpawnpoint(string input)
	{
		if (!GameManager.IsAdmin(GameManager.SP.localPlayerNode))
		{
			return "You need to be an admin for this command";
		}
		Vector3 position = GameManager.SP.localPlayerNode.transform.position;
		position = new Vector3(Mathf.Round(position.x), Mathf.Round(position.y) + 2f, Mathf.Round(position.z));
		if (input.Length > 3)
		{
			string[] array = input.Split(' ');
			if (array.Length == 4)
			{
				int num = int.Parse(array[1]);
				int num2 = int.Parse(array[2]);
				int num3 = int.Parse(array[3]);
				position = new Vector3(num, num2, num3);
			}
		}
		position = new Vector3(Mathf.Clamp((int)position.x, 0, WorldData.SP.GetWorldSizeX()), Mathf.Clamp((int)position.y, 0, WorldData.SP.GetWorldSizeY() + 200), Mathf.Clamp((int)position.z, 0, WorldData.SP.GetWorldSizeZ()));
		WorldData.SP.SetSpawnpoint(position);
		Vector3 vector = position;
		return "Spawnpoint set to " + vector.ToString();
	}

	public void postRecord(string str)
	{
		addGameChatMessage(playerName + str);
	}

	public void addGameChatMessage(string str)
	{
		base.networkView.RPC("ApplyGlobalChatText", RPCMode.All, str, true, Network.player);
	}

	public void addGameChatMessage(string str, NetworkPlayer player)
	{
		base.networkView.RPC("ApplyGlobalChatText", player, str, true, Network.player);
	}

	public static Rect DrawOutline(string text)
	{
		string text2 = "ChatEntryOut";
		int num = 1;
		GUILayout.Label(text, text2);
		Rect lastRect = GUILayoutUtility.GetLastRect();
		lastRect.y += num;
		lastRect.x -= num;
		GUI.Label(lastRect, text, text2);
		lastRect.x += num * 2;
		GUI.Label(lastRect, text, text2);
		lastRect.x -= num;
		lastRect.y += num;
		GUI.Label(lastRect, text, text2);
		lastRect.y -= num;
		text2 = "ChatEntry";
		GUI.Label(lastRect, text, text2);
		return lastRect;
	}

	public static void ConsoleMessage(string msg)
	{
		if (!SP.HasMessage(msg))
		{
			SP.ApplyGlobalChatText(msg, gameMessage: true, Network.player);
		}
	}

	public bool HasMessage(string msg)
	{
		foreach (ChatEntry entry in entries)
		{
			if (entry.text == msg)
			{
				return true;
			}
		}
		return false;
	}

	[RPC]
	public void ApplyGlobalChatText(string str, bool gameMessage, NetworkPlayer sendBy)
	{
		int num = 100;
		if (str.Length >= num * 2)
		{
			str = str.Substring(0, num * 2);
		}
		ChatEntry chatEntry = new ChatEntry();
		chatEntry.sender = GameManager.SP.GetPlayer(sendBy);
		chatEntry.text = str;
		chatEntry.timeAdded = Time.realtimeSinceStartup;
		chatEntry.color = new Color(1f, 1f, 1f, 1f);
		if (!gameMessage)
		{
			chatEntry.showBubble = true;
		}
		StartCoroutine(FadeOutBubble(4, 1f, chatEntry));
		entries.Add(chatEntry);
		if (entries.Count > 7)
		{
			entries.RemoveAt(0);
		}
		scrollPosition.y = 1000000f;
	}

	private IEnumerator FadeOutBubble(int fadeAfter, float fadeTime, ChatEntry entry)
	{
		yield return new WaitForSeconds(fadeAfter);
		float endTime = Time.realtimeSinceStartup + fadeTime;
		while (Time.time < endTime)
		{
			float num = endTime - Time.realtimeSinceStartup;
			entry.color = new Color(a: num / fadeTime, r: entry.color.r, g: entry.color.g, b: entry.color.b);
			yield return 0;
		}
		entry.showBubble = false;
	}
}
