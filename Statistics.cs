using System.Collections;
using System.Collections.Generic;
using LitJson;
using UnityEngine;

public class Statistics : MonoBehaviour
{
	private static Dictionary<Stats, int> statDict = new Dictionary<Stats, int>();

	private static Dictionary<Stats, int> statDict3 = new Dictionary<Stats, int>();

	public static void Clear()
	{
		statDict = new Dictionary<Stats, int>();
		statDict3 = new Dictionary<Stats, int>();
	}

	public static int GetStat(Stats stat)
	{
		if (statDict.ContainsKey(stat))
		{
			int num = statDict[stat];
			int num2 = statDict3[stat] / 3;
			if (num != num2)
			{
				statDict[stat] = Mathf.Min(num, num2);
				statDict3[stat] = statDict[stat] * 3;
			}
			return statDict3[stat] / 3;
		}
		return 0;
	}

	public static void AddStat(Stats stat, int amount)
	{
		if (statDict.ContainsKey(stat))
		{
			Dictionary<Stats, int> dictionary;
			Dictionary<Stats, int> dictionary2 = (dictionary = statDict);
			Stats key;
			Stats key2 = (key = stat);
			int num = dictionary2[key];
			dictionary[key2] = num + amount;
			Dictionary<Stats, int> dictionary3;
			Dictionary<Stats, int> dictionary4 = (dictionary3 = statDict3);
			Stats key3 = (key = stat);
			num = dictionary4[key];
			dictionary3[key3] = num + amount * 3;
		}
		else
		{
			statDict.Add(stat, amount);
			statDict3.Add(stat, amount * 3);
		}
	}

	public static string GetStatsRaw()
	{
		string text = string.Empty;
		foreach (KeyValuePair<Stats, int> item in statDict3)
		{
			string text2 = text;
			text = string.Concat(text2, "#", item.Key, "-", GetStat(item.Key));
		}
		return text;
	}

	public static IEnumerator UploadStats()
	{
		if (!AccountManager.RealAccount() || DedicatedServer.isDedicated)
		{
			yield break;
		}
		if (Application.isWebPlayer)
		{
			Debug.Log("Submit Kong stats");
			if (statDict.ContainsKey(Stats.cubesBuilt))
			{
				Kongregate.SubmitStat("CubesBuilt", statDict[Stats.cubesBuilt]);
				Debug.Log("cubes" + statDict[Stats.cubesBuilt]);
			}
		}
		string statsRaw = GetStatsRaw();
		string value = Utils.Md5Sum(AccountManager.GetSessionKey() + "x34" + statsRaw);
		Clear();
		string url = "http://gamedata.cubelands.com/accounts.php";
		WWWForm wWWForm = new WWWForm();
		wWWForm.AddField("action", "uploadstats");
		wWWForm.AddField("userID", AccountManager.GetUserID());
		wWWForm.AddField("sessionKey", AccountManager.GetSessionKey());
		wWWForm.AddField("uploadStats", statsRaw);
		wWWForm.AddField("sum", value);
		WWW www = new WWW(url, wWWForm);
		yield return www;
		if (www.error != null)
		{
			Debug.LogError("ERROR: WWW call failed!");
			yield break;
		}
		string text = www.text;
		if (text == null || text == string.Empty)
		{
			Debug.LogError("ERROR: WWW had invalid return data" + text);
			yield break;
		}
		if (text.Substring(0, 1) != "1")
		{
			Debug.LogError("ERROR: WWW had invalid return data; not 1: " + text);
			yield break;
		}
		JsonData jsonData = JsonMapper.ToObject(text.Substring(1));
		if (jsonData == null)
		{
			Debug.LogError("Fetch friends failed");
			yield break;
		}
		JsonData jsonData2 = jsonData["Notifications"];
		for (int i = 0; i < jsonData2.Count; i++)
		{
			JsonData jsonData3 = jsonData2[i];
			Notifications.AddNotification((string)jsonData3["title"], (string)jsonData3["description"], NotificationTypes.Achievement);
		}
	}
}
