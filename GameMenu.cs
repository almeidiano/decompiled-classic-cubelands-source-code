using System.Collections;
using UnityEngine;

public class GameMenu : MonoBehaviour
{
	public static GameMenu SP;

	public GUISkin skin;

	private MenuStates menuState;

	public Texture2D screenshotButton;

	private Inventory inventory;

	public Transform musicPrefab;

	public RenderTexture currentInventoryRender;

	private float realTimeLevelLoaded;

	private string wwwURL = "http://gamedata.cubelands.com/accounts.php";

	private string loadingStatus = string.Empty;

	private Texture2D screenShot;

	private byte[] screenShotBytes;

	private int currentScreenshot;

	private int lastUploadedScreenshot = -1;

	private bool hideGUIForScreenshot;

	private float lastScreenShotTime;

	private Rect windowRect;

	private string screenshotTitle = string.Empty;

	private string screenshotDesc = string.Empty;

	private string screenshotUploadStatus = string.Empty;

	private bool uploadingScreenshot;

	private Vector2 scrollPos = Vector2.zero;

	private string optionTooltip = string.Empty;

	public float updateInterval = 0.5f;

	private float accum;

	private int frames;

	private float timeleft;

	private float currentFPS;

	private int tutorialStep = -1;

	public AudioClip tutorialStepComplete;

	public Texture2D trophyImage;

	private string skinChangerURL = "";

	private Texture2D skinChangerTexture;

	private float skinChangerPreviewSize = 180f;

	private void Awake()
	{
		realTimeLevelLoaded = Time.realtimeSinceStartup;
		SP = this;
		if (!DedicatedServer.isDedicated)
		{
			Screen.lockCursor = true;
		}
		inventory = GetComponent<Inventory>();
		Object.Instantiate(musicPrefab);
	}

	private void Start()
	{
		Options.LoadOptions();
		if (PlayerPrefs.GetInt("OptimizeFirstRun" + Application.platform, 1) == 1)
		{
			StartCoroutine(OptimizeGame());
		}
		if (!AccountManager.DidTutorial())
		{
			StartTutorial();
		}
	}

	private string GetLoadingStatus()
	{
		return loadingStatus;
	}

	public void SetLoadingStatus(string newS)
	{
		loadingStatus = newS;
	}

	private IEnumerator OptimizeGame()
	{
		if (!DedicatedServer.isDedicated)
		{
			while (!GameManager.SP.GetFinishedLoading())
			{
				yield return 0;
			}
			float startTime = Time.realtimeSinceStartup;
			while (Time.realtimeSinceStartup - 6f < startTime)
			{
				float lastOptimize = Time.realtimeSinceStartup;
				int lastOptimizeFrameCount = Time.frameCount;
				yield return new WaitForSeconds(0.5f);
				float num = Time.realtimeSinceStartup - lastOptimize;
				Options.OptimizeGame((int)((float)(Time.frameCount - lastOptimizeFrameCount) / num));
			}
			PlayerPrefs.SetInt("OptimizeFirstRun" + Application.platform, 0);
		}
	}

	private void Update()
	{
		if (DedicatedServer.isDedicated)
		{
			return;
		}
		if ((Input.GetKeyDown(KeyCode.Escape) || (Input.GetButtonDown("Show menu") && (menuState == MenuStates.Inventory || menuState == MenuStates.main || menuState == MenuStates.hide))) && !Chat.IsUsing() && (!Utils.IsWebplayer() || Screen.lockCursor))
		{
			Screen.lockCursor = !Screen.lockCursor;
		}
		if (Screen.lockCursor && menuState != 0)
		{
			SwitchMenu(MenuStates.hide);
		}
		else if (!Screen.lockCursor && menuState == MenuStates.hide)
		{
			SwitchMenu(MenuStates.main);
		}
		else if (!Chat.IsUsing() && (menuState == MenuStates.Inventory || menuState == MenuStates.hide) && Input.GetButtonDown("Inventory"))
		{
			if (menuState == MenuStates.Inventory)
			{
				SwitchMenu(MenuStates.hide);
			}
			else
			{
				SwitchMenu(MenuStates.Inventory);
			}
		}
		else if (AccountManager.RealAccount() && Input.GetButtonDown("Screenshot - No GUI"))
		{
			StartCoroutine(CreateScreenshot(withGUI: false));
		}
		else if (AccountManager.RealAccount() && Input.GetButtonDown("Screenshot - With GUI"))
		{
			StartCoroutine(CreateScreenshot(withGUI: true));
		}
		else if ((menuState == MenuStates.hide || menuState == MenuStates.ServerInfo) && Input.GetKeyDown(KeyCode.Tab))
		{
			if (menuState == MenuStates.ServerInfo)
			{
				SwitchMenu(MenuStates.hide);
			}
			else
			{
				SwitchMenu(MenuStates.ServerInfo);
			}
		}
		if (Options.showFPS)
		{
			RunFPSStuff();
		}
	}

	private IEnumerator CreateScreenshot(bool withGUI)
	{
		if (lastScreenShotTime > Time.time - 0.5f)
		{
			yield break;
		}
		Debug.Log("CreateScreenshot screenshot  GUI=" + withGUI);
		lastScreenShotTime = Time.time;
		GUILayer guiLayer = Camera.main.GetComponent<GUILayer>();
		Camera blokCam = null;
		if (Camera.allCameras.Length >= 2)
		{
			blokCam = Camera.allCameras[1];
		}
		if (!withGUI)
		{
			Chat.SP.enabled = false;
			guiLayer.enabled = withGUI;
			SwitchMenu(MenuStates.hide);
			hideGUIForScreenshot = true;
			if ((bool)blokCam)
			{
				blokCam.enabled = false;
			}
		}
		yield return new WaitForEndOfFrame();
		int width = Screen.width;
		int height = Screen.height;
		Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, mipmap: false);
		tex.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
		if (!withGUI)
		{
			hideGUIForScreenshot = false;
			Chat.SP.enabled = true;
			guiLayer.enabled = true;
			if ((bool)blokCam)
			{
				blokCam.enabled = true;
			}
		}
		tex.Apply();
		GameManager.SP.localPlayerNode.transform.networkView.RPC("PlayCameraSound", RPCMode.All);
		yield return 0;
		JPGEncoder encoder = new JPGEncoder(tex, 95f);
		while (!encoder.isDone)
		{
			yield return 0;
		}
		screenShot = tex;
		screenShotBytes = encoder.GetBytes();
		currentScreenshot++;
		SwitchMenu(MenuStates.UploadScreenshot);
	}

	private IEnumerator UploadScreenshot(string title, string description)
	{
		screenshotUploadStatus = "Starting upload.";
		if (lastUploadedScreenshot == currentScreenshot)
		{
			screenshotUploadStatus = "You have already uploaded this screenshot.";
			yield break;
		}
		uploadingScreenshot = true;
		byte[] contents = screenShotBytes;
		WWWForm wWWForm = new WWWForm();
		wWWForm.AddField("userID", AccountManager.GetUserID());
		wWWForm.AddField("action", "screenshot");
		wWWForm.AddField("title", title);
		wWWForm.AddField("desc", description);
		if (GetTutorialStep() == TutorialSteps.Screenshot)
		{
			wWWForm.AddField("istutshot", 1);
		}
		wWWForm.AddField("key", Utils.Md5Sum(AccountManager.GetUserID() + "scr88nK1y"));
		wWWForm.AddField("sessionKey", AccountManager.GetSessionKey());
		wWWForm.AddField("serverID", GameManager.SP.GetServerID());
		wWWForm.AddBinaryData("fileUpload", contents, "screenShot.jpeg", "image/jpeg");
		WWW w = new WWW(wwwURL, wWWForm);
		while (!w.isDone)
		{
			screenshotUploadStatus = "Uploading: " + (int)(w.uploadProgress * 100f) + "%";
			yield return 0;
		}
		Debug.Log(w.text);
		if (GetTutorialStep() == TutorialSteps.Screenshot)
		{
			FinishedTutorialStep();
		}
		if (w.text == string.Empty || w.error != null)
		{
			Debug.Log(w.error);
			screenshotUploadStatus = "Error uploading: " + w.error;
		}
		else
		{
			Debug.Log(w.text);
			switch (int.Parse(w.text + string.Empty))
			{
			case 0:
				screenshotUploadStatus = "You're not logged in any longer!";
				GameSettings.gameHadError = "Your account has logged in from another location.";
				StartCoroutine(GameManager.SP.QuitGame());
				break;
			case 1:
				lastUploadedScreenshot = currentScreenshot;
				screenshotUploadStatus = "Your screenshot has been uploaded!";
				break;
			case 2:
				screenshotUploadStatus = "Sorry, you've reached your max. upload limit, please buy the game or delete old screenshots.";
				break;
			case 3:
				screenshotUploadStatus = "Sorry, authentication error.";
				break;
			default:
				screenshotUploadStatus = "Unknown error, please try again later (" + w.text + ").";
				break;
			}
		}
		uploadingScreenshot = false;
	}

	public void SwitchMenu(MenuStates newState)
	{
		if (newState == menuState)
		{
			Debug.LogError("SwitchMenu, already: " + newState);
			return;
		}
		menuState = newState;
		if (newState == MenuStates.hide)
		{
			Screen.lockCursor = true;
			return;
		}
		Screen.lockCursor = false;
		switch (newState)
		{
		case MenuStates.main:
			SetNewWindowHeight(270, 250);
			break;
		case MenuStates.options:
			SetNewWindowHeight(550, 500);
			break;
		case MenuStates.help:
			if (GetTutorialStep() == TutorialSteps.Help)
			{
				FinishedTutorialStep();
				if (!AccountManager.RealAccount())
				{
					FinishedTutorialStep();
				}
			}
			SetNewWindowHeight(475, 425);
			break;
		case MenuStates.Inventory:
			if (GetTutorialStep() == TutorialSteps.Inventory)
			{
				FinishedTutorialStep();
			}
			SetNewWindowHeight(260, 410);
			break;
		case MenuStates.ServerInfo:
			SetNewWindowHeight(475, 600);
			break;
		case MenuStates.Friends:
			SetNewWindowHeight(400, 400);
			break;
		case MenuStates.UploadScreenshot:
			SetNewWindowHeight(525, 525);
			break;
		case MenuStates.SkinChanger:
			SetNewWindowHeight(400, 400);
			break;
		}
	}

	private void SetNewWindowHeight(int newHeight, int newwidth)
	{
		Vector3 vector = new Vector3(Screen.width / 2 - newwidth / 2, Screen.height / 2 - newHeight / 2, 0f);
		windowRect = new Rect(vector.x, vector.y, newwidth, newHeight);
	}

	private void OnGUI()
	{
		if (DedicatedServer.isDedicated)
		{
			GUILayout.Label("Uptime : " + Mathf.Floor(Time.realtimeSinceStartup / 3600f) + ":" + Mathf.Floor(Time.realtimeSinceStartup / 60f) + ":" + Time.realtimeSinceStartup % 60f);
			GUILayout.Label("Connections: " + Network.connections.Length);
			if (GUILayout.Button("Save"))
			{
				StartCoroutine(WorldData.SP.SaveWorld(rightAway: true));
			}
			if (GUILayout.Button("Quit - SAVE NOT GARUANTEED"))
			{
				Application.Quit();
			}
		}
		else
		{
			if (DedicatedServer.isDedicated || hideGUIForScreenshot)
			{
				return;
			}
			GUI.skin = skin;
			if (!GameManager.SP.GetFinishedLoading())
			{
				GUILayout.BeginArea(new Rect(10f, 10f, Screen.width, 25f));
				GUILayout.BeginHorizontal();
				Chat.DrawOutline(GetLoadingStatus() + " " + (int)(Time.realtimeSinceStartup - realTimeLevelLoaded) + "...");
				GUILayout.EndHorizontal();
				GUILayout.EndArea();
			}
			else if (GameManager.SP.IsQuitting())
			{
				GUILayout.BeginArea(new Rect(10f, 10f, Screen.width, 25f));
				GUILayout.BeginHorizontal();
				Chat.DrawOutline("Quitting game and saving world...");
				GUILayout.EndHorizontal();
				GUILayout.EndArea();
				return;
			}
			if (DoingTutorial())
			{
				GUI.Window(22, new Rect(10f, 10f, 350f, 200f), ShowTutorial, string.Empty);
			}
			if (menuState == MenuStates.main)
			{
				windowRect = GUI.Window(21, windowRect, ShowMenu, string.Empty);
			}
			else if (menuState == MenuStates.options)
			{
				windowRect = GUI.Window(21, windowRect, ShowOptions, string.Empty);
			}
			else if (menuState == MenuStates.help)
			{
				windowRect = GUI.Window(21, windowRect, ShowHelp, string.Empty);
			}
			else if (menuState == MenuStates.UploadScreenshot)
			{
				windowRect = GUI.Window(21, windowRect, ShowScreenshotUpload, string.Empty);
			}
			else if (menuState == MenuStates.ServerInfo)
			{
				windowRect = GUI.Window(21, windowRect, ShowServerInfo, string.Empty);
			}
			else if (menuState == MenuStates.Friends)
			{
				windowRect = GUI.Window(21, windowRect, ShowFriends, string.Empty);
			}
			else if (menuState == MenuStates.Inventory)
			{
				windowRect = GUI.Window(21, windowRect, inventory.ShowInventory, string.Empty);
			}
			else if (menuState == MenuStates.SkinChanger)
			{
				windowRect = GUI.Window(21, windowRect, ShowSkinChanger, string.Empty);
			}
			if (menuState != 0 && !Chat.IsUsing())
			{
				GUI.FocusWindow(21);
			}
			if (Options.showFPS)
			{
				ShowFPSGUI();
			}
			foreach (PlayerNode player in GameManager.SP.GetPlayerList())
			{
				if (!player.isLocal && player.transform != null)
				{
					Vector3 position = player.transform.position;
					position += new Vector3(0f, 1f, 0f);
					Vector3 vector = Camera.main.WorldToScreenPoint(position);
					if (vector.z > 0f && Vector3.Distance(Camera.main.transform.position, player.transform.position) <= 450f)
					{
						GUI.Label(new Rect(vector.x - 45f, (float)Screen.height - vector.y - 10f, 90f, 20f), player.name, "LabelCentered");
					}
				}
			}
		}
	}

	private void ShowMenu(int windowID)
	{
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		GUILayout.Label("Game menu", "Label_Header");
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		GUILayout.Space(10f);
		if (GUILayout.Button("Options"))
		{
			SwitchMenu(MenuStates.options);
		}
		if (GUILayout.Button("Server info"))
		{
			SwitchMenu(MenuStates.ServerInfo);
		}
		if (AccountManager.RealAccount() && GUILayout.Button("Friends"))
		{
			SwitchMenu(MenuStates.Friends);
		}
		if (GUILayout.Button("Help"))
		{
			SwitchMenu(MenuStates.help);
		}
		if (GUILayout.Button("Return to game"))
		{
			SwitchMenu(MenuStates.hide);
		}
		GUILayout.Space(20f);
		if (GUILayout.Button("Exit game"))
		{
			StartCoroutine(GameManager.SP.QuitGame());
		}
	}

	private void ShowScreenshotUpload(int windowID)
	{
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Close", GUILayout.MaxWidth(150f)))
		{
			SwitchMenu(MenuStates.hide);
		}
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		GUILayout.Label("Upload screenshot", "Label_Header");
		if (screenshotUploadStatus != string.Empty)
		{
			GUILayout.Label("Upload status: " + screenshotUploadStatus);
		}
		if (!uploadingScreenshot && lastUploadedScreenshot != currentScreenshot)
		{
			GUILayout.Label("You can upload this screenshot to your online profile.\n");
			GUILayout.BeginHorizontal();
			GUILayout.Label("Title", GUILayout.Width(130f));
			screenshotTitle = GUILayout.TextField(screenshotTitle);
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			GUILayout.Label("Description", GUILayout.Width(130f));
			screenshotDesc = GUILayout.TextField(screenshotDesc);
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			GUILayout.Space(130f);
			if (screenshotTitle.Length >= 1)
			{
				if (GUILayout.Button("Upload"))
				{
					StartCoroutine(UploadScreenshot(screenshotTitle, screenshotDesc));
				}
			}
			else
			{
				GUILayout.Label("Please enter a title and description.");
			}
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
		}
		GUILayout.Label("Preview", "Label_Header");
		GUI.DrawTexture(new Rect(40f, 230f, 450f, 270f), screenShot, ScaleMode.ScaleToFit);
	}

	private void ShowFriends(int windowID)
	{
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Close", GUILayout.MaxWidth(150f)))
		{
			SwitchMenu(MenuStates.hide);
		}
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		FriendsMenu.FriendContent();
	}

	private void ShowServerInfo(int windowID)
	{
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Close", GUILayout.MaxWidth(150f)))
		{
			SwitchMenu(MenuStates.hide);
		}
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		GUILayout.Label("Server info", "Label_Header");
		bool isAdmin = GameManager.SP.localPlayerNode.isAdmin;
		GUILayout.BeginHorizontal();
		GUILayout.Label("Connection information:", GUILayout.Width(150f));
		GUILayout.Label(GameSettings.IP + ":" + GameSettings.port);
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label("Title:", GUILayout.Width(150f));
		if (isAdmin)
		{
			GameSettings.serverTitle = GUILayout.TextField(GameSettings.serverTitle, GUILayout.MaxWidth(400f));
		}
		else
		{
			GUILayout.Label(GameSettings.serverTitle);
		}
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label("Description:", GUILayout.Width(150f));
		if (isAdmin)
		{
			GameSettings.description = GUILayout.TextField(GameSettings.description, GUILayout.MaxWidth(400f));
		}
		else
		{
			GUILayout.Label(GameSettings.description);
		}
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label("Message of the day:", GUILayout.Width(150f));
		if (isAdmin)
		{
			GameSettings.motd = GUILayout.TextField(GameSettings.motd, GUILayout.MaxWidth(400f));
		}
		else
		{
			GUILayout.Label(GameSettings.motd);
		}
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label("Builderlist:", GUILayout.Width(150f));
		bool useBuilderList = GameSettings.useBuilderList;
		if (isAdmin)
		{
			GameSettings.useBuilderList = GUILayout.Toggle(GameSettings.useBuilderList, string.Empty, GUILayout.MaxWidth(400f));
		}
		else
		{
			GUILayout.Label((!GameSettings.useBuilderList) ? "Disabled" : "Enabled");
		}
		GUILayout.EndHorizontal();
		if (isAdmin)
		{
			bool flag = false;
			if (useBuilderList != GameSettings.useBuilderList)
			{
				flag = true;
			}
			GameSettings.serverTitle = Utils.CapString(GameSettings.serverTitle, 50);
			GameSettings.motd = Utils.CapString(GameSettings.motd, 150);
			GameSettings.description = Utils.CapString(GameSettings.description, 100);
			GUILayout.BeginHorizontal();
			GUILayout.Space(150f);
			if (flag)
			{
				GameManager.SP.SendGameServerData(RPCMode.All);
			}
			if (GUILayout.Button("Change"))
			{
				GameManager.SP.SendGameServerData(RPCMode.All);
				Chat.SP.addGameChatMessage("New MOTD by " + GameManager.SP.localPlayerNode.name + ": " + GameSettings.motd);
			}
			GUILayout.EndHorizontal();
		}
		GUILayout.Label("Playerlist", "Label_Header");
		scrollPos = GUILayout.BeginScrollView(scrollPos);
		foreach (PlayerNode player in GameManager.SP.GetPlayerList())
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label(player.name, GUILayout.Width(150f));
			GUILayout.Label((!player.isAdmin) ? string.Empty : "Admin", GUILayout.Width(50f));
			GUILayout.Label((!player.isMod) ? string.Empty : "Mod", GUILayout.Width(50f));
			GUILayout.Label((!GameSettings.useBuilderList || !player.isBuilder) ? string.Empty : "Builder", GUILayout.Width(50f));
			GUILayout.Label((!player.boughtGame) ? string.Empty : "premium player");
			if (GameManager.IsMod(GameManager.SP.localPlayerNode) && player.networkPlayer != Network.player)
			{
				if (player.isAdmin)
				{
					GUILayout.Width(100f);
				}
				else
				{
					if (GUILayout.Button("Kick", GUILayout.Width(50f)))
					{
						Statistics.AddStat(Stats.kickedUsers, 1);
						GameManager.SP.RequestKick(player.networkPlayer);
					}
					if (GUILayout.Button("Ban", GUILayout.Width(50f)))
					{
						Statistics.AddStat(Stats.bannedUsers, 1);
						GameManager.SP.Ban(player.networkPlayer);
					}
				}
			}
			if (GameSettings.useBuilderList && GameManager.IsMod(GameManager.SP.localPlayerNode))
			{
				player.isBuilder = GUILayout.Toggle(player.isBuilder, "Builder");
				if (GUI.changed)
				{
					if (player.isBuilder)
					{
						GameSettings.builderList.Add(player.name);
					}
					else
					{
						GameSettings.builderList.Remove(player.name);
					}
					GameManager.SP.SendGameServerData(RPCMode.All);
				}
			}
			if (GameManager.IsAdmin(GameManager.SP.localPlayerNode) && Network.player != player.networkPlayer)
			{
				player.isMod = player.isAdmin | GUILayout.Toggle(player.isMod, "Mod");
				if (GUI.changed)
				{
					if (player.isMod)
					{
						GameSettings.modList.Add(player.name);
					}
					else
					{
						GameSettings.modList.Remove(player.name);
					}
					GameManager.SP.SendGameServerData(RPCMode.All);
				}
				player.isAdmin = GUILayout.Toggle(player.isAdmin, "Admin");
				if (GUI.changed)
				{
					if (player.isAdmin)
					{
						GameSettings.adminList.Add(player.name);
					}
					else
					{
						GameSettings.adminList.Remove(player.name);
					}
					GameManager.SP.SendGameServerData(RPCMode.All);
				}
			}
			GUILayout.EndHorizontal();
		}
		GUILayout.EndScrollView();
	}

	private void ShowHelp(int windowID)
	{
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Close", GUILayout.MaxWidth(150f)))
		{
			SwitchMenu(MenuStates.hide);
		}
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		GUILayout.Label("Help", "Label_Header");
		GUILayout.Label("Build whatever you want and show off on Cubelands.com!");
		GUILayout.Space(5f);
		GUILayout.Label("Move around", "Label_Header2");
		GUILayout.Label("Use WASD or the arrow keys to move\nUse the mouse to look around");
		GUILayout.Space(5f);
		GUILayout.Label("Chat", "Label_Header2");
		GUILayout.Label("Press Enter to chat");
		GUILayout.Space(5f);
		GUILayout.Label("Build/remove", "Label_Header2");
		GUILayout.BeginHorizontal();
		GUILayout.Label("Left mouse button / Control :", GUILayout.Width(130f));
		GUILayout.Label("Build selected block");
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label("Right mouse button / Alt:", GUILayout.Width(130f));
		GUILayout.Label("Remove highlighted block");
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label("Middle mouse button / C:", GUILayout.Width(130f));
		GUILayout.Label("Select the highlighted block");
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label("Scrollwheel or +/- keys:", GUILayout.Width(130f));
		GUILayout.Label("Select building block");
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label("0-9 keys:", GUILayout.Width(130f));
		GUILayout.Label("Select the first 10 building blocks");
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label("'I' key", GUILayout.Width(130f));
		GUILayout.Label("Open the inventory (allows you to rearrange it)");
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label("F1 and F2", GUILayout.Width(130f));
		GUILayout.Label("Create screenshots (with or without GUI)");
		GUILayout.EndHorizontal();
	}

	private void ShowOptions(int windowID)
	{
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Back", GUILayout.MaxWidth(150f)))
		{
			SwitchMenu(MenuStates.main);
		}
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		GUILayout.Label("Options", "Label_Header");
		GUILayout.Label(optionTooltip);
		Options.ShowGUI();
	}

	private void ShowSkinChanger(int windowID)
	{
		GUILayout.Label("Skin Changer", "Label_Header");
		GUILayout.Label("Skin URL:");
		skinChangerURL = GUILayout.TextField(skinChangerURL);
		if (skinChangerURL.Length >= 1 && GUILayout.Button("Download Skin"))
		{
			StartCoroutine(SkinChangerDownload());
		}
		if (skinChangerTexture != null)
		{
			GUI.DrawTexture(new Rect(200f - skinChangerPreviewSize * 0.5f, 400f - skinChangerPreviewSize - 40f, skinChangerPreviewSize, skinChangerPreviewSize), skinChangerTexture);
		}
	}

	private IEnumerator SkinChangerDownload()
	{
		WWW www = new WWW(skinChangerURL);
		yield return www;
		if (www.error == null && www.size > 0)
		{
			if (skinChangerTexture != null)
			{
				Object.Destroy(skinChangerTexture);
				skinChangerTexture = null;
			}
			skinChangerTexture = www.texture;
		}
	}

	private void ShowFPSGUI()
	{
		Color color = GUI.color;
		if (currentFPS < 30f)
		{
			GUI.color = Color.yellow;
		}
		else if (currentFPS < 10f)
		{
			GUI.color = Color.red;
		}
		else
		{
			GUI.color = Color.green;
		}
		GUILayout.Label("FPS:" + currentFPS);
		GUI.color = color;
	}

	private void RunFPSStuff()
	{
		timeleft -= Time.deltaTime;
		accum += Time.timeScale / Time.deltaTime;
		frames++;
		if ((double)timeleft <= 0.0)
		{
			currentFPS = (int)(accum / (float)frames) * 100 / 100;
			timeleft = updateInterval;
			accum = 0f;
			frames = 0;
		}
	}

	public void StartTutorial()
	{
		tutorialStep = 1;
	}

	public TutorialSteps GetTutorialStep()
	{
		return (TutorialSteps)tutorialStep;
	}

	public bool DoingTutorial()
	{
		return tutorialStep != -1;
	}

	public void FinishedTutorialStep()
	{
		base.audio.PlayOneShot(tutorialStepComplete);
		AccountManager.JustFinishedTutorial();
		if (tutorialStep == 6)
		{
			FinishedTutorial();
		}
		else
		{
			tutorialStep++;
		}
	}

	public void FinishedTutorial()
	{
		tutorialStep = -1;
		PlayerPrefs.SetInt("didTut_" + AccountManager.GetUsername(), 1);
		if (AccountManager.RealAccount())
		{
			StartCoroutine(SetDoneTutorial());
			StartCoroutine(Statistics.UploadStats());
		}
	}

	private void ShowTutorial(int windowID)
	{
		int num = tutorialStep * 16;
		GUI.DrawTexture(new Rect(20f, 34f, 32f, 32f), trophyImage);
		GUILayout.BeginHorizontal();
		GUILayout.Space(32f);
		GUILayout.Label("Tutorial: " + num + "%", "Label_Header");
		GUILayout.EndHorizontal();
		switch (tutorialStep)
		{
		case 1:
			GUILayout.Label("Follow these simple instructions for a quick guide to Cubelands. It will only take a few minutes.");
			GUILayout.Space(5f);
			GUILayout.BeginHorizontal();
			GUILayout.Label("Objective:", GUILayout.Width(60f));
			GUILayout.Label("Walk around (WASD or the Arrow keys, Spacebar to jump) and build a block (Press left mouse button or Control).");
			GUILayout.EndHorizontal();
			break;
		case 2:
			GUILayout.Label("Great! Now remove a cube.");
			GUILayout.Space(5f);
			GUILayout.BeginHorizontal();
			GUILayout.Label("Objective:", GUILayout.Width(60f));
			GUILayout.Label("Remove a cube by looking at it and pressing right mouse button or the Alt key.");
			GUILayout.EndHorizontal();
			break;
		case 3:
			GUILayout.Label("Now copy a cube");
			GUILayout.Space(5f);
			GUILayout.BeginHorizontal();
			GUILayout.Label("Objective:", GUILayout.Width(60f));
			GUILayout.Label("Aim at an existing cube and press middle mouse button or the 'C' key.");
			GUILayout.EndHorizontal();
			break;
		case 4:
			GUILayout.Label("For easier building, you can see and rearrange your inventory.");
			GUILayout.Space(5f);
			GUILayout.BeginHorizontal();
			GUILayout.Label("Objective:", GUILayout.Width(60f));
			GUILayout.Label("Open the inventory by pressing the 'I' key.");
			GUILayout.EndHorizontal();
			break;
		case 5:
			GUILayout.Label("Almost there! Now we'll show you how to read information about the controls.");
			GUILayout.Space(5f);
			GUILayout.BeginHorizontal();
			GUILayout.Label("Objective:", GUILayout.Width(60f));
			GUILayout.Label("First close the inventory, then open the main menu by pressing the escape key or 'M'. Then open and read the HELP screen.");
			GUILayout.EndHorizontal();
			break;
		case 6:
			GUILayout.Label("Create a screenshot and upload it to your Cubelands.com profile to finish the tutorial.");
			GUILayout.Space(5f);
			GUILayout.BeginHorizontal();
			GUILayout.Label("Objective:", GUILayout.Width(60f));
			GUILayout.Label("Press F1 to create a screenshot and follow the instructions to upload it.");
			GUILayout.EndHorizontal();
			break;
		}
	}

	private IEnumerator SetDoneTutorial()
	{
		WWWForm wWWForm = new WWWForm();
		wWWForm.AddField("userID", AccountManager.GetUserID());
		wWWForm.AddField("action", "setTutorialDone");
		wWWForm.AddField("sessionKey", AccountManager.GetSessionKey());
		WWW w = new WWW(wwwURL, wWWForm);
		while (!w.isDone)
		{
			yield return 0;
		}
		Debug.Log(w.text);
	}
}
