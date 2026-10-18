using System.Collections.Generic;
using UnityEngine;

public class WorldMenu : MenuPage
{
	public static WorldMenu SP;

	private Rect windowRect;

	private List<string> webplayerPlayerPrefsMapnames;

	private string exportString = string.Empty;

	private int exportLength;

	private string deleteWorld = string.Empty;

	private int menuPage;

	private string importString = string.Empty;

	private string saveName = "MyImportedWorld";

	private string result = string.Empty;

	private List<string> standaloneMaps;

	private void Awake()
	{
		SP = this;
		SetNewWindowHeight(500);
	}

	public override void EnableMenu()
	{
		LoadWebplayerMaps();
		LoadSAMaps();
	}

	public override void DisableMenu()
	{
	}

	public override void ShowGUI()
	{
		if (Utils.IsWebplayer() || Application.platform == RuntimePlatform.OSXDashboardPlayer)
		{
			windowRect = GUI.Window(21, windowRect, MenuMainWeb, string.Empty);
		}
		else
		{
			windowRect = GUI.Window(21, windowRect, MenuMainSA, string.Empty);
		}
	}

	private void SetNewWindowHeight(int newHeight)
	{
		int num = 550;
		Vector3 vector = new Vector3(Screen.width / 2 - num / 2, Screen.height / 2 - newHeight / 2, 0f);
		windowRect = new Rect(vector.x, vector.y, num, newHeight);
	}

	public void LoadWebplayerMaps()
	{
		webplayerPlayerPrefsMapnames = new List<string>();
		if (PlayerPrefs.GetString(GameSettings.mapname, string.Empty).Length >= 10)
		{
			webplayerPlayerPrefsMapnames.Add(GameSettings.mapname);
		}
	}

	private void MenuMainWeb(int windowID)
	{
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Back", GUILayout.MaxWidth(150f)))
		{
			mainMenu.ShowMain();
		}
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		GUILayout.Label("Manage worlds", "Label_Header");
		GUILayout.Label("Using this tool you can export your worlddata to a standalone player or dedicated server. You cannot import maps to a webplayer.");
		GUILayout.Space(10f);
		PrefsExport();
	}

	private void WebExport(string mapName)
	{
		exportString = PlayerPrefs.GetString(mapName, "empty");
		exportLength = exportString.Length;
	}

	private void WebDeleteWorlds(string mapName)
	{
		deleteWorld = mapName;
	}

	private void PrefsExport()
	{
		if (deleteWorld != string.Empty)
		{
			GUILayout.Label("Delete world", "Label_Header2");
			GUILayout.Label("World: " + deleteWorld);
			GUILayout.Label("Are you sure you want to reset this world?");
			GUILayout.Label("This world will be reset to the default webplayer world. Play via the standalone player to be able to fully manage your worlds.");
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Cancel"))
			{
				deleteWorld = string.Empty;
			}
			if (GUILayout.Button("Delete"))
			{
				TextAsset textAsset = (TextAsset)Resources.Load("defaultWorld", typeof(TextAsset));
				PlayerPrefs.SetString(deleteWorld, textAsset.text);
				deleteWorld = string.Empty;
			}
			GUILayout.EndHorizontal();
			return;
		}
		if (exportString != string.Empty)
		{
			GUILayout.Label("World data", "Label_Header2");
			if (exportString == "empty")
			{
				GUILayout.Label("The data seem to be empty!");
			}
			else
			{
				GUILayout.Label("Copy&paste this data directly in the standalone game import feature.");
				GUILayout.Label("Warning: Pasting it in a .rtf or .doc will probably mess up the data.");
				GUILayout.TextArea(exportString, GUILayout.Height(200f));
				GUILayout.Label("Length: " + exportLength + " (data length: " + exportString.Length + ")", GUILayout.Height(200f));
			}
			if (GUILayout.Button("Back"))
			{
				exportString = string.Empty;
			}
			return;
		}
		GUILayout.Label("Saved worlds", "Label_Header2");
		if (webplayerPlayerPrefsMapnames.Count == 0)
		{
			GUILayout.Label("You have no saved worlds yet.");
			return;
		}
		foreach (string webplayerPlayerPrefsMapname in webplayerPlayerPrefsMapnames)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label("Name: " + webplayerPlayerPrefsMapname, GUILayout.Width(100f));
			if (GUILayout.Button("Export", GUILayout.Width(80f)))
			{
				WebExport(webplayerPlayerPrefsMapname);
			}
			if (GUILayout.Button("Reset", GUILayout.Width(80f)))
			{
				WebDeleteWorlds(webplayerPlayerPrefsMapname);
			}
			GUILayout.EndHorizontal();
		}
	}

	private void MenuMainSA(int windowID)
	{
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Back", GUILayout.MaxWidth(150f)))
		{
			mainMenu.ShowMain();
		}
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		GUILayout.Label("Manage worlds", "Label_Header");
		GUILayout.Label("You can use this tool to import worlds from the webplayer version of the game.");
		GUILayout.Space(10f);
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Select world"))
		{
			menuPage = 0;
		}
		if (GUILayout.Button("Import world"))
		{
			menuPage = 1;
		}
		GUILayout.EndHorizontal();
		if (menuPage == 0)
		{
			SelectWorld();
		}
		if (menuPage == 1)
		{
			ImportTool();
		}
	}

	private void SelectWorld()
	{
		GUILayout.Label("Select world data", "Label_Header2");
		if (standaloneMaps.Count == 0)
		{
			GUILayout.Label("You have no worlds yet, play the game or import worlds.");
			return;
		}
		foreach (string standaloneMap in standaloneMaps)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label("Name: " + standaloneMap, GUILayout.Width(200f));
			if (GameSettings.mapname == standaloneMap)
			{
				GUILayout.Label("Selected");
			}
			else if (GUILayout.Button("Select", GUILayout.Width(150f)))
			{
				GameSettings.mapname = standaloneMap;
				PlayerPrefs.SetString("SelectedWorld", standaloneMap);
			}
			GUILayout.EndHorizontal();
		}
	}

	private void ImportTool()
	{
		GUILayout.Label("Import world data", "Label_Header2");
		if (result != string.Empty)
		{
			GUILayout.Label("Result: " + result);
			if (GUILayout.Button("Close"))
			{
				result = string.Empty;
			}
			return;
		}
		GUILayout.Label("Paste your exported world data below.");
		saveName = GUILayout.TextField(saveName);
		string text = importString;
		importString = GUILayout.TextArea(importString, GUILayout.Height(200f));
		if (importString.Length <= 10)
		{
			importString = string.Empty;
		}
		if (text.Length < importString.Length && importString.Length - text.Length <= 5)
		{
			importString = text;
		}
		if (GUILayout.Button("Import"))
		{
			result = ImportWorld(importString, saveName);
		}
	}

	private string ImportWorld(string worldData, string worldName)
	{
		if (!Utils.IsAlfabetical(worldName))
		{
			return "Error: The save filename can only contain the a-Z characters.";
		}
		string text = string.Empty;
		try
		{
			text = Zipper.UnzipString(worldData);
		}
		catch
		{
		}
		if (text == string.Empty)
		{
			return "Wrong import data; the begin or end is wrong. Please export your level again.";
		}
		WorldData.WriteWorldData(worldName, worldData);
		LoadSAMaps();
		GameSettings.mapname = worldName;
		PlayerPrefs.SetString("SelectedWorld", worldName);
		return "Imported, please verify the map!";
	}

	public void LoadSAMaps()
	{
		standaloneMaps = new List<string>();
		WorldData.CreateSaveFolder();
		string[] saveNames = WorldData.GetSaveNames();
		foreach (string item in saveNames)
		{
			standaloneMaps.Add(item);
		}
	}
}
