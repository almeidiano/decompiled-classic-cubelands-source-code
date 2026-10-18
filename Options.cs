using UnityEngine;

public class Options : MonoBehaviour
{
	public static bool showFPS;

	public static bool optionSSAO;

	public static bool optionDOF;

	public static float cameraFarClipPlane;

	public static int gameQuality;

	public static float audioVolume;

	public static bool saveWorldRegularly;

	public static int mouseSensitivity;

	public static bool swapMouseButtons;

	public static bool invertMouseY;

	public static float musicVolume;

	private static string optionTooltip = string.Empty;

	private static Vector2 optionsScroll = Vector2.zero;

	public static void LoadOptions()
	{
		optionDOF = PlayerPrefs.GetInt("options_DOF", 0) == 1;
		optionSSAO = PlayerPrefs.GetInt("options_SSAO", 0) == 1;
		MouseLook.sensitivityX = (MouseLook.sensitivityY = (mouseSensitivity = PlayerPrefs.GetInt("mouseSensitivity", 400)));
		float farClipPlane = PlayerPrefs.GetInt("options_renderDistance", 100);
		Camera.main.farClipPlane = farClipPlane;
		cameraFarClipPlane = farClipPlane;
		QualitySettings.currentLevel = (QualityLevel)PlayerPrefs.GetInt("options_qualityLevel", 5);
		farClipPlane = (audioVolume = (AudioListener.volume = PlayerPrefs.GetFloat("options_masterVolume", 1f)));
		musicVolume = PlayerPrefs.GetFloat("options_musicVolume", 1f);
		gameQuality = (int)QualitySettings.currentLevel;
		saveWorldRegularly = PlayerPrefs.GetInt("saveWorldRegularly", 1) == 1;
		swapMouseButtons = PlayerPrefs.GetInt("swapMouseButtons", 0) == 1;
		invertMouseY = PlayerPrefs.GetInt("invertMouseY", 0) == 1;
		if (DedicatedServer.isDedicated)
		{
			optionSSAO = (optionDOF = false);
			cameraFarClipPlane = 5f;
			AudioListener.volume = 0f;
			QualitySettings.currentLevel = QualityLevel.Fastest;
		}
		SetMusicVolume(musicVolume);
		SetQuality(gameQuality);
		ReloadDOFOption();
		ReloadSSAOOption();
	}

	public static void SetFPS(bool newVal)
	{
		showFPS = newVal;
		PlayerPrefs.SetInt("options_showFPS", newVal ? 1 : 0);
	}

	public static void SetMouseSensitivity(int newVal)
	{
		MouseLook.sensitivityX = (MouseLook.sensitivityY = (mouseSensitivity = newVal));
		PlayerPrefs.SetInt("mouseSensitivity", mouseSensitivity);
	}

	public static void SetSwapMouseButtons(bool newVal)
	{
		swapMouseButtons = newVal;
		PlayerPrefs.SetInt("swapMouseButtons", swapMouseButtons ? 1 : 0);
	}

	public static void SetInvertMouseY(bool newVal)
	{
		invertMouseY = newVal;
		PlayerPrefs.SetInt("invertMouseY", invertMouseY ? 1 : 0);
	}

	public static void SetSaveWorldRegulary(bool newVal)
	{
		saveWorldRegularly = newVal;
		PlayerPrefs.SetInt("saveWorldRegularly", saveWorldRegularly ? 1 : 0);
	}

	public static void SetCameraFarClip(float newVal)
	{
		Camera.main.farClipPlane = (cameraFarClipPlane = newVal);
		PlayerPrefs.SetInt("options_renderDistance", (int)Camera.main.farClipPlane);
	}

	public static void SetQuality(int newVal)
	{
		gameQuality = newVal;
		QualitySettings.currentLevel = (QualityLevel)gameQuality;
		PlayerPrefs.SetInt("options_qualityLevel", (int)QualitySettings.currentLevel);
		if ((bool)WorldData.SP)
		{
			if (gameQuality >= 4)
			{
				WorldData.SP.ChangeTerrainQuality(toLow: false);
			}
			else
			{
				WorldData.SP.ChangeTerrainQuality(toLow: true);
			}
		}
	}

	public static void SetMasterVolume(float newVal)
	{
		AudioListener.volume = (audioVolume = newVal);
		PlayerPrefs.SetFloat("options_masterVolume", AudioListener.volume);
	}

	public static void SetMusicVolume(float newVal)
	{
		musicVolume = newVal;
		if (Music.SP != null)
		{
			Music.SP.SetVolume(newVal);
		}
		else if (MainMenu.SP != null)
		{
			MainMenu.SP.audio.volume = newVal;
		}
		PlayerPrefs.SetFloat("options_musicVolume", musicVolume);
	}

	public static void SetSSAO(bool newVal)
	{
		optionSSAO = newVal;
		PlayerPrefs.SetInt("options_SSAO", optionSSAO ? 1 : 0);
		ReloadSSAOOption();
	}

	public static void SetDOF(bool newVal)
	{
		optionDOF = newVal;
		PlayerPrefs.SetInt("options_DOF", optionDOF ? 1 : 0);
		ReloadDOFOption();
	}

	private static void ReloadDOFOption()
	{
		if (!optionDOF)
		{
			Camera.main.gameObject.SendMessage("DisableDOF", SendMessageOptions.DontRequireReceiver);
		}
		else
		{
			Camera.main.gameObject.SendMessage("EnableDOF", SendMessageOptions.DontRequireReceiver);
		}
	}

	private static void ReloadSSAOOption()
	{
		if (!optionSSAO)
		{
			Camera.main.gameObject.SendMessage("DisableSSAO", SendMessageOptions.DontRequireReceiver);
		}
		else
		{
			Camera.main.gameObject.SendMessage("EnableSSAO", SendMessageOptions.DontRequireReceiver);
		}
	}

	public static bool OptimizeGame(int avgFPS)
	{
		if (avgFPS <= 20)
		{
			if (gameQuality >= 4)
			{
				SetQuality(gameQuality--);
				return true;
			}
			if (cameraFarClipPlane > 50f)
			{
				SetCameraFarClip(50f);
				return true;
			}
			if (optionDOF)
			{
				SetDOF(newVal: false);
				return true;
			}
			if (optionSSAO)
			{
				SetSSAO(newVal: false);
				return true;
			}
			if (gameQuality >= 1)
			{
				SetQuality(gameQuality--);
				return true;
			}
			if (cameraFarClipPlane > 25f)
			{
				SetCameraFarClip(25f);
				return true;
			}
		}
		else if (avgFPS >= 60)
		{
			if (cameraFarClipPlane < 25f)
			{
				SetCameraFarClip(25f);
				return true;
			}
			if (gameQuality < 4)
			{
				SetQuality(gameQuality++);
				return true;
			}
			if (cameraFarClipPlane < 75f)
			{
				SetCameraFarClip(75f);
				return true;
			}
			if (cameraFarClipPlane < 50f)
			{
				SetCameraFarClip(50f);
				return true;
			}
			if (cameraFarClipPlane < 100f)
			{
				SetCameraFarClip(100f);
				return true;
			}
			if (gameQuality < 5)
			{
				SetQuality(gameQuality++);
				return true;
			}
			if (cameraFarClipPlane < 150f)
			{
				SetCameraFarClip(150f);
				return true;
			}
			if (avgFPS >= 100 && !optionDOF)
			{
				SetDOF(newVal: true);
				return true;
			}
		}
		return false;
	}

	public static void ShowGUI()
	{
		GUILayout.Label((!(optionTooltip == string.Empty)) ? optionTooltip : "Hover over an option for a description", GUILayout.Height(40f));
		optionsScroll = GUILayout.BeginScrollView(optionsScroll);
		GUILayout.Label("Performance settings", "Label_Header2");
		GUILayout.BeginHorizontal();
		GUILayout.Label(new GUIContent("Show FPS in-game", "Show a performance counter in the top-left of the screen"), GUILayout.Width(175f));
		showFPS = GUILayout.Toggle(showFPS, string.Empty);
		if (GUI.changed)
		{
			SetFPS(showFPS);
		}
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label(new GUIContent("Fullscreen", "Switch between fullscreen/windowed mode"), GUILayout.Width(175f));
		if (GUILayout.Button(new GUIContent("Switch", "Switch between fullscresunen/windowed mode")))
		{
			Screen.fullScreen = !Screen.fullScreen;
		}
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label(new GUIContent("SSAO", "SSAO: Adds ambient occlusion \"shadows\", quite heavy though!"), GUILayout.Width(175f));
		optionSSAO = GUILayout.Toggle(optionSSAO, new GUIContent(string.Empty, "SSAO: Adds ambient occlusion \"shadows\", quite heavy though!"));
		if (GUI.changed)
		{
			SetSSAO(optionSSAO);
		}
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label(new GUIContent("Depth of Field", "DOF: Adds a blur, quite heavy though"), GUILayout.Width(175f));
		optionDOF = GUILayout.Toggle(optionDOF, new GUIContent(string.Empty, "DOF: Adds a blur, quite heavy though"));
		if (GUI.changed)
		{
			SetDOF(optionDOF);
		}
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label(new GUIContent("Camera distance (" + (int)Camera.main.farClipPlane + "/250)", "To increase performance you can reduce the maximum camera distance."));
		cameraFarClipPlane = GUILayout.HorizontalSlider(cameraFarClipPlane, 10f, 250f, GUILayout.Width(200f));
		GUILayout.EndHorizontal();
		if (GUI.changed)
		{
			SetCameraFarClip(cameraFarClipPlane);
		}
		GUILayout.BeginHorizontal();
		GUILayout.Label(new GUIContent("Game quality (" + (int)(QualitySettings.currentLevel + 1) + "/6)", "Use this slider to quickly change the game quality to improve your performance."));
		gameQuality = (int)GUILayout.HorizontalSlider(gameQuality, 0f, 5f, GUILayout.Width(200f));
		GUILayout.EndHorizontal();
		if (GUI.changed)
		{
			SetQuality(gameQuality);
		}
		GUILayout.Space(10f);
		GUILayout.Label("Audio settings", "Label_Header2");
		GUILayout.BeginHorizontal();
		GUILayout.Label(new GUIContent("Master volume (" + (int)(audioVolume * 100f) + "%)", "Change the master volume: This affects all music and sound effects."));
		audioVolume = GUILayout.HorizontalSlider(audioVolume, 0f, 1f, GUILayout.Width(200f));
		GUILayout.EndHorizontal();
		if (GUI.changed)
		{
			SetMasterVolume(audioVolume);
		}
		GUILayout.BeginHorizontal();
		GUILayout.Label(new GUIContent("Music volume (" + (int)(musicVolume * 100f) + "%)", "Change the music volume. Drag it to the far left to disable"));
		musicVolume = GUILayout.HorizontalSlider(musicVolume, 0f, 1f, GUILayout.Width(200f));
		GUILayout.EndHorizontal();
		if (GUI.changed)
		{
			SetMusicVolume(musicVolume);
		}
		GUILayout.Space(10f);
		GUILayout.Label("Controls", "Label_Header2");
		GUILayout.BeginHorizontal();
		GUILayout.Label(new GUIContent("Mouse sensitivity", "Slide this to the right for faster mouse movement"), GUILayout.Width(175f));
		mouseSensitivity = (int)GUILayout.HorizontalSlider(mouseSensitivity, 10f, 720f, GUILayout.Width(200f));
		GUILayout.EndHorizontal();
		if (GUI.changed)
		{
			SetMouseSensitivity(mouseSensitivity);
		}
		GUILayout.BeginHorizontal();
		GUILayout.Label(new GUIContent("Swap left&right mouse buttons", "By default(when the option is off) LMB=Build RMB=Destroy"), GUILayout.Width(175f));
		swapMouseButtons = GUILayout.Toggle(swapMouseButtons, new GUIContent(string.Empty, "By default(when the option is off) LMB=Build RMB=Destroy"));
		GUILayout.EndHorizontal();
		if (GUI.changed)
		{
			SetSwapMouseButtons(swapMouseButtons);
		}
		GUILayout.BeginHorizontal();
		GUILayout.Label(new GUIContent("Invert mouse Y axis", "Inverts the vertical mouse movement"), GUILayout.Width(175f));
		invertMouseY = GUILayout.Toggle(invertMouseY, new GUIContent(string.Empty, "Inverts the vertical mouse movement"));
		GUILayout.EndHorizontal();
		if (GUI.changed)
		{
			SetInvertMouseY(invertMouseY);
		}
		GUILayout.EndScrollView();
		if (Event.current.type == EventType.Repaint)
		{
			optionTooltip = GUI.tooltip;
		}
	}
}
