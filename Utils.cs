using System;
using System.Collections;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

public class Utils : MonoBehaviour
{
	public static int gameVersion;

	public static float downloadProgress;

	public static string downloadErrorMessage = string.Empty;

	public static bool IsWebplayer()
	{
		if (Application.platform != RuntimePlatform.OSXDashboardPlayer && Application.platform != RuntimePlatform.OSXWebPlayer)
		{
			return Application.platform == RuntimePlatform.WindowsWebPlayer;
		}
		return true;
	}

	public static bool IsStandalone()
	{
		if (Application.platform != RuntimePlatform.WindowsEditor && Application.platform != RuntimePlatform.WindowsPlayer)
		{
			return Application.platform == RuntimePlatform.OSXPlayer;
		}
		return true;
	}

	public static bool IsEditor()
	{
		if (Application.platform != RuntimePlatform.WindowsEditor)
		{
			return Application.platform == RuntimePlatform.OSXEditor;
		}
		return true;
	}

	public static bool IsAlfabetical(string input)
	{
		return new Regex("^[a-z]*$", RegexOptions.IgnoreCase).IsMatch(input);
	}

	public static bool PositionWithinRect(float x, float y, Rect myRect)
	{
		int num = 5;
		if (x >= myRect.x - (float)num && x <= myRect.xMax + (float)num && y <= (float)Screen.height - myRect.y + (float)num)
		{
			return y >= (float)Screen.height - myRect.yMax - (float)num;
		}
		return false;
	}

	public static string SelectList(ArrayList list, string selected, GUIStyle defaultStyle, GUIStyle selectedStyle)
	{
		for (int i = 0; i < list.Count; i++)
		{
			if (GUILayout.Button(list[i].ToString(), (!(selected == (string)list[i])) ? defaultStyle : selectedStyle))
			{
				selected = list[i].ToString();
			}
		}
		return selected;
	}

	public static bool IsCubelandsHosted()
	{
		if (Application.absoluteURL.IndexOf(".cubelands.com") > 0)
		{
			return Application.absoluteURL.IndexOf(".cubelands.com") <= 14;
		}
		return false;
	}

	public static string CapString(string input, int maxLength)
	{
		if (input.Length <= maxLength)
		{
			return input;
		}
		return input.Substring(0, maxLength);
	}

	public static string Md5Sum(string strToEncrypt)
	{
		byte[] bytes = new UTF8Encoding().GetBytes(strToEncrypt);
		byte[] array = new MD5CryptoServiceProvider().ComputeHash(bytes);
		string text = string.Empty;
		for (int i = 0; i < array.Length; i++)
		{
			text += Convert.ToString(array[i], 16).PadLeft(2, '0');
		}
		return text.PadLeft(32, '0');
	}

	public static int GetGameVersion()
	{
		if (gameVersion == 0 || IsEditor())
		{
			gameVersion = int.Parse(((TextAsset)Resources.Load("version", typeof(TextAsset))).text);
		}
		return gameVersion;
	}

	public static string BoolToString(bool mB)
	{
		if (mB)
		{
			return "1";
		}
		return "0";
	}

	public static string ColorToString(Color myC)
	{
		return FloatToString(myC.r) + "," + FloatToString(myC.g) + "," + FloatToString(myC.b) + "," + FloatToString(myC.a);
	}

	public static string Vector3ToString(Vector3 myF)
	{
		return FloatToString(myF.x) + "," + FloatToString(myF.y) + "," + FloatToString(myF.z);
	}

	public static string FloatToString(float myF)
	{
		return myF.ToString(CultureInfo.InvariantCulture.NumberFormat);
	}

	public static string RectToString(Rect rect)
	{
		return FloatToString(rect.x) + "#" + FloatToString(rect.y) + "#" + FloatToString(rect.width) + "#" + FloatToString(rect.height);
	}

	public static bool StringToBool(string mB)
	{
		return mB == "1";
	}

	public static Color StringToColor(string myS)
	{
		string[] array = myS.Split(',');
		if (array.Length != 4)
		{
			Debug.LogError("Could not parse '" + myS + "' to Color!");
			Application.Quit();
		}
		return new Color(StringToFloat(array[0]), StringToFloat(array[1]), StringToFloat(array[2]), StringToFloat(array[3]));
	}

	public static Vector3 StringToVector3(string myS)
	{
		string[] array = myS.Split(',');
		if (array.Length != 3)
		{
			Debug.LogError("Could not parse '" + myS + "' to Vector3!");
			Application.Quit();
		}
		return new Vector3(StringToFloat(array[0]), StringToFloat(array[1]), StringToFloat(array[2]));
	}

	public static float StringToFloat(string myS)
	{
		return float.Parse(myS, CultureInfo.InvariantCulture.NumberFormat);
	}

	public static Rect StringToRect(string myS)
	{
		string[] array = myS.Split('#');
		if (array.Length == 4)
		{
			return new Rect(StringToFloat(array[0]), StringToFloat(array[1]), StringToFloat(array[2]), StringToFloat(array[3]));
		}
		return default(Rect);
	}

	public static int SafeIntParse(string value, int defaultN)
	{
		if (int.TryParse(value, out var result))
		{
			return result;
		}
		return defaultN;
	}

	public static IEnumerator DownloadLatestWebplayer(int latestVersion)
	{
		string url = "http://files.cubelands.com/cubelands_game.unity3d?" + latestVersion;
		WWW stream = new WWW(url);
		while (!stream.isDone)
		{
			downloadProgress = stream.progress;
			yield return 0;
		}
		downloadProgress = stream.progress;
		yield return 0;
		if (stream.bytes.Length <= 500 && downloadProgress != 1f)
		{
			downloadErrorMessage = "Downloaded file too small (" + stream.bytes.Length + " bytes)";
			yield break;
		}
		if (stream.error != null)
		{
			downloadErrorMessage = stream.error;
			yield break;
		}
		yield return 0;
		Debug.Log(Time.realtimeSinceStartup + "DownloadLatestWebplayer LOADUNITY downLink=");
		stream.LoadUnityWeb();
	}
}
