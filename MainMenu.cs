using System.Collections;
using UnityEngine;

public class MainMenu : MenuPage
{
	public static MainMenu SP;

	public GUISkin skin;

	public Texture2D backgroundImage;

	public Texture2D CLLogo;

	public AudioClip clickSound;

	private MenuPage currentMenuPage;

	private Rect windowRect;

	private string connectIP = string.Empty;

	private int connectPort;

	private static bool hasUsedConnectPars;

	private void Awake()
	{
		Resources.UnloadUnusedAssets();
		if (Application.platform == RuntimePlatform.OSXDashboardPlayer)
		{
			Application.runInBackground = false;
		}
		else
		{
			Application.runInBackground = true;
		}
		SP = this;
		currentMenuPage = GetComponent<MultiplayerMenu>();
		ShowMain();
		if (Application.isWebPlayer || Application.isEditor)
		{
			SetNewWindowHeight(320);
		}
		else
		{
			SetNewWindowHeight(360);
		}
		Screen.lockCursor = false;
		string absoluteURL = Application.absoluteURL;
		int num = absoluteURL.LastIndexOf("joinIP=");
		if (!hasUsedConnectPars && num > 0)
		{
			absoluteURL = absoluteURL.Substring(num + "joinIP=".Length);
			int num2 = absoluteURL.LastIndexOf("&joinPort=");
			if (num2 > 0)
			{
				connectIP = absoluteURL.Substring(0, num2);
				connectPort = int.Parse(absoluteURL.Substring(num2 + "&joinPort=".Length));
				hasUsedConnectPars = true;
			}
		}
		else if (PlayerPrefs.HasKey("connectIP"))
		{
			connectIP = PlayerPrefs.GetString("connectIP", "localhost");
			connectPort = PlayerPrefs.GetInt("connectPort", 25010);
			PlayerPrefs.DeleteKey("connectIP");
			PlayerPrefs.DeleteKey("connectPort");
		}
	}

	public void DoConnect(string IP, int port)
	{
		connectIP = IP;
		connectPort = port;
	}

	public void OnGUI()
	{
		if (DedicatedServer.isDedicated)
		{
			GUILayout.Label("Loading... " + Time.realtimeSinceStartup);
			return;
		}
		GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), backgroundImage);
		GUI.skin = skin;
		GUI.Label(new Rect(10f, 10f, 400f, 25f), "Cubelands Classic - modded build 2026/10");
		if (GameSettings.gameHadError != string.Empty)
		{
			GUILayout.BeginArea(new Rect(100f, 50f, Screen.width - 200, Screen.height - 100));
			windowRect = GUI.Window(21, windowRect, ShowError, string.Empty);
			GUILayout.EndArea();
			return;
		}
		if (Application.isLoadingLevel)
		{
			GUILayout.BeginArea(new Rect(100f, 50f, Screen.width - 200, Screen.height - 100));
			windowRect = GUI.Window(21, windowRect, ShowLoading, string.Empty);
			GUILayout.EndArea();
			return;
		}
		if (!AccountManager.LoggedIn())
		{
			AccountGUI.SP.ShowGUI();
			return;
		}
		if (MultiplayerFunctions.SP.ReadyLoading() && MultiplayerFunctions.SP.HasReceivedHostList() && connectIP != string.Empty && currentMenuPage != MultiplayerMenu.SP)
		{
			SwitchMainMenu(MultiplayerMenu.SP);
			MultiplayerFunctions.SP.DirectConnect(connectIP, connectPort, string.Empty, doRetryConnection: true, null);
			connectIP = string.Empty;
		}
		GUILayout.BeginArea(new Rect(100f, 50f, Screen.width - 200, Screen.height - 100));
		currentMenuPage.ShowGUI();
		GUILayout.EndArea();
	}

	public void SwitchMainMenu(MenuPage newPage)
	{
		PlayClickSound();
		if (!(currentMenuPage == newPage))
		{
			currentMenuPage.DisableMenu();
			currentMenuPage = newPage;
			currentMenuPage.EnableMenu();
		}
	}

	public void PlayClickSound()
	{
		base.audio.PlayOneShot(clickSound);
	}

	public void ShowMain()
	{
		SwitchMainMenu(this);
	}

	private void SetNewWindowHeight(int newHeight)
	{
		int num = 350;
		Vector3 vector = new Vector3(Screen.width / 2 - num / 2, Screen.height / 2 - newHeight / 2, 0f);
		windowRect = new Rect(vector.x, vector.y, num, newHeight);
	}

	public override void ShowGUI()
	{
		windowRect = GUI.Window(21, windowRect, ShowMyMenu, string.Empty);
	}

	private void ShowMyMenu(int windowID)
	{
		GUI.DrawTexture(new Rect(24f, 30f, 300f, 50f), CLLogo);
		GUILayout.Space(55f);
		GUILayout.BeginHorizontal();
		GUILayout.Label("Logged in as: " + AccountManager.GetUsername(), "Label_Header2");
		if (GUILayout.Button("Logout"))
		{
			StartCoroutine(AccountGUI.SP.DoLogout(wipeAutoLogin: false));
		}
		GUILayout.EndHorizontal();
		GUILayout.Space(20f);
		if (GUILayout.Button("Host Game"))
		{
			MultiplayerFunctions.SP.StartServer("", MultiplayerFunctions.SP.defaultServerPort, 1, enableSecurity: false);
			StartCoroutine(LoadLevel());
			return;
		}
		if (GUILayout.Button("Join Game"))
		{
			SwitchMainMenu(MultiplayerMenu.SP);
			return;
		}
		if (GUILayout.Button("Quickplay"))
		{
			MultiplayerFunctions.SP.StartServer("", MultiplayerFunctions.SP.defaultServerPort, 1, enableSecurity: false);
			StartCoroutine(LoadLevel());
			return;
		}
		GUILayout.Space(10f);
		if (AccountManager.RealAccount() && GUILayout.Button("Friends"))
		{
			SwitchMainMenu(FriendsMenu.SP);
		}
		if (GUILayout.Button("Manage worlds"))
		{
			SwitchMainMenu(WorldMenu.SP);
		}
		if (GUILayout.Button("Options"))
		{
			SwitchMainMenu(OptionsMenu.SP);
		}
		if (GUILayout.Button("Credits"))
		{
			SwitchMainMenu(CreditsMenu.SP);
		}
		if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.OSXPlayer)
		{
			GUILayout.Space(10f);
			if (GUILayout.Button("Quit game"))
			{
				Application.Quit();
			}
		}
		GUILayout.FlexibleSpace();
	}

	private void ShowError(int windowID)
	{
		GUI.DrawTexture(new Rect(24f, 30f, 300f, 50f), CLLogo);
		GUILayout.Space(55f);
		GUILayout.Label("Error ", "Label_Header2");
		GUILayout.Space(15f);
		GUILayout.Label(GameSettings.gameHadError);
		GUILayout.Space(15f);
		if (GUILayout.Button("Close"))
		{
			GameSettings.gameHadError = string.Empty;
		}
	}

	private void ShowLoading(int windowID)
	{
		GUI.DrawTexture(new Rect(24f, 30f, 300f, 50f), CLLogo);
		GUILayout.Space(55f);
		GUILayout.Label("Loading game ", "Label_Header2");
		GUILayout.Space(15f);
		GUILayout.Label("Loaded game data: " + Mathf.Floor(Application.GetStreamProgressForLevel(Application.loadedLevel + 1) * 100f) + "%");
		GUILayout.Space(10f);
		GUILayout.Label("Server title: " + GameSettings.serverTitle);
		GUILayout.Label("MOTD: " + GameSettings.motd);
		GUILayout.Space(15f);
	}

	public IEnumerator LoadLevel()
	{
		Resources.UnloadUnusedAssets();
		while (!Application.CanStreamedLevelBeLoaded(Application.loadedLevel + 1))
		{
			yield return 0;
		}
		Application.LoadLevel(Application.loadedLevel + 1);
	}
}
