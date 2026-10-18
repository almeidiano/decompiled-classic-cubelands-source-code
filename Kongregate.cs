using UnityEngine;

public class Kongregate : MonoBehaviour
{
	private static Kongregate _instance;

	public static int userId;

	public static string username = string.Empty;

	public static string gameAuthToken = string.Empty;

	public static bool IsOnKongregate()
	{
		return Application.absoluteURL.IndexOf("kongregate.com") != -1;
	}

	public static void SubmitStat(string stat, int val)
	{
		Application.ExternalCall("kongregate.stats.submit", stat, val);
	}

	private void Awake()
	{
		Debug.Log("Install Kong " + Application.absoluteURL);
		if (!IsOnKongregate() || (_instance != null && _instance != this))
		{
			Object.Destroy(base.gameObject);
			return;
		}
		Object.DontDestroyOnLoad(this);
		_instance = this;
	}

	private void Start()
	{
		Application.ExternalEval("if(typeof(kongregateUnitySupport) != 'undefined'){ kongregateUnitySupport.initAPI('Kongregate', 'OnKongregateAPILoaded');};");
		Application.ExternalEval("kongregate.services.addEventListener('login', function(){   var services = kongregate.services;   var params=[services.getUserId(), services.getUsername(), services.getGameAuthToken()].join('|');   kongregateUnitySupport.getUnityObject().SendMessage('Kongregate', 'OnKongregateUserSignedIn', params);});");
	}

	public void OnKongregateAPILoaded(string userInfoString)
	{
		Debug.Log("OnKongregateAPILoaded " + userInfoString);
		string[] array = userInfoString.Split("|"[0]);
		userId = int.Parse(array[0]);
		username = array[1];
		gameAuthToken = array[2];
		if (!AccountManager.LoggedIn())
		{
			AccountManager.GuestLoggedin("Kong_" + username);
		}
	}

	public void OnKongregateUserSignedIn(string userInfoString)
	{
		Debug.Log("OnKongregateUserSignedIn " + userInfoString);
		string[] array = userInfoString.Split("|"[0]);
		userId = int.Parse(array[0]);
		username = array[1];
		gameAuthToken = array[2];
		if (!AccountManager.LoggedIn())
		{
			AccountManager.GuestLoggedin("Kong_" + username);
		}
	}
}
