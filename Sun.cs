using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Sun : MonoBehaviour
{
	public static Sun SP;

	public Material customSkyboxMat;

	public Material defaultSkyboxMat;

	private Vector3 rotateDirection = Vector3.zero;

	private float rotationSpeed = 1f;

	private Material skyboxMaterial;

	private Material skyboxMaterialNight;

	private Material skyboxMaterialLerp;

	private Color dayLightAmbient;

	private Color nightLightAmbient;

	private Color skyboxAmbient;

	private Color skyboxAmbientNight;

	private Color normalFogColor;

	private Light sunLight;

	private Flare flareCache;

	private bool customSkybox;

	private bool hasSetup;

	private Transform trans;

	private float lastPercentage = -1f;

	private bool wasDown;

	private bool clientWaitSunUp;

	private bool skipOneSunCheck;

	private List<byte[]> skyboxBytes;

	private void Awake()
	{
		trans = base.transform;
		SP = this;
		if (Network.isServer)
		{
			SetupSun();
		}
	}

	private Texture2D loadSkyboxTexture(string path)
	{
		byte[] data = File.ReadAllBytes(path);
		Texture2D texture2D = new Texture2D(512, 512);
		texture2D.LoadImage(data);
		return texture2D;
	}

	public void SetupSun()
	{
		skyboxMaterial = (RenderSettings.skybox = defaultSkyboxMat);
		trans.localEulerAngles = GameSettings.sunRotation;
		rotationSpeed = GameSettings.rotateSun_speed;
		rotateDirection = GameSettings.rotateSun_Direction;
		skyboxAmbient = GameSettings.skyboxColor;
		skyboxAmbientNight = GameSettings.skyboxColorNight;
		dayLightAmbient = GameSettings.ambientColor;
		nightLightAmbient = GameSettings.ambientColorNight;
		normalFogColor = GameSettings.fogColor;
		sunLight = base.light;
		flareCache = sunLight.flare;
		float lightning = Mathf.Clamp(Mathf.Abs(trans.forward.y), 0f, 1f);
		SetLightning(lightning);
		if (Network.isServer && GameSettings.customSkyboxFolder != string.Empty)
		{
			customSkybox = ServerLoadNCacheSkybox();
		}
		if (Network.isClient)
		{
			base.networkView.RPC("AskForCustomSkybox", RPCMode.Server);
		}
		hasSetup = true;
	}

	private void Update()
	{
		if (!GameSettings.rotateSun || !hasSetup || clientWaitSunUp)
		{
			return;
		}
		bool flag = !(trans.forward.y <= 0f);
		if (!wasDown && flag)
		{
			wasDown = flag;
		}
		else if (wasDown && !flag)
		{
			wasDown = flag;
			if (Network.isServer)
			{
				base.networkView.RPC("SunUp", RPCMode.All, GetRotation());
			}
			else if (Network.isClient)
			{
				if (!skipOneSunCheck)
				{
					clientWaitSunUp = true;
					return;
				}
				skipOneSunCheck = false;
			}
		}
		float num = 1f;
		if (flag)
		{
			num += trans.forward.y * 7f;
		}
		else if (!flag)
		{
			num -= Mathf.Abs(trans.forward.y) * 0.25f;
		}
		trans.RotateAroundLocal(rotateDirection, Time.deltaTime * rotationSpeed);
		if (!DedicatedServer.isDedicated)
		{
			float num2 = 0f;
			if (trans.forward.y < 0f)
			{
				num2 = Mathf.Clamp(Mathf.Abs(trans.forward.y), 0f, 1f);
			}
			if (lastPercentage == -1f || !(Mathf.Abs(lastPercentage - num2) <= 0.01f))
			{
				lastPercentage = num2;
				SetLightning(lastPercentage);
			}
		}
	}

	private void SetLightning(float lightPercentage)
	{
		float b = 0.15f;
		RenderSettings.ambientLight = new Color(Mathf.Max(dayLightAmbient.r * lightPercentage, b), Mathf.Max(dayLightAmbient.g * lightPercentage, b), Mathf.Max(dayLightAmbient.b * lightPercentage, b), 255f);
		skyboxMaterial.SetColor("_Tint", new Color(skyboxAmbient.r * lightPercentage, skyboxAmbient.g * lightPercentage, skyboxAmbient.b * lightPercentage, 255f));
		float b2 = 0.1f;
		RenderSettings.fogColor = new Color(Mathf.Max(normalFogColor.r * lightPercentage, b2), Mathf.Max(normalFogColor.g * lightPercentage, b2), Mathf.Max(normalFogColor.b * lightPercentage, b2), 255f);
	}

	public bool IsBlocked()
	{
		return sunLight.flare == null;
	}

	public void SetFlare(bool enabled)
	{
		if (hasSetup)
		{
			if (enabled)
			{
				sunLight.flare = flareCache;
			}
			else
			{
				sunLight.flare = null;
			}
		}
	}

	public Vector3 GetRotation()
	{
		return trans.localEulerAngles;
	}

	public Vector3 GetForward()
	{
		return trans.forward;
	}

	[RPC]
	public void SunUp(Vector3 serverRot)
	{
		if (!clientWaitSunUp)
		{
			skipOneSunCheck = true;
		}
		clientWaitSunUp = false;
		SetServerRotation(serverRot);
	}

	public void SetServerRotation(Vector3 rott)
	{
		trans.localEulerAngles = rott;
	}

	private bool ServerLoadNCacheSkybox()
	{
		skyboxBytes = new List<byte[]>();
		Debug.Log("ServerLoadNCacheSkybox=" + GameSettings.customSkyboxFolder);
		if (GameSettings.customSkyboxFolder == string.Empty)
		{
			return false;
		}
		if (!Directory.Exists("Mods/" + GameSettings.customSkyboxFolder))
		{
			Debug.LogError("Couldn't find customSkyboxFolder: Mods/" + GameSettings.customSkyboxFolder);
			return false;
		}
		for (int i = 1; i <= 6; i++)
		{
			byte[] array = null;
			if (File.Exists("Mods/" + GameSettings.customSkyboxFolder + "/" + i + ".png"))
			{
				array = File.ReadAllBytes("Mods/" + GameSettings.customSkyboxFolder + "/" + i + ".png");
			}
			else if (File.Exists("Mods/" + GameSettings.customSkyboxFolder + "/" + i + ".jpg"))
			{
				array = File.ReadAllBytes("Mods/" + GameSettings.customSkyboxFolder + "/" + i + ".jpg");
			}
			if (array == null)
			{
				return false;
			}
			skyboxBytes.Add(array);
		}
		if (!DedicatedServer.isDedicated)
		{
			List<Texture2D> list = new List<Texture2D>();
			foreach (byte[] skyboxByte in skyboxBytes)
			{
				Texture2D texture2D = new Texture2D(512, 512);
				texture2D.LoadImage(skyboxByte);
				list.Add(texture2D);
			}
			skyboxMaterial = (RenderSettings.skybox = customSkyboxMat);
			skyboxMaterial.SetTexture("_FrontTex", list[0]);
			skyboxMaterial.SetTexture("_BackTex", list[1]);
			skyboxMaterial.SetTexture("_LeftTex", list[2]);
			skyboxMaterial.SetTexture("_RightTex", list[3]);
			skyboxMaterial.SetTexture("_UpTex", list[4]);
			skyboxMaterial.SetTexture("_DownTex", list[5]);
		}
		for (int j = 0; j <= 5; j++)
		{
			skyboxBytes[j] = Zipper.ZipBytes(skyboxBytes[j]);
		}
		return true;
	}

	[RPC]
	private void AskForCustomSkybox(NetworkMessageInfo info)
	{
		if (customSkybox)
		{
			base.networkView.RPC("ReceiveSkybox", info.sender, skyboxBytes[0], skyboxBytes[1], skyboxBytes[2], skyboxBytes[3], skyboxBytes[4], skyboxBytes[5]);
		}
	}

	[RPC]
	private void ReceiveSkybox(byte[] text1, byte[] text2, byte[] text3, byte[] text4, byte[] text5, byte[] text6)
	{
		skyboxMaterial = (RenderSettings.skybox = customSkyboxMat);
		List<byte[]> list = new List<byte[]>();
		list.Add(Zipper.UnZipBytes(text1));
		list.Add(Zipper.UnZipBytes(text2));
		list.Add(Zipper.UnZipBytes(text3));
		list.Add(Zipper.UnZipBytes(text4));
		list.Add(Zipper.UnZipBytes(text5));
		list.Add(Zipper.UnZipBytes(text6));
		List<Texture2D> list2 = new List<Texture2D>();
		for (int i = 0; i <= 5; i++)
		{
			Texture2D texture2D = new Texture2D(512, 512);
			texture2D.LoadImage(list[i]);
			list2.Add(texture2D);
		}
		skyboxMaterial.SetTexture("_FrontTex", list2[0]);
		skyboxMaterial.SetTexture("_BackTex", list2[1]);
		skyboxMaterial.SetTexture("_LeftTex", list2[2]);
		skyboxMaterial.SetTexture("_RightTex", list2[3]);
		skyboxMaterial.SetTexture("_UpTex", list2[4]);
		skyboxMaterial.SetTexture("_DownTex", list2[5]);
	}
}
