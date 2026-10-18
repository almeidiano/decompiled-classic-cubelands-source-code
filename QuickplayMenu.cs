using UnityEngine;

public class QuickplayMenu : MenuPage
{
	public static QuickplayMenu SP;

	private bool viewingPage;

	private float currentTimerstart;

	private Rect windowRect;

	private string quickPlayStatus = "Waiting";

	private bool loading;

	private void Awake()
	{
		SP = this;
		SetNewWindowHeight(275);
	}

	public override void EnableMenu()
	{
		currentTimerstart = Time.realtimeSinceStartup;
		viewingPage = true;
		StartRandomJoin();
	}

	public override void DisableMenu()
	{
		viewingPage = false;
		MultiplayerMenu.SP.AbortRandomConnect();
	}

	private void StartRandomJoin()
	{
		currentTimerstart = Time.realtimeSinceStartup;
		if (viewingPage && !MultiplayerMenu.SP.IsDoingRandomConnect())
		{
			StartCoroutine(MultiplayerMenu.SP.StartJoinRandom(6));
		}
	}

	private float Timer()
	{
		return Time.realtimeSinceStartup - currentTimerstart;
	}

	public override void ShowGUI()
	{
		windowRect = GUI.Window(21, windowRect, MenuMain, string.Empty);
	}

	private void SetNewWindowHeight(int newHeight)
	{
		int num = 400;
		Vector3 vector = new Vector3(Screen.width / 2 - num / 2, Screen.height / 2 - newHeight / 2, 0f);
		windowRect = new Rect(vector.x, vector.y, num, newHeight);
	}

	private void MenuMain(int windowID)
	{
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Back", GUILayout.MaxWidth(150f)))
		{
			mainMenu.ShowMain();
		}
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		GUILayout.Space(20f);
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		GUILayout.Label("Quickplay", "Label_Header");
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		GUILayout.Label("Trying to connect to the first possible multiplayer server. If all servers fail the game will start hosting a game.");
		GUILayout.Label("Time: " + Mathf.Floor(Timer() * 10f) / 10f);
		GUILayout.Label("Status: " + quickPlayStatus);
		DoAutoJoin();
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		GUILayout.Label("In a hurry?");
		if (GUILayout.Button("Host a game right away"))
		{
			MainMenu.SP.PlayClickSound();
			StartHostingGame();
		}
		GUILayout.Space(5f);
		GUILayout.EndHorizontal();
	}

	private void DoAutoJoin()
	{
		if (!MultiplayerFunctions.SP.HasReceivedHostList())
		{
			quickPlayStatus = "Loading multiplayer games list. This should finish under 5 seconds.";
		}
		else if (!MultiplayerFunctions.SP.ReadyLoading())
		{
			quickPlayStatus = "Connecting to master server. This should take up to 10 seconds.";
		}
		else if (MultiplayerMenu.SP.IsDoingRandomConnect())
		{
			quickPlayStatus = "Trying to connect to " + MultiplayerMenu.SP.RandConnectNr();
		}
		else if (Timer() > 10f)
		{
			quickPlayStatus = "Hosting a game ourselves.";
			StartHostingGame();
		}
		else
		{
			quickPlayStatus = "Waiting...";
		}
	}

	private void StartHostingGame()
	{
		if (!loading)
		{
			GameSettings.Clear();
			MultiplayerMenu.SP.StartHostingGameDefaultSettings();
			StartCoroutine(mainMenu.LoadLevel());
			loading = true;
		}
	}
}
