using System.Collections;
using System.Text;
using UnityEngine;

public class MultiplayerFunctions : MonoBehaviour
{
	public delegate void VoidDelegate();

	public string masterserverGameName = "Cubelands";

	public int defaultServerPort = 25010;

	public int connectTimeoutValue = 30;

	public static MultiplayerFunctions SP;

	private static bool hasLoadedMasterserverSettings;

	private static HostData[] hostData;

	private bool connecting;

	private HostData lastConnectHostData;

	private string[] lastConnectIP;

	private int lastConnectPort;

	private string lastConnectPW = string.Empty;

	private float lastConnectStarted;

	private bool lastConnectUsedNAT;

	private VoidDelegate lastConnectionFailDelegate;

	private bool lastConnectMayRetry = true;

	private NetworkConnectionError lastConnectionError;

	private float lastMSRetry;

	private int msRetries;

	private float lastHostListRequest = -999f;

	private VoidDelegate currentHostListDelegate;

	private bool hasReceivedHostListResponse;

	private bool awaitingHostList;

	private static bool hasTestedNAT;

	private static bool testedUseNat;

	private void Awake()
	{
		SP = this;
	}

	public bool ReadyLoading()
	{
		if (hasLoadedMasterserverSettings)
		{
			return hasTestedNAT;
		}
		return false;
	}

	private IEnumerator SetupMasterServer()
	{
		hasLoadedMasterserverSettings = true;
		FetchHostList();
		yield return 0;
	}

	public void HostDataConnect(HostData hData, string password, bool doRetryConnection, VoidDelegate failDelegate)
	{
		lastConnectHostData = hData;
		lastConnectUsedNAT = lastConnectHostData.useNat;
		StartedConnecting();
		lastConnectMayRetry = doRetryConnection;
		if (password == string.Empty)
		{
			Network.Connect(hData);
		}
		else
		{
			Network.Connect(hData, password);
		}
		Invoke("ConnectTimeout", connectTimeoutValue);
		lastConnectIP = hData.ip;
		lastConnectPort = hData.port;
		lastConnectPW = password;
		lastConnectStarted = Time.time;
		if (failDelegate != null)
		{
			lastConnectionFailDelegate = failDelegate;
		}
		connecting = true;
	}

	public void DirectConnect(string IP, int port, string password, bool doRetryConnection, VoidDelegate failDelegate)
	{
		DirectConnect(new string[1] { IP }, port, password, doRetryConnection, failDelegate);
	}

	public void DirectConnect(string[] IP, int port, string password, bool doRetryConnection, VoidDelegate failDelegate)
	{
		StartedConnecting();
		lastConnectMayRetry = doRetryConnection;
		lastConnectUsedNAT = false;
		if (password == string.Empty)
		{
			Network.Connect(IP, port);
		}
		else
		{
			Network.Connect(IP, port, password);
		}
		Invoke("ConnectTimeout", connectTimeoutValue);
		connecting = true;
		lastConnectHostData = null;
		lastConnectIP = IP;
		lastConnectPort = port;
		lastConnectPW = password;
		lastConnectStarted = Time.time;
		if (failDelegate != null)
		{
			lastConnectionFailDelegate = failDelegate;
		}
	}

	private void StartedConnecting()
	{
		CancelInvoke("ConnectTimeout");
		connecting = true;
		lastConnectStarted = Time.realtimeSinceStartup;
	}

	public void CancelConnection()
	{
		CancelInvoke("ConnectTimeout");
		lastConnectMayRetry = false;
		connecting = false;
	}

	private void ConnectTimeout()
	{
		Debug.Log("Connect timeout");
		OnFailedToConnect(NetworkConnectionError.NoError);
	}

	private void OnFailedToConnect(NetworkConnectionError info)
	{
		CancelInvoke("ConnectTimeout");
		if (lastConnectIP != null)
		{
			Debug.Log("Failed to connect [" + lastConnectIP[0] + ":" + lastConnectPort + " ] info:" + info);
			StartCoroutine(FailedConnectRetry(info));
		}
		else
		{
			Debug.Log("Failed to connect, no data:S " + info);
			connecting = false;
		}
	}

	private void OnConnectedToServer()
	{
		CancelInvoke("ConnectTimeout");
	}

	private IEnumerator FailedConnectRetry(NetworkConnectionError info)
	{
		lastConnectionError = info;
		if (lastConnectMayRetry && info != NetworkConnectionError.TooManyConnectedPlayers && info != NetworkConnectionError.InvalidPassword && (lastConnectUsedNAT || lastConnectPort != defaultServerPort))
		{
			yield return 0;
			DirectConnect(lastConnectIP, SP.defaultServerPort, lastConnectPW, doRetryConnection: true, lastConnectionFailDelegate);
			yield break;
		}
		connecting = false;
		if (lastConnectionFailDelegate != null)
		{
			lastConnectionFailDelegate();
		}
	}

	public bool IsConnecting()
	{
		return connecting;
	}

	private void OnFailedToConnectToMasterServer(NetworkConnectionError info)
	{
		Debug.LogWarning("OnFailedToConnectToMasterServer: " + info);
		int num = 5 + 5 * msRetries * msRetries;
		if (lastMSRetry < Time.time - (float)num)
		{
			lastMSRetry = Time.time;
			FetchHostList();
		}
	}

	public void FetchHostList()
	{
		if (!hasLoadedMasterserverSettings)
		{
			Debug.LogError("Calling FetchHostList but we havent loaded MS settings yet");
		}
		else if (lastHostListRequest < Time.realtimeSinceStartup - 1f)
		{
			lastHostListRequest = Time.realtimeSinceStartup;
			MasterServer.RequestHostList(masterserverGameName);
		}
	}

	public void SetHostListDelegate(VoidDelegate newD)
	{
		currentHostListDelegate = newD;
	}

	public HostData[] GetHostData()
	{
		return hostData;
	}

	public bool HasReceivedHostList()
	{
		return hasReceivedHostListResponse;
	}

	private void OnMasterServerEvent(MasterServerEvent msEvent)
	{
		switch (msEvent)
		{
		case MasterServerEvent.HostListReceived:
			StartCoroutine(WaitForAllHostData());
			break;
		case MasterServerEvent.RegistrationFailedGameName:
		case MasterServerEvent.RegistrationFailedGameType:
		case MasterServerEvent.RegistrationFailedNoServer:
			Debug.Log("Masterserver error: " + msEvent);
			break;
		case MasterServerEvent.RegistrationSucceeded:
			break;
		}
	}

	private IEnumerator WaitForAllHostData()
	{
		if (awaitingHostList)
		{
			yield break;
		}
		awaitingHostList = true;
		hostData = MasterServer.PollHostList();
		while (true)
		{
			yield return new WaitForSeconds(0.2f);
			if (MasterServer.PollHostList().Length == hostData.Length)
			{
				break;
			}
			hostData = MasterServer.PollHostList();
		}
		if (currentHostListDelegate != null)
		{
			currentHostListDelegate();
		}
		hasReceivedHostListResponse = true;
		awaitingHostList = false;
	}

	public void StartServer(string password, int port, int maxConnections, bool enableSecurity)
	{
		if (password != string.Empty)
		{
			Network.incomingPassword = password;
		}
		if (enableSecurity)
		{
			Network.InitializeSecurity();
		}
		Network.InitializeServer(maxConnections, port, GetNATStatus());
	}

	public void RegisterServer(string serverTitle, string comment)
	{
		if (!Network.isServer)
		{
			Debug.LogError("RegisterServer: is not a server!");
			return;
		}
		Debug.Log("RegisterServer - MS");
		MasterServer.RegisterHost(masterserverGameName, serverTitle, comment);
	}

	public void UnRegisterServer()
	{
		if (!Network.isServer)
		{
			Debug.LogError("RegisterServer: is not a server!");
			return;
		}
		Debug.Log("UN-RegisterServer - MS");
		MasterServer.UnregisterHost();
	}

	public bool GetNATStatus()
	{
		if (!hasTestedNAT)
		{
			Debug.LogWarning("Calling GetNATStatus, but we havent finished testing yet!");
		}
		return testedUseNat;
	}

	private IEnumerator TestConnection()
	{
		if (hasTestedNAT)
		{
			yield break;
		}
		while (!hasLoadedMasterserverSettings)
		{
			yield return 0;
		}
		testedUseNat = !Network.HavePublicAddress();
		ConnectionTesterStatus connectionTestResult = ConnectionTesterStatus.Undetermined;
		float timeoutAt = Time.realtimeSinceStartup + 10f;
		float timer = 0f;
		bool probingPublicIP = false;
		string testMessage = string.Empty;
		while (!hasTestedNAT)
		{
			yield return 0;
			if (Time.realtimeSinceStartup >= timeoutAt)
			{
				Debug.LogWarning("TestConnect NAT test aborted; timeout");
				break;
			}
			connectionTestResult = Network.TestConnection();
			switch (connectionTestResult)
			{
			case ConnectionTesterStatus.Error:
				testMessage = "Problem determining NAT capabilities";
				hasTestedNAT = true;
				break;
			case ConnectionTesterStatus.Undetermined:
				testMessage = "Undetermined NAT capabilities";
				hasTestedNAT = false;
				break;
			case ConnectionTesterStatus.PublicIPIsConnectable:
				testMessage = "Directly connectable public IP address.";
				testedUseNat = false;
				hasTestedNAT = true;
				break;
			case ConnectionTesterStatus.PublicIPPortBlocked:
				testMessage = "Non-connectble public IP address (port " + defaultServerPort + " blocked), running a server is impossible.";
				hasTestedNAT = false;
				if (!probingPublicIP)
				{
					Debug.Log("Testing if firewall can be circumvented");
					connectionTestResult = Network.TestConnectionNAT();
					probingPublicIP = true;
					timer = Time.time + 10f;
				}
				else if (Time.time > timer)
				{
					probingPublicIP = false;
					testedUseNat = true;
					hasTestedNAT = true;
				}
				break;
			case ConnectionTesterStatus.PublicIPNoServerStarted:
				testMessage = "Public IP address but server not initialized, it must be started to check server accessibility. Restart connection test when ready.";
				break;
			case ConnectionTesterStatus.LimitedNATPunchthroughPortRestricted:
				testMessage = "Limited NAT punchthrough capabilities. Cannot connect to all types of NAT servers. Running a server is ill adviced as not everyone can connect.";
				testedUseNat = true;
				hasTestedNAT = true;
				break;
			case ConnectionTesterStatus.LimitedNATPunchthroughSymmetric:
				testMessage = "Limited NAT punchthrough capabilities. Cannot connect to all types of NAT servers. Running a server is ill adviced as not everyone can connect.";
				testedUseNat = true;
				hasTestedNAT = true;
				break;
			case ConnectionTesterStatus.NATpunchthroughFullCone:
			case ConnectionTesterStatus.NATpunchthroughAddressRestrictedCone:
				testMessage = "NAT punchthrough capable. Can connect to all servers and receive connections from all clients. Enabling NAT punchthrough functionality.";
				testedUseNat = true;
				hasTestedNAT = true;
				break;
			default:
				testMessage = "Error in test routine, got " + connectionTestResult;
				break;
			}
		}
		hasTestedNAT = true;
		Debug.Log(string.Concat("TestConnection result: testedUseNat=", testedUseNat, " connectionTestResult=", connectionTestResult, " probingPublicIP=", probingPublicIP, " hasTestedNAT=", hasTestedNAT, " testMessage=", testMessage));
	}

	public string ConnectingToAddress()
	{
		return lastConnectIP[0] + ":" + lastConnectPort;
	}

	public string[] LastIP()
	{
		return lastConnectIP;
	}

	public int LastPort()
	{
		return lastConnectPort;
	}

	public NetworkConnectionError LastConnectionError()
	{
		return lastConnectionError;
	}

	public float TimeSinceLastConnect()
	{
		return Time.realtimeSinceStartup - lastConnectStarted;
	}

	public static byte[] StringToBytes(string str)
	{
		return Encoding.UTF8.GetBytes(str);
	}

	public static string BytesToString(byte[] by)
	{
		return Encoding.UTF8.GetString(by);
	}
}
