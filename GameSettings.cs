using System.Collections.Generic;
using UnityEngine;

public class GameSettings : MonoBehaviour
{
	public static string gameHadError = string.Empty;

	public static string serverTitle;

	public static string description;

	public static string motd;

	public static string password;

	public static string IP;

	public static int port;

	public static int players;

	public static Color ambientColor = new Color(0.6f, 0.6f, 0.6f, 1f);

	public static Color ambientColorNight = new Color(0.15f, 0.15f, 0.38f, 1f);

	public static Color fogColor = new Color(0.482f, 0.525f, 0.647f, 1f);

	public static Color skyboxColor = new Color(0.6f, 0.6f, 0.6f, 1f);

	public static Color skyboxColorNight = new Color(0.15f, 0.15f, 0.38f, 1f);

	public static Vector3 sunRotation = new Vector3(30f, 30f, 0f);

	public static bool rotateSun = true;

	public static Vector3 rotateSun_Direction = new Vector3(0f, 1f, 0f);

	public static float rotateSun_speed = 0.007f;

	public static string customTerrainFolder = string.Empty;

	public static string customSkyboxFolder = string.Empty;

	public static bool isDedicatedServer;

	public static string mapname = "MyWorld";

	public static int emptyMapHeight = 64;

	public static int emptyMapFillInHeight = 30;

	public static int emptyMapX = 128;

	public static int emptyMapZ = 128;

	public static bool useBuilderList = false;

	public static List<string> modList = new List<string>();

	public static List<string> adminList = new List<string>();

	public static List<string> bannedList = new List<string>();

	public static List<string> builderList = new List<string>();

	public static void Clear()
	{
		serverTitle = (description = (motd = (password = string.Empty)));
		isDedicatedServer = false;
		modList = new List<string>();
		adminList = new List<string>();
	}

	public static string ListToString(List<string> input)
	{
		string text = string.Empty;
		foreach (string item in input)
		{
			if (text != string.Empty)
			{
				text += ",";
			}
			text += item;
		}
		return text;
	}

	public static List<string> StringToList(string inp)
	{
		List<string> list = new List<string>();
		string[] array = inp.Split(',');
		foreach (string text in array)
		{
			if (text.Length >= 2 && !list.Contains(text))
			{
				list.Add(text);
			}
		}
		return list;
	}
}
