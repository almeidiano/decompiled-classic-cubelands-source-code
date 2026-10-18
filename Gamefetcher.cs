using System.Collections;
using UnityEngine;

public class Gamefetcher : MonoBehaviour
{
	private float progress;

	private string currentTask = "Starting fetcher...";

	public Texture2D progressImage;

	public GUITexture fadeTexture;

	public GUIStyle emptyStyle;

	private int latestVersion;

	private string errorMessage = string.Empty;

	private float oldprogress;

	private int retries;

	private void Awake()
	{
		Application.runInBackground = true;
		Debug.Log("GAMEFETCHER AWAKE");
		Time.timeScale = 1f;
	}

	private IEnumerator Start()
	{
		StartCoroutine(GetLatestGameVersion());
		if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.OSXPlayer)
		{
			Debug.LogError("ERROR: STANDLONE SHOULDNT USE GAMEFETCHER");
			yield break;
		}
		while (latestVersion == 0)
		{
			yield return 0;
		}
		yield return 0;
		currentTask = "Starting web download.";
		StartCoroutine(Utils.DownloadLatestWebplayer(latestVersion));
		while (Utils.downloadProgress < 1f && Utils.downloadErrorMessage == string.Empty)
		{
			yield return 0;
			currentTask = "Downloading latest game: " + Mathf.Round(Utils.downloadProgress * 100f) + "%";
		}
		if (Utils.downloadErrorMessage != string.Empty)
		{
			errorMessage = Utils.downloadErrorMessage;
		}
		else
		{
			currentTask = "Starting game";
		}
	}

	private void OnGUI()
	{
		int num = 20;
		GUI.Label(new Rect((float)(Screen.width - progressImage.width * 2) * progress, Screen.height - progressImage.height - num, progressImage.width, progressImage.height), progressImage);
		if (errorMessage != string.Empty)
		{
			GUI.Label(new Rect(Screen.width / 2 - 200, Screen.height / 2 - 25 + 90, 400f, 50f), "Download error: " + errorMessage, emptyStyle);
			if (GUI.Button(new Rect(Screen.width / 2 - 125, Screen.height / 2 + 15 + 90, 250f, 30f), "Play this game on Cubelands.com"))
			{
				Application.OpenURL("http://classic.cubelands.com/?error=" + errorMessage);
			}
		}
		GUI.Label(new Rect(Screen.width / 2 - 150, Screen.height - 22, 300f, 20f), currentTask, emptyStyle);
	}

	private IEnumerator GetLatestGameVersion()
	{
		WWW stream = new WWW("http://gamedata.cubelands.com/version.php?platform=" + Application.platform);
		while (!stream.isDone)
		{
			yield return 0;
		}
		if (stream.error != null)
		{
			Debug.Log("Couldn't get game version...");
			StartCoroutine(CheckRetry());
			yield break;
		}
		string[] array = stream.text.Split('#');
		if (array.Length >= 2)
		{
			latestVersion = int.Parse(array[0]);
		}
		else
		{
			StartCoroutine(CheckRetry());
		}
	}

	private IEnumerator CheckRetry()
	{
		if (retries < 4)
		{
			retries++;
			yield return new WaitForSeconds(10f);
			StartCoroutine(GetLatestGameVersion());
		}
	}
}
