using System.Collections;
using System.IO;
using UnityEngine;

public class Preloader : MonoBehaviour
{
	private float progress;

	public Texture2D progressImage;

	public GUITexture companyLogo;

	public GUIStyle emptyStyle;

	private float loadTime = 2.5f;

	private bool checkedGameVersion;

	private bool haveToDismissUpdateMessage;

	private bool allowedToDismiss = true;

	private int latestVersion;

	private bool startedGame;

	private bool downloadingUpdate;

	private void Awake()
	{
		Debug.Log("Preloader awake");
		Application.runInBackground = true;
		Time.timeScale = 1f;
		if (Utils.IsWebplayer())
		{
			Application.targetFrameRate = 50;
		}
		else
		{
			Application.targetFrameRate = 144;
		}
		if (File.Exists("REMOVEME.txt"))
		{
			StartDedicated();
			File.Delete("REMOVEME.txt");
		}
	}

	private void Start()
	{
		checkedGameVersion = true;
		haveToDismissUpdateMessage = false;
		downloadingUpdate = false;
		StartCoroutine(Fade.use.Alpha(companyLogo, -1f, 1f, loadTime * 0.95f, EaseType.Out));
	}

	private void StartDedicated()
	{
		Debug.Log("Start dedicated");
		new GameObject("DedicatedServer").AddComponent<DedicatedServer>();
	}

	private IEnumerator GetLatestGameVersion()
	{
		if (Application.platform == RuntimePlatform.OSXPlayer)
		{
			checkedGameVersion = true;
			yield break;
		}
		if (PlayerPrefs.GetString("SelectedWorld", string.Empty) == string.Empty)
		{
			PlayerPrefs.SetString("SelectedWorld", "MyWorld");
		}
		GameSettings.mapname = PlayerPrefs.GetString("SelectedWorld", "MyWorld");
		WWW stream = new WWW("http://gamedata.cubelands.com/version.php?platform=" + Application.platform);
		while (!stream.isDone)
		{
			yield return 0;
		}
		if (stream.error != null)
		{
			Debug.Log("Couldn't get game version...");
		}
		else
		{
			string text = stream.text;
			Debug.Log(text);
			string[] array = text.Split('#');
			latestVersion = int.Parse(array[0]);
			allowedToDismiss = int.Parse(array[1]) == 1;
			Debug.Log("Cubelands: latestVersion=" + latestVersion + " myVersion=" + Utils.GetGameVersion());
			if (Utils.GetGameVersion() < latestVersion)
			{
				if (Application.platform == RuntimePlatform.OSXDashboardPlayer)
				{
					DownloadDashboardUpdate();
				}
				else
				{
					haveToDismissUpdateMessage = true;
					if (DedicatedServer.isDedicated)
					{
						Debug.Log("Dedicated server quit: Please download the latest version!");
						Application.Quit();
					}
				}
			}
		}
		checkedGameVersion = true;
	}

	private void DownloadDashboardUpdate()
	{
		downloadingUpdate = true;
		string text = "Starting web download.";
		string text2 = string.Empty;
		StartCoroutine(Utils.DownloadLatestWebplayer(latestVersion));
		while (Utils.downloadProgress < 1f && Utils.downloadErrorMessage == string.Empty)
		{
			text = "Downloading latest game: " + Mathf.Round(Utils.downloadProgress * 100f) + "%";
		}
		if (Utils.downloadErrorMessage != string.Empty)
		{
			text2 = Utils.downloadErrorMessage;
		}
		else
		{
			text = "Starting game";
		}
		Debug.Log("currentTask=" + text + " & optional_errorMessage=" + text2);
	}

	private void Update()
	{
		if (!startedGame && !haveToDismissUpdateMessage && !downloadingUpdate && checkedGameVersion && Application.CanStreamedLevelBeLoaded(1))
		{
			StartCoroutine(StartGame());
		}
	}

	private IEnumerator StartGame()
	{
		if (!startedGame)
		{
			Debug.Log("Preloader: STARTGAME CALLED");
			startedGame = true;
			yield return 0;
			Application.LoadLevel(1);
		}
	}

	private void OnGUI()
	{
		int num = 20;
		GUI.Label(new Rect((float)(Screen.width - progressImage.width * 2) * progress, Screen.height - progressImage.height - num, progressImage.width, progressImage.height), progressImage);
		if (haveToDismissUpdateMessage && !downloadingUpdate)
		{
			if (Application.platform == RuntimePlatform.WindowsWebPlayer || Application.platform == RuntimePlatform.OSXWebPlayer)
			{
				GUI.Label(new Rect(Screen.width / 2 - 250, Screen.height - 22 - 100, 500f, 30f), "This game is out of date, the webmaster needs to update it! (" + Utils.GetGameVersion() + " -> " + latestVersion + ")\n. Webplayer: There are ways to have the game auto-upate! Please contact us.", emptyStyle);
			}
			else if (Application.platform == RuntimePlatform.OSXDashboardPlayer)
			{
				GUI.Label(new Rect(Screen.width / 2 - 250, Screen.height - 22 - 100, 500f, 20f), "The game has been updated, please download the latest version (" + Utils.GetGameVersion() + " -> " + latestVersion + ").", emptyStyle);
				if (GUI.Button(new Rect(Screen.width / 2 - 175, Screen.height - 22 - 70, 350f, 20f), "Download the latest version at Cubelands.com"))
				{
					Application.OpenURL("http://classic.cubelands.com/?page=play&platform=" + Application.platform);
				}
				if (allowedToDismiss && GUI.Button(new Rect(Screen.width / 2 - 75, Screen.height - 22 - 40, 150f, 20f), "Dismiss, play Cubelands"))
				{
					haveToDismissUpdateMessage = false;
				}
			}
			else if (Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
			{
				GUI.Label(new Rect(Screen.width / 2 - 250, Screen.height - 22 - 100, 500f, 20f), "The game has been updated, please download the latest version (" + Utils.GetGameVersion() + " -> " + latestVersion + ").", emptyStyle);
				if (GUI.Button(new Rect(Screen.width / 2 - 175, Screen.height - 22 - 70, 350f, 20f), "Download the latest version at Cubelands.com"))
				{
					Application.OpenURL("http://classic.cubelands.com/?page=play&platform=" + Application.platform);
				}
				if (allowedToDismiss && GUI.Button(new Rect(Screen.width / 2 - 75, Screen.height - 22 - 40, 150f, 20f), "Dismiss, play Cubelands"))
				{
					haveToDismissUpdateMessage = false;
				}
			}
		}
		string text = "Loading: " + Mathf.Floor(progress * 100f) + "%";
		if (progress >= 1f && !haveToDismissUpdateMessage)
		{
			text = "Finished loading";
		}
		GUI.Label(new Rect(Screen.width / 2 - 150, Screen.height - 22, 300f, 20f), text, emptyStyle);
	}
}
