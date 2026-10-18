using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class DedicatedServer : MonoBehaviour
{
	public static bool isDedicated;

	public static bool useLevelFiles = true;

	private void Awake()
	{
		isDedicated = true;
		Object.DontDestroyOnLoad(this);
		AudioListener.volume = 0f;
		if (Application.loadedLevel == 0)
		{
			OnLevelWasLoaded(0);
		}
		Screen.SetResolution(350, 200, fullscreen: false);
	}

	private IEnumerator OnLevelWasLoaded(int level)
	{
		yield return 0;
		yield return 0;
		Debug.Log("Dedicated server: " + Application.loadedLevel);
		if (Application.loadedLevel != 0)
		{
			if (Application.loadedLevel == 1)
			{
				while (!MultiplayerFunctions.SP.ReadyLoading() && Time.realtimeSinceStartup <= 10f)
				{
					yield return 0;
				}
				StartServer();
			}
			else
			{
				_ = Application.loadedLevel;
			}
		}
		Debug.Log("DedicatedServer: done for this scene");
	}

	public static Dictionary<string, string> ReadSettingsFile()
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		if (File.Exists("serversettings.txt"))
		{
			StreamReader streamReader = new StreamReader("serversettings.txt");
			for (string text = streamReader.ReadLine(); text != null; text = streamReader.ReadLine())
			{
				string[] array = text.Split('=');
				if (array.Length == 2)
				{
					Debug.Log(array[0] + "--" + array[1]);
					dictionary.Add(array[0], array[1]);
				}
			}
			streamReader.Close();
		}
		if (!dictionary.ContainsKey("title"))
		{
			dictionary.Add("title", "Dedicated server " + Random.Range(1, 999));
		}
		if (!dictionary.ContainsKey("MOTD"))
		{
			dictionary.Add("MOTD", "The Admin must enter a MOTD");
		}
		if (!dictionary.ContainsKey("serverDescription"))
		{
			dictionary.Add("serverDescription", "The Admin must enter a description");
		}
		if (!dictionary.ContainsKey("PW"))
		{
			dictionary.Add("PW", string.Empty);
		}
		if (!dictionary.ContainsKey("players"))
		{
			dictionary.Add("players", "16");
		}
		if (!dictionary.ContainsKey("port"))
		{
			dictionary.Add("port", MultiplayerFunctions.SP.defaultServerPort + string.Empty);
		}
		if (!dictionary.ContainsKey("mapName"))
		{
			dictionary.Add("mapName", "MyDedicatedWorld");
		}
		if (!dictionary.ContainsKey("emptyMapHeight"))
		{
			dictionary.Add("emptyMapHeight", "64");
		}
		if (!dictionary.ContainsKey("emptyMapXWidth"))
		{
			dictionary.Add("emptyMapXWidth", "128");
		}
		if (!dictionary.ContainsKey("emptyMapZWidth"))
		{
			dictionary.Add("emptyMapZWidth", "128");
		}
		if (!dictionary.ContainsKey("emptyMapFillInHeight"))
		{
			dictionary.Add("emptyMapFillInHeight", "30");
		}
		if (!dictionary.ContainsKey("targetFrameRate"))
		{
			dictionary.Add("targetFrameRate", "25");
		}
		if (!dictionary.ContainsKey("modList"))
		{
			dictionary.Add("modList", string.Empty);
		}
		if (!dictionary.ContainsKey("adminList"))
		{
			dictionary.Add("adminList", string.Empty);
		}
		if (!dictionary.ContainsKey("bannedList"))
		{
			dictionary.Add("bannedList", string.Empty);
		}
		if (!dictionary.ContainsKey("builderList"))
		{
			dictionary.Add("builderList", string.Empty);
		}
		if (!dictionary.ContainsKey("useBuilderList"))
		{
			dictionary.Add("useBuilderList", "0");
		}
		if (!dictionary.ContainsKey("customSkyboxFolder"))
		{
			dictionary.Add("customSkyboxFolder", GameSettings.customSkyboxFolder);
		}
		if (!dictionary.ContainsKey("customTerrainFolder"))
		{
			dictionary.Add("customTerrainFolder", GameSettings.customTerrainFolder);
		}
		if (!dictionary.ContainsKey("ambientColor"))
		{
			dictionary.Add("ambientColor", Utils.ColorToString(GameSettings.ambientColor));
		}
		if (!dictionary.ContainsKey("skyboxColor"))
		{
			dictionary.Add("skyboxColor", Utils.ColorToString(GameSettings.skyboxColor));
		}
		if (!dictionary.ContainsKey("fogColor"))
		{
			dictionary.Add("fogColor", Utils.ColorToString(GameSettings.fogColor));
		}
		if (!dictionary.ContainsKey("sunRotation"))
		{
			dictionary.Add("sunRotation", Utils.Vector3ToString(GameSettings.sunRotation));
		}
		if (!dictionary.ContainsKey("rotateSun"))
		{
			dictionary.Add("rotateSun", Utils.BoolToString(GameSettings.rotateSun));
		}
		if (!dictionary.ContainsKey("rotateSun_Direction"))
		{
			dictionary.Add("rotateSun_Direction", Utils.Vector3ToString(GameSettings.rotateSun_Direction));
		}
		if (!dictionary.ContainsKey("rotateSun_speed"))
		{
			dictionary.Add("rotateSun_speed", Utils.FloatToString(GameSettings.rotateSun_speed));
		}
		return dictionary;
	}

	private void StartServer()
	{
		Dictionary<string, string> dictionary = ReadSettingsFile();
		GameSettings.Clear();
		Application.targetFrameRate = Mathf.Clamp(Utils.SafeIntParse(dictionary["targetFrameRate"], 25), -1, 120);
		GameSettings.modList = GameSettings.StringToList(dictionary["modList"]);
		GameSettings.adminList = GameSettings.StringToList(dictionary["adminList"]);
		GameSettings.bannedList = GameSettings.StringToList(dictionary["bannedList"]);
		GameSettings.builderList = GameSettings.StringToList(dictionary["builderList"]);
		GameSettings.serverTitle = dictionary["title"];
		GameSettings.description = dictionary["serverDescription"];
		GameSettings.motd = dictionary["MOTD"];
		GameSettings.password = dictionary["PW"];
		GameSettings.port = Utils.SafeIntParse(dictionary["port"], 25010);
		GameSettings.useBuilderList = Utils.StringToBool(dictionary["useBuilderList"]);
		GameSettings.ambientColor = Utils.StringToColor(dictionary["ambientColor"]);
		GameSettings.fogColor = Utils.StringToColor(dictionary["fogColor"]);
		GameSettings.skyboxColor = Utils.StringToColor(dictionary["skyboxColor"]);
		GameSettings.sunRotation = Utils.StringToVector3(dictionary["sunRotation"]);
		GameSettings.rotateSun_Direction = Utils.StringToVector3(dictionary["rotateSun_Direction"]);
		GameSettings.rotateSun = Utils.StringToBool(dictionary["rotateSun"]);
		GameSettings.rotateSun_speed = Utils.StringToFloat(dictionary["rotateSun_speed"]);
		GameSettings.customTerrainFolder = dictionary["customTerrainFolder"];
		GameSettings.customSkyboxFolder = dictionary["customSkyboxFolder"];
		int maxplayers = (GameSettings.players = Utils.SafeIntParse(dictionary["players"], 16));
		string mapName = (GameSettings.mapname = dictionary["mapName"]);
		int emptyMapHeight = (GameSettings.emptyMapHeight = Utils.SafeIntParse(dictionary["emptyMapHeight"], 64));
		int emptymapx = (GameSettings.emptyMapX = Utils.SafeIntParse(dictionary["emptyMapXWidth"], 128));
		int emptymapz = (GameSettings.emptyMapZ = Utils.SafeIntParse(dictionary["emptyMapZWidth"], 128));
		int worldFillInHeight = (GameSettings.emptyMapFillInHeight = Utils.SafeIntParse(dictionary["emptyMapFillInHeight"], 30));
		SaveSettingsToFile();
		MultiplayerMenu.SP.StartHostingGame(GameSettings.motd, GameSettings.description, GameSettings.serverTitle, GameSettings.port, GameSettings.password != string.Empty, GameSettings.password, maxplayers, mapName, emptyMapHeight, emptymapx, emptymapz, worldFillInHeight);
		Debug.Log("Starting dedicated server");
	}

	public static void SaveSettingsToFile()
	{
		WriteSettings(new Dictionary<string, string>
		{
			{
				"######## GENERAL",
				string.Empty
			},
			{
				"port",
				GameSettings.port + string.Empty
			},
			{
				"players",
				GameSettings.players + string.Empty
			},
			{
				"PW",
				GameSettings.password
			},
			{
				"MOTD",
				GameSettings.motd
			},
			{
				"serverDescription",
				GameSettings.description
			},
			{
				"title",
				GameSettings.serverTitle
			},
			{
				"targetFrameRate",
				Application.targetFrameRate + string.Empty
			},
			{
				"######## MAP DATA",
				string.Empty
			},
			{
				"mapName",
				GameSettings.mapname
			},
			{
				"emptyMapHeight",
				GameSettings.emptyMapHeight + string.Empty
			},
			{
				"emptyMapXWidth",
				GameSettings.emptyMapX + string.Empty
			},
			{
				"emptyMapZWidth",
				GameSettings.emptyMapZ + string.Empty
			},
			{
				"emptyMapFillInHeight",
				GameSettings.emptyMapFillInHeight + string.Empty
			},
			{
				"######## SERVERPERMISSIONS",
				string.Empty
			},
			{
				"modList",
				GameSettings.ListToString(GameSettings.modList)
			},
			{
				"adminList",
				GameSettings.ListToString(GameSettings.adminList)
			},
			{
				"bannedList",
				GameSettings.ListToString(GameSettings.bannedList)
			},
			{
				"useBuilderList",
				Utils.BoolToString(GameSettings.useBuilderList)
			},
			{
				"builderList",
				GameSettings.ListToString(GameSettings.builderList)
			},
			{
				"######## RENDERSETTINGS",
				string.Empty
			},
			{
				"customSkyboxFolder",
				GameSettings.customSkyboxFolder
			},
			{
				"customTerrainFolder",
				GameSettings.customTerrainFolder
			},
			{
				"ambientColor",
				Utils.ColorToString(GameSettings.ambientColor)
			},
			{
				"fogColor",
				Utils.ColorToString(GameSettings.fogColor)
			},
			{
				"skyboxColor",
				Utils.ColorToString(GameSettings.skyboxColor)
			},
			{
				"sunRotation",
				Utils.Vector3ToString(GameSettings.sunRotation)
			},
			{
				"rotateSun",
				Utils.BoolToString(GameSettings.rotateSun)
			},
			{
				"rotateSun_Direction",
				Utils.Vector3ToString(GameSettings.rotateSun_Direction)
			},
			{
				"rotateSun_speed",
				Utils.FloatToString(GameSettings.rotateSun_speed)
			}
		});
		Debug.Log("Wrote server settings back to file.");
	}

	public static void WriteSettings(Dictionary<string, string> saveDict)
	{
		if (File.Exists("serversettings.txt"))
		{
			File.Delete("serversettings.txt");
		}
		StreamWriter streamWriter = File.CreateText("serversettings.txt");
		foreach (KeyValuePair<string, string> item in saveDict)
		{
			if (item.Key.Substring(0, 1) != "#")
			{
				streamWriter.WriteLine(item.Key + "=" + item.Value);
				continue;
			}
			streamWriter.WriteLine(string.Empty);
			streamWriter.WriteLine(item.Key + item.Value);
		}
		streamWriter.Close();
	}
}
