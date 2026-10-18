using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiplayerMenu : MenuPage
{
	public static MultiplayerMenu SP;

	public Texture2D iconBuilderlist;

	public Texture2D iconCustomTextures;

	public Texture2D iconPassword;

	public Texture2D iconDedicated;

	private Rect windowRect;

	private Vector2 JoinScrollPosition;

	private List<CubelandsHostData> hostDataList;

	private int joinPort;

	private string joinIP;

	private string joinPW;

	private bool joinUsePW;

	private string failConnectMesage = string.Empty;

	private int lastSort = 4;

	private bool joiningRandomGame;

	private int randConnectNr;

	private NetworkConnectionError lastConnectError;

	private string hostTitle;

	private string hostMOTD;

	private string hostDescription;

	private string hostPW;

	private int hostMaxPlayers;

	private int hostPort;

	private bool hostUsePassword;

	private bool hasParsedHostListOnce;

	private bool parsingHostList;

	private void Awake()
	{
		SP = this;
		currentGUIMethod = JoinMenu;
		StartCoroutine(MyStart());
		SetNewWindowHeight(500);
	}

	public IEnumerator MyStart()
	{
		yield return 0;
		joinPort = MultiplayerFunctions.SP.defaultServerPort;
		joinIP = (joinPW = string.Empty);
		hostTitle = PlayerPrefs.GetString("hostTitle" + Application.platform, AccountManager.GetUsername() + "s server");
		hostDescription = PlayerPrefs.GetString("hostDescription" + Application.platform, "Servers description");
		hostMOTD = PlayerPrefs.GetString("hostMOTD" + Application.platform, "Servers message of the day");
		hostPW = PlayerPrefs.GetString("hostPassword" + Application.platform, string.Empty);
		hostMaxPlayers = PlayerPrefs.GetInt("hostPlayers" + Application.platform, 8);
		hostPort = PlayerPrefs.GetInt("hostPort" + Application.platform, MultiplayerFunctions.SP.defaultServerPort);
		hostDataList = new List<CubelandsHostData>();
		MultiplayerFunctions.SP.SetHostListDelegate(FullHostListReceived);
	}

	public override void EnableMenu()
	{
		MultiplayerFunctions.SP.FetchHostList();
	}

	public override void DisableMenu()
	{
		currentGUIMethod = JoinMenu;
		SP.AbortRandomConnect();
		if (MultiplayerFunctions.SP.IsConnecting())
		{
			MultiplayerFunctions.SP.CancelConnection();
		}
	}

	public override void ShowGUI()
	{
		windowRect = GUI.Window(21, windowRect, WindowGUI, string.Empty);
	}

	private void SetNewWindowHeight(int newHeight)
	{
		int num = 720;
		Vector3 vector = new Vector3(Screen.width / 2 - num / 2, Screen.height / 2 - newHeight / 2, 0f);
		windowRect = new Rect(vector.x, vector.y, num, newHeight);
	}

	public void WindowGUI(int windowID)
	{
		GUILayout.BeginHorizontal();
		GUILayout.Label("Multiplayer", "Label_Header");
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Back", GUILayout.MaxWidth(200f)))
		{
			mainMenu.ShowMain();
		}
		GUILayout.EndHorizontal();
		if (currentGUIMethod == new GUIMethod(JoinMenu))
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label("Select an option:");
			GUILayout.Space(5f);
			GUILayout.Label("Join", "DisabledButton", GUILayout.Width(75f));
			if (GUILayout.Button("Host", GUILayout.Width(75f)))
			{
				currentGUIMethod = HostMenu;
			}
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
		}
		else
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label("Select an option:");
			GUILayout.Space(5f);
			if (GUILayout.Button("Join", GUILayout.Width(75f)))
			{
				currentGUIMethod = JoinMenu;
			}
			GUILayout.Label("Host", "DisabledButton", GUILayout.Width(75f));
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
		}
		GUILayout.Space(25f);
		currentGUIMethod();
	}

	private void JoinMenu()
	{
		if (MultiplayerFunctions.SP.IsConnecting())
		{
			float num = Mathf.Round(MultiplayerFunctions.SP.TimeSinceLastConnect() * 10f) / 10f;
			string text = "Trying to connect to [" + MultiplayerFunctions.SP.ConnectingToAddress() + "]";
			if (joinPW != string.Empty)
			{
				text += " using password.";
			}
			GUILayout.Label(text);
			GUILayout.Label("Waiting: " + num);
			if (num >= 2f && GUILayout.Button("Cancel"))
			{
				MultiplayerFunctions.SP.CancelConnection();
			}
			return;
		}
		if (failConnectMesage != string.Empty)
		{
			GUILayout.Label("The game failed to connect:\n" + failConnectMesage);
			if (lastConnectError == NetworkConnectionError.InvalidPassword)
			{
				GUILayout.Label("You entered a wrong password, try again here:");
				joinIP = MultiplayerFunctions.SP.LastIP()[0];
				joinPort = MultiplayerFunctions.SP.LastPort();
				GUILayout.BeginHorizontal();
				GUILayout.Space(5f);
				GUILayout.Label("IP");
				GUILayout.Label(joinIP, GUILayout.Width(100f));
				GUILayout.Label("Port");
				GUILayout.Label(joinPort + string.Empty, GUILayout.Width(50f));
				GUILayout.Label("Password");
				joinPW = GUILayout.TextField(joinPW, GUILayout.Width(100f));
				if (GUILayout.Button("Connect"))
				{
					MultiplayerFunctions.SP.DirectConnect(joinIP, joinPort, joinPW, doRetryConnection: true, OnFinalFailedToConnect);
				}
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
			}
			GUILayout.Space(10f);
			if (GUILayout.Button("Cancel"))
			{
				failConnectMesage = string.Empty;
			}
			return;
		}
		if (joiningRandomGame)
		{
			GUILayout.Label("Trying to connect to first possible game...", "Label_Header2");
			if (GUILayout.Button("Cancel"))
			{
				joiningRandomGame = false;
				MultiplayerFunctions.SP.CancelConnection();
			}
			return;
		}
		GUILayout.BeginHorizontal();
		GUILayout.Label("Game list:", "Label_Header2");
		GUILayout.FlexibleSpace();
		if (hostDataList.Count > 0 && GUILayout.Button("Join random game"))
		{
			StartCoroutine(StartJoinRandom(4));
		}
		if (GUILayout.Button("Refresh list"))
		{
			MultiplayerFunctions.SP.FetchHostList();
		}
		GUILayout.EndHorizontal();
		GUILayout.Space(2f);
		GUILayout.BeginHorizontal();
		GUILayout.Space(24f);
		if (GUILayout.Button("Title", "DisabledButton", GUILayout.Width(200f)))
		{
			SetSorting(1);
		}
		if (GUILayout.Button("Players", "DisabledButton", GUILayout.Width(55f)))
		{
			SetSorting(2);
		}
		if (GUILayout.Button("IP", "DisabledButton", GUILayout.Width(150f)))
		{
			SetSorting(3);
		}
		if (GUILayout.Button("Dedicated", "DisabledButton", GUILayout.Width(70f)))
		{
			SetSorting(4);
		}
		if (GUILayout.Button("Custom", "DisabledButton", GUILayout.Width(70f)))
		{
			SetSorting(5);
		}
		if (GUILayout.Button("Builderlist", "DisabledButton", GUILayout.Width(70f)))
		{
			SetSorting(6);
		}
		GUILayout.EndHorizontal();
		JoinScrollPosition = GUILayout.BeginScrollView(JoinScrollPosition);
		int num2 = 0;
		foreach (CubelandsHostData hostData in hostDataList)
		{
			if (hostData.gameVersion != Utils.GetGameVersion())
			{
				num2++;
				continue;
			}
			GUILayout.BeginHorizontal();
			if (hostData.passwordProtected)
			{
				GUILayout.Label(iconPassword, GUILayout.MaxWidth(16f));
			}
			else
			{
				GUILayout.Space(24f);
			}
			if (GUILayout.Button(string.Empty + hostData.title, GUILayout.Width(200f)))
			{
				MultiplayerFunctions.SP.HostDataConnect(hostData.realHostData, string.Empty, doRetryConnection: true, OnFinalFailedToConnect);
			}
			GUILayout.Label(hostData.connectedPlayers + "/" + hostData.maxPlayers, GUILayout.Width(55f));
			GUILayout.Label(hostData.IP[0] + ":" + hostData.port, GUILayout.Width(150f));
			GUILayout.Space(27f);
			if (hostData.isDedicated)
			{
				GUILayout.Label(iconDedicated, GUILayout.Width(70f));
			}
			else
			{
				GUILayout.Space(70f);
			}
			if (hostData.useCustomTextures)
			{
				GUILayout.Label(iconCustomTextures, GUILayout.Width(70f));
			}
			else
			{
				GUILayout.Space(70f);
			}
			if (hostData.useBuilderList)
			{
				GUILayout.Label(iconBuilderlist, GUILayout.Width(16f));
			}
			else
			{
				GUILayout.Space(16f);
			}
			GUILayout.EndHorizontal();
		}
		GUILayout.EndScrollView();
		string text2 = hostDataList.Count + " total servers";
		if (num2 > 0)
		{
			text2 = text2 + ", " + num2 + " servers hidden because of a different game version.";
		}
		GUILayout.Label(text2);
		GUILayout.BeginHorizontal();
		GUILayout.Label("Direct join:", "Label_Header2");
		GUILayout.Space(5f);
		GUILayout.Label("IP");
		joinIP = GUILayout.TextField(joinIP, GUILayout.Width(100f));
		GUILayout.Label("Port");
		joinPort = int.Parse(GUILayout.TextField(joinPort + string.Empty, GUILayout.Width(50f)) + string.Empty);
		GUILayout.Label("Password");
		joinUsePW = GUILayout.Toggle(joinUsePW, string.Empty, GUILayout.MaxWidth(22f));
		if (joinUsePW)
		{
			joinPW = GUILayout.TextField(joinPW, GUILayout.Width(100f));
		}
		if (GUILayout.Button("Connect"))
		{
			MultiplayerFunctions.SP.DirectConnect(joinIP, joinPort, joinPW, doRetryConnection: true, OnFinalFailedToConnect);
		}
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		GUILayout.Space(4f);
	}

	public void SetSorting(int newSort)
	{
		if (newSort != lastSort)
		{
			hostDataList.Sort(new CubelandsHostDataSorter(newSort));
		}
		lastSort = newSort;
	}

	public IEnumerator StartJoinRandom(int sorting)
	{
		if (joiningRandomGame)
		{
			yield break;
		}
		joiningRandomGame = true;
		SetSorting(sorting);
		while (joiningRandomGame && (!hasParsedHostListOnce || !MultiplayerFunctions.SP.ReadyLoading() || !MultiplayerFunctions.SP.HasReceivedHostList()))
		{
			yield return 0;
		}
		if (joiningRandomGame)
		{
			randConnectNr = 1;
			foreach (CubelandsHostData hostData in hostDataList)
			{
				if ((AccountManager.DidTutorial() || !hostData.useBuilderList) && hostData.gameVersion == Utils.GetGameVersion())
				{
					MultiplayerFunctions.SP.HostDataConnect(hostData.realHostData, string.Empty, doRetryConnection: true, OnFinalFailedToConnect);
					yield return new WaitForSeconds(2f);
					if (Network.isClient || !joiningRandomGame)
					{
						break;
					}
					randConnectNr++;
				}
			}
		}
		joiningRandomGame = false;
	}

	public void AbortRandomConnect()
	{
		joiningRandomGame = false;
	}

	public bool IsDoingRandomConnect()
	{
		return joiningRandomGame;
	}

	public string RandConnectNr()
	{
		return randConnectNr + "/" + hostDataList.Count;
	}

	private void OnConnectedToServer()
	{
		Network.isMessageQueueRunning = false;
		GameSettings.Clear();
		StartCoroutine(mainMenu.LoadLevel());
	}

	private void OnFinalFailedToConnect()
	{
		lastConnectError = MultiplayerFunctions.SP.LastConnectionError();
		failConnectMesage = string.Concat(failConnectMesage, "Attempting to connect to [", MultiplayerFunctions.SP.LastIP()[0], ":", MultiplayerFunctions.SP.LastPort(), "]: ", lastConnectError, "\n");
		Debug.Log("OnFinalFailedToConnect=" + failConnectMesage);
	}

	private void HostMenu()
	{
		GUILayout.BeginHorizontal();
		GUILayout.Label("Host a new game:");
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label("Title:");
		GUILayout.FlexibleSpace();
		hostTitle = GUILayout.TextField(hostTitle, GUILayout.Width(200f));
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label("Server description");
		GUILayout.FlexibleSpace();
		hostDescription = GUILayout.TextField(hostDescription, GUILayout.Width(200f));
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label("MOTD");
		GUILayout.FlexibleSpace();
		hostMOTD = GUILayout.TextField(hostMOTD, GUILayout.Width(200f));
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label("Server password ", GUILayout.Width(200f), GUILayout.Height(23f));
		GUILayout.FlexibleSpace();
		hostUsePassword = GUILayout.Toggle(hostUsePassword, string.Empty, GUILayout.MaxWidth(40f));
		if (hostUsePassword)
		{
			hostPW = GUILayout.TextField(hostPW, GUILayout.Width(200f));
		}
		else
		{
			GUILayout.Label(string.Empty, "DisabledButton", GUILayout.Height(23f), GUILayout.Width(200f));
		}
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label("Max players (1-32)");
		GUILayout.FlexibleSpace();
		hostMaxPlayers = int.Parse(GUILayout.TextField(hostMaxPlayers + string.Empty, GUILayout.Width(50f)) + string.Empty);
		GUILayout.EndHorizontal();
		CheckHostVars();
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Start server", GUILayout.Width(150f)))
		{
			GameSettings.Clear();
			StartHostingGame(hostMOTD, hostDescription, hostTitle, hostPort, hostUsePassword, hostPW, hostMaxPlayers, GameSettings.mapname, 64, 128, 128, 30);
		}
		GUILayout.EndHorizontal();
	}

	private void CheckHostVars()
	{
		hostMaxPlayers = Mathf.Clamp(hostMaxPlayers, 1, 64);
		hostPort = Mathf.Clamp(hostPort, 10000, 100000);
		hostTitle = Utils.CapString(hostTitle, 50);
		hostMOTD = Utils.CapString(hostMOTD, 150);
		hostDescription = Utils.CapString(hostDescription, 100);
		hostPW = Utils.CapString(hostPW, 50);
	}

	public void StartHostingGameDefaultSettings()
	{
		StartHostingGame(hostMOTD, hostDescription, hostTitle, hostPort, usePassword: false, string.Empty, 8, GameSettings.mapname, 64, 128, 128, 30);
	}

	public void StartHostingGame(string motd, string description, string title, int port, bool usePassword, string password, int maxplayers, string mapName, int emptyMapHeight, int emptymapx, int emptymapz, int worldFillInHeight)
	{
		if (Network.isServer)
		{
			Network.Disconnect();
		}
		maxplayers = Mathf.Clamp(maxplayers, 0, 64);
		port = Mathf.Clamp(port, 10000, 100000);
		title = Utils.CapString(title, 50);
		motd = Utils.CapString(motd, 250);
		description = Utils.CapString(description, 100);
		password = Utils.CapString(password, 50);
		if (title == "Guests server")
		{
			title = AccountManager.GetUsername() + "s server";
		}
		GameSettings.motd = motd;
		GameSettings.description = description;
		GameSettings.serverTitle = title;
		GameSettings.port = port;
		GameSettings.IP = "localhost";
		GameSettings.players = maxplayers;
		GameSettings.mapname = mapName;
		GameSettings.emptyMapHeight = emptyMapHeight;
		GameSettings.emptyMapX = emptymapx;
		GameSettings.emptyMapZ = emptymapz;
		GameSettings.emptyMapFillInHeight = worldFillInHeight;
		PlayerPrefs.SetString("hostTitle" + Application.platform, title);
		PlayerPrefs.SetString("hostDescription" + Application.platform, description);
		PlayerPrefs.SetString("hostMOTD" + Application.platform, motd);
		PlayerPrefs.SetString("hostPassword" + Application.platform, password);
		PlayerPrefs.SetInt("hostPlayers" + Application.platform, maxplayers);
		PlayerPrefs.SetInt("hostPort" + Application.platform, port);
		if (usePassword && password != string.Empty)
		{
			GameSettings.password = password;
			Network.incomingPassword = password;
		}
		if (!DedicatedServer.isDedicated)
		{
			maxplayers--;
		}
		Network.InitializeSecurity();
		Network.InitializeServer(maxplayers, port, !Network.HavePublicAddress());
		Debug.Log("InitializeServer " + maxplayers + " port=" + port + "nat=" + !Network.HavePublicAddress());
		StartCoroutine(mainMenu.LoadLevel());
	}

	public void FullHostListReceived()
	{
		StartCoroutine(ReloadHosts());
	}

	private IEnumerator ReloadHosts()
	{
		if (parsingHostList)
		{
			yield break;
		}
		parsingHostList = true;
		HostData[] hostData = MultiplayerFunctions.SP.GetHostData();
		for (int num = -1; num != hostData.Length; num = hostData.Length)
		{
			yield return new WaitForSeconds(0.5f);
			hostData = MultiplayerFunctions.SP.GetHostData();
		}
		hostDataList.Clear();
		HostData[] array = hostData;
		HostData[] array2 = array;
		foreach (HostData hostData2 in array2)
		{
			CubelandsHostData cubelandsHostData = new CubelandsHostData
			{
				realHostData = hostData2,
				connectedPlayers = hostData2.connectedPlayers,
				IP = hostData2.ip,
				port = hostData2.port,
				maxPlayers = hostData2.playerLimit,
				passwordProtected = hostData2.passwordProtected,
				title = hostData2.gameName,
				useNAT = hostData2.useNat
			};
			string[] array3 = hostData2.comment.Split('#');
			cubelandsHostData.gameVersion = int.Parse(array3[0]);
			cubelandsHostData.isDedicated = array3[1] == "1";
			if (array3.Length >= 3 && array3[2] == "1")
			{
				cubelandsHostData.useBuilderList = true;
			}
			if (array3.Length >= 4 && array3[3] == "1")
			{
				cubelandsHostData.useCustomTextures = true;
			}
			if (cubelandsHostData.isDedicated)
			{
				cubelandsHostData.connectedPlayers--;
				cubelandsHostData.maxPlayers--;
			}
			hostDataList.Add(cubelandsHostData);
			hostDataList.Sort(new CubelandsHostDataSorter(lastSort));
			if (hostDataList.Count % 3 == 0)
			{
				yield return 0;
			}
		}
		parsingHostList = false;
		hasParsedHostListOnce = true;
	}
}
