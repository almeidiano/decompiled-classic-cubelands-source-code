using System.Collections;
using UnityEngine;

public class AccountGUI : MonoBehaviour
{
	private bool showGUI;

	private Rect errorWin;

	private Rect popupWindowRect;

	public Texture2D registerImage;

	public static AccountGUI SP;

	private string accountMenu = string.Empty;

	private string loginUsernameInput = string.Empty;

	private string loginPasswordInput = string.Empty;

	private string loginStatus = string.Empty;

	private string[] registerStrings = new string[4];

	private string registerStatus = string.Empty;

	private bool loggingOut;

	private bool loggingIn;

	private bool autoLogin = true;

	private bool tryingWebLogin;

	private void Awake()
	{
		SP = this;
		if (Application.isWebPlayer || Application.isEditor)
		{
			SetNewWindowHeight(230);
		}
		else
		{
			SetNewWindowHeight(260);
		}
		loginUsernameInput = PlayerPrefs.GetString("LoginUsername" + Application.platform, string.Empty);
		registerStrings[0] = (registerStrings[1] = (registerStrings[2] = (registerStrings[3] = string.Empty)));
	}

	private IEnumerator Start()
	{
		if (Utils.IsCubelandsHosted())
		{
			Application.ExternalEval("if(window.location.host.indexOf(\"classic.cubelands.com\")==0){ RequestCubelandsLogin();}else{GetUnity().SendMessage(\"MainMenu\", \"ContinueWebSession\", \"0#0#0\");}");
			tryingWebLogin = true;
		}
		while (tryingWebLogin && Time.realtimeSinceStartup <= 2f)
		{
			yield return 0;
		}
		if (!AccountManager.LoggedIn() && !DedicatedServer.isDedicated)
		{
			string @string = PlayerPrefs.GetString("LoginUsername" + Application.platform, string.Empty);
			string string2 = PlayerPrefs.GetString("autoLoginPass" + Application.platform, string.Empty);
			if (@string != string.Empty && string2 != string.Empty)
			{
				StartCoroutine(DoLogin(@string, string2, autoLogin: true));
			}
			else if (Kongregate.IsOnKongregate())
			{
				Debug.Log("Guest login Kong=" + Kongregate.username);
				if (Kongregate.username != string.Empty)
				{
					AccountManager.GuestLoggedin("Kong_" + Kongregate.username);
				}
			}
		}
		InvokeRepeating("Ping", 0f, 299f);
	}

	private void SwitchPage(string newpage)
	{
		if (newpage == "register")
		{
			SetNewWindowHeight(450);
		}
		accountMenu = newpage;
		MainMenu.SP.PlayClickSound();
	}

	private void SetNewWindowHeight(int newHeight)
	{
		int num = 350;
		Vector3 vector = new Vector3(Screen.width / 2 - num / 2, Screen.height / 2 - newHeight / 2, 0f);
		popupWindowRect = new Rect(vector.x, vector.y, num, newHeight);
		errorWin = new Rect(vector.x, vector.y, num, newHeight);
	}

	public void ShowGUI()
	{
		if (loginStatus != string.Empty)
		{
			errorWin = GUI.Window(100, errorWin, LoginErrorWindow, "Login error");
		}
		else
		{
			popupWindowRect = GUI.Window(21, popupWindowRect, ShowAccountGUI, string.Empty);
		}
	}

	public void Show()
	{
		showGUI = true;
	}

	public void Hide()
	{
		showGUI = false;
	}

	private void Ping()
	{
		StartCoroutine(DoPing());
	}

	private IEnumerator DoPing()
	{
		if (AccountManager.RealAccount())
		{
			IENumeratorOutput output = new IENumeratorOutput();
			yield return StartCoroutine(AccountManager.Ping(output));
			if (output.Failed() && (int)output.GetOutput() == 2)
			{
				StartCoroutine(DoLogout(wipeAutoLogin: false));
			}
		}
	}

	private IEnumerator ContinueWebSession(string data)
	{
		Debug.Log("ContinueWebSession: " + data);
		if (data.Length <= 10)
		{
			Debug.Log("ContinueWebSession: abort 1");
			tryingWebLogin = false;
			yield break;
		}
		string[] array = data.Split('#');
		int num = int.Parse(array[0]);
		string text = array[1];
		string text2 = array[2];
		if (text == null || text == string.Empty || text2 == null || text2 == string.Empty)
		{
			Debug.Log("ContinueWebSession: abort 2");
			tryingWebLogin = false;
			yield break;
		}
		if (!AccountManager.LoggedIn() || AccountManager.GetUserID() != num)
		{
			yield return StartCoroutine(DoWebLogin(num, text, text2));
		}
		tryingWebLogin = false;
	}

	public void ShowAccountGUI(int windowID)
	{
		if (!AccountManager.LoggedIn())
		{
			if (accountMenu == "forgot")
			{
				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Back to login", GUILayout.MaxWidth(200f)))
				{
					SwitchPage(string.Empty);
				}
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
				GUILayout.Space(15f);
				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				GUILayout.Label("Register", "Label_Header");
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				GUILayout.Label("You can recover your password on Cubelands.com.");
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Recover my account", GUILayout.MaxWidth(150f)))
				{
					MainMenu.SP.PlayClickSound();
					Application.OpenURL("http://classic.cubelands.com/?page=forgotpassword");
				}
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
			}
			else if (accountMenu == "register")
			{
				int num = 60;
				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Back to login", GUILayout.MaxWidth(200f)))
				{
					SetNewWindowHeight(220);
					SwitchPage(string.Empty);
				}
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
				GUILayout.Space(5f);
				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				GUILayout.Label("Register", "Label_Header");
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal();
				GUILayout.Space(num);
				GUILayout.Label("Playing Cubelands is free, we only require you to register a username!");
				GUILayout.Space(num);
				GUILayout.EndHorizontal();
				GUILayout.Space(5f);
				GUILayout.BeginHorizontal();
				GUILayout.Space(num);
				GUILayout.Label("Username");
				registerStrings[0] = GUILayout.TextField(registerStrings[0], GUILayout.Width(150f));
				GUILayout.Space(num);
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal();
				GUILayout.Space(num);
				if (registerStrings[0].Length >= 2)
				{
					GUILayout.Label("Email");
					registerStrings[1] = GUILayout.TextField(registerStrings[1], GUILayout.Width(150f));
				}
				else
				{
					GUILayout.Label(string.Empty);
				}
				GUILayout.Space(num);
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal();
				GUILayout.Space(num);
				if (registerStrings[1].Length >= 6)
				{
					GUILayout.Label("Password (min 5, max 40 chars)");
					registerStrings[2] = PasswordField(registerStrings[2], "*", GUILayout.Width(150f));
				}
				else
				{
					GUILayout.Label(string.Empty);
				}
				GUILayout.Space(num);
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal();
				GUILayout.Space(num);
				if (registerStrings[2].Length >= 5)
				{
					GUILayout.Label("Repeat password");
					registerStrings[3] = PasswordField(registerStrings[3], "*", GUILayout.Width(150f));
				}
				else
				{
					GUILayout.Label(string.Empty);
				}
				GUILayout.Space(num);
				GUILayout.EndHorizontal();
				if (registerStrings[3].Length >= 5 && registerStrings[3] == registerStrings[2])
				{
					GUILayout.Space(5f);
					GUILayout.BeginHorizontal();
					GUILayout.FlexibleSpace();
					if (GUILayout.Button("Register", GUILayout.MaxWidth(150f)))
					{
						MainMenu.SP.PlayClickSound();
						StartCoroutine(DoRegister(registerStrings[0], registerStrings[1], registerStrings[2], registerStrings[3]));
					}
					GUILayout.FlexibleSpace();
					GUILayout.EndHorizontal();
				}
				else
				{
					GUILayout.Label(string.Empty);
				}
				GUILayout.BeginHorizontal();
				GUILayout.Space(num);
				GUILayout.Label(registerStatus);
				GUILayout.Space(num);
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				GUILayout.BeginVertical();
				GUILayout.Label("Fun awaits you in any of our amazing worlds!", "Label_Header2");
				GUILayout.Label(registerImage);
				GUILayout.EndVertical();
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
			}
			else
			{
				GUILayout.BeginArea(new Rect(30f, 30f, 250f, 300f));
				GUILayout.Label("Login", "Label_Header");
				GUILayout.BeginHorizontal();
				GUILayout.Label("Username");
				loginUsernameInput = GUILayout.TextField(loginUsernameInput, GUILayout.Width(150f));
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal();
				GUILayout.Label("Password");
				loginPasswordInput = PasswordField(loginPasswordInput, "*", GUILayout.Width(150f));
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal();
				autoLogin = GUILayout.Toggle(autoLogin, "Auto login");
				GUILayout.FlexibleSpace();
				if (loggingIn)
				{
					GUILayout.Label("Logging in...", GUILayout.MaxWidth(150f));
				}
				else if (GUILayout.Button("Login", GUILayout.MaxWidth(150f)))
				{
					MainMenu.SP.PlayClickSound();
					StartCoroutine(DoLogin(loginUsernameInput, Utils.Md5Sum(loginPasswordInput.ToLower() + "mySEED"), autoLogin));
				}
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Forgot password?", GUILayout.MaxWidth(150f)))
				{
					SwitchPage("forgot");
				}
				GUILayout.EndHorizontal();
				GUILayout.Space(10f);
				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Legal", GUILayout.MaxWidth(80f)))
				{
					MainMenu.SP.PlayClickSound();
					Application.OpenURL("http://classic.cubelands.com/?page=legal");
				}
				if (GUILayout.Button("Privacy", GUILayout.MaxWidth(80f)))
				{
					MainMenu.SP.PlayClickSound();
					Application.OpenURL("http://classic.cubelands.com/?page=privacy");
				}
				if (GUILayout.Button("Terms", GUILayout.MaxWidth(80f)))
				{
					MainMenu.SP.PlayClickSound();
					Application.OpenURL("http://classic.cubelands.com/?page=terms");
				}
				GUILayout.Label(loginStatus);
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
				GUILayout.Label(loginStatus);
				GUILayout.EndArea();
				if (!Application.isEditor && !Application.isWebPlayer && GUI.Button(new Rect(126f, 210f, 100f, 20f), "Quit game"))
				{
					Application.Quit();
				}
			}
		}
		else
		{
			GUILayout.Label("Welcome " + AccountManager.GetUsername() + "!");
			GUILayout.Space(20f);
			if (GUILayout.Button("Close screen"))
			{
				showGUI = !showGUI;
			}
			GUILayout.Space(50f);
			if (loggingOut)
			{
				GUILayout.Label("Logging out..");
			}
			else if (GUILayout.Button("Logout"))
			{
				MainMenu.SP.PlayClickSound();
				StartCoroutine(DoLogout(wipeAutoLogin: true));
			}
		}
	}

	private IEnumerator DoRegister(string user, string email, string pass1, string pass2)
	{
		if (pass1 != pass2)
		{
			registerStatus = "Registration failed: The two passwords you entered did not match!";
			yield break;
		}
		registerStatus = "Registering...";
		IENumeratorOutput output = new IENumeratorOutput();
		yield return StartCoroutine(AccountManager.Register(output, user, email, pass1));
		if (output.Failed())
		{
			registerStatus = "It is not possible to register a new account at the moment, because a new version is under development. Please come back...";
			Debug.Log("Account server overloaded or down?");
			yield break;
		}
		string text = (string)output.GetOutput();
		if (text == "succes")
		{
			PlayerPrefs.SetString("LoginUsername" + Application.platform, user);
			PlayerPrefs.SetString("autoLoginPass" + Application.platform, Utils.Md5Sum(pass1.ToLower() + "mySEED"));
			registerStatus = string.Empty;
			SwitchPage(string.Empty);
		}
		else
		{
			registerStatus = "Registration failed: " + text;
		}
	}

	private IEnumerator DoLogin(string user, string pass, bool autoLogin)
	{
		IENumeratorOutput output = new IENumeratorOutput();
		yield return StartCoroutine(AccountManager.Login(output, user, ""));
		if (output.Failed())
		{
			loginStatus = "Login failed: The server was \n unable to process the login request.";
			Debug.Log("Login server overloaded or down?");
		}
		else
		{
			switch ((string)output.GetOutput())
			{
			case "succes":
				PlayerPrefs.SetString("LoginUsername" + Application.platform, user);
				loginStatus = string.Empty;
				showGUI = false;
				break;
			case "wrongpassword":
				loginStatus = "Login failed: Wrong email and \n password combination.";
				break;
			case "buygame":
				loginStatus = "Sorry; you need to buy the game \n to use the standalone player.";
				StartCoroutine(DoLogout(wipeAutoLogin: false));
				break;
			default:
				loginStatus = "Login failed: This account is still \n logged in somewhere, wait for it to logout (max 10 minutes).";
				break;
			}
		}
		loggingIn = false;
	}

	private IEnumerator DoWebLogin(int webUserID, string webSessionKey, string passwordPart)
	{
		Debug.Log("DoWebLogin!");
		loginStatus = "Logging in...";
		loggingIn = true;
		IENumeratorOutput output = new IENumeratorOutput();
		yield return StartCoroutine(AccountManager.WebLogin(output, webUserID, webSessionKey, passwordPart));
		if (output.Failed())
		{
			loginStatus = "Login failed: The server was unable \n to process the login request.";
			Debug.Log("Login server overloaded or down?");
		}
		else
		{
			string text = (string)output.GetOutput();
			if (text == "succes")
			{
				loginStatus = string.Empty;
				showGUI = false;
			}
			Debug.Log("DoWebLogin: " + text);
		}
		loggingIn = false;
	}

	public IEnumerator DoLogout(bool wipeAutoLogin)
	{
		loggingOut = true;
		IENumeratorOutput output = new IENumeratorOutput();
		yield return StartCoroutine(AccountManager.Logout(output));
		if (output.Failed())
		{
			Debug.Log("DoLogout failed");
		}
		if (wipeAutoLogin)
		{
			PlayerPrefs.SetString("autoLoginPass" + Application.platform, string.Empty);
		}
		loggingOut = false;
	}

	private IEnumerator OnApplicationQuit()
	{
		if (AccountManager.RealAccount())
		{
			yield return StartCoroutine(Statistics.UploadStats());
			yield return StartCoroutine(DoLogout(wipeAutoLogin: false));
		}
	}

	private string PasswordField(string password, string maskChar, GUILayoutOption gOpti)
	{
		string empty = string.Empty;
		if (Event.current.type == EventType.Repaint || Event.current.type == EventType.MouseDown)
		{
			empty = string.Empty;
			for (int i = 0; i < password.Length; i++)
			{
				empty += maskChar;
			}
		}
		else
		{
			empty = password;
		}
		GUI.changed = false;
		empty = GUILayout.TextField(empty, gOpti);
		if (GUI.changed)
		{
			password = empty;
		}
		return password;
	}

	private void LoginErrorWindow(int winID)
	{
		if (GUI.Button(new Rect(20f, 35f, errorWin.width - 40f, 40f), loginStatus))
		{
			loginStatus = string.Empty;
		}
	}
}
