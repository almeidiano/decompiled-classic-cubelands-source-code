using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class WorldData : MonoBehaviour
{
	public struct Chunk
	{
		public int x;

		public int y;

		public int z;

		public int chunkSize;

		public Chunk(int x, int y, int z, int size)
		{
			this.x = x;
			this.y = y;
			this.z = z;
			chunkSize = size;
		}
	}

	private Dictionary<ushort, Dictionary<ushort, Dictionary<ushort, BlockInstance>>> xWorldData = new Dictionary<ushort, Dictionary<ushort, Dictionary<ushort, BlockInstance>>>();

	public static WorldData SP;

	public Transform physicsBlockPrefab;

	public Material terrainMaterial;

	public Texture2D terrainTexture;

	private byte[] terrainTexturePNG;

	private Dictionary<string, GameObject> combinedAreas;

	private Dictionary<string, int> combinedAreaSize;

	private Dictionary<int, BlockData> blockDataHashtable;

	private int worldSizeX = 128;

	private int worldSizeZ = 128;

	private int worldHeight = 64;

	private int worldFillInHeight = 30;

	private Transform localPlayerTransform;

	public AudioClip digSound;

	public AudioClip createSound;

	private Transform thisTrans;

	public Shader terrainShaderLow;

	private Shader terrainShaderNormal;

	public BlockSoundTypeSounds[] blockSoundTypes;

	private Dictionary<int, Rect> terrainTextureUVDict = new Dictionary<int, Rect>();

	private Dictionary<int, Texture2D> terrainTexturesMinis = new Dictionary<int, Texture2D>();

	public Texture2D loadingCubeImage;

	private string customTerrainData = string.Empty;

	private bool customTerrain;

	private bool downloadedLevel;

	private bool isSaving;

	private Vector3 spawnpoint = Vector3.zero;

	private List<Vector3> changeAreaQueue = new List<Vector3>();

	private Vector3 lastCullingPos;

	private bool workingTerrain;

	private Hashtable physicBlocks = new Hashtable();

	private float lastDisable;

	private Chunk lastClientChunk;

	private List<Chunk> clientRegisteredChunksRequests = new List<Chunk>();

	private Dictionary<Chunk, int> receivedChunkParts = new Dictionary<Chunk, int>();

	private Chunk clientWaitingForChunk;

	private List<Chunk> clientRegisteredChunksAccepted = new List<Chunk>();

	private Dictionary<Chunk, List<PlayerNode>> serverChunkRegistrations = new Dictionary<Chunk, List<PlayerNode>>();

	private int networkChunkSize = 32;

	private int chunkAddDistance = 6;

	private int chunkRemoveDistance = 9;

	public BlockData GetBlockData(int blockType)
	{
		return blockDataHashtable[blockType];
	}

	public int GetBlockNr(int i)
	{
		int num = 0;
		foreach (KeyValuePair<int, BlockData> item in blockDataHashtable)
		{
			if (i <= num)
			{
				return item.Key;
			}
			num++;
		}
		return 1;
	}

	private void Awake()
	{
		terrainShaderNormal = Shader.Find("Specular");
		terrainShaderLow = Shader.Find("Diffuse Fast");
		SP = this;
		combinedAreas = new Dictionary<string, GameObject>();
		combinedAreaSize = new Dictionary<string, int>();
		blockDataHashtable = new Dictionary<int, BlockData>(50);
		thisTrans = base.transform;
	}

	private void Start()
	{
		StartCoroutine(GenerateWorld());
	}

	private IEnumerator GenerateWorld()
	{
		float startedTime2 = Time.realtimeSinceStartup;
		if (Network.isClient)
		{
			GameMenu.SP.SetLoadingStatus("Awaiting level download. Time: ");
			while (!downloadedLevel)
			{
				yield return 0;
			}
		}
		else
		{
			if (!LoadCustomTerrainTexture())
			{
				LoadDefaultTerrain();
			}
			string text = string.Empty;
			if (!Utils.IsWebplayer())
			{
				Debug.Log("Attempting to read level file: " + GameSettings.mapname + ".txt");
				FileInfo fileInfo = new FileInfo("Worlds/" + GameSettings.mapname + "/" + GameSettings.mapname + ".txt");
				if (fileInfo.Exists)
				{
					StreamReader streamReader = fileInfo.OpenText();
					text = streamReader.ReadToEnd();
					streamReader.Close();
				}
			}
			else
			{
				text = PlayerPrefs.GetString(GameSettings.mapname, string.Empty);
			}
			if (text != string.Empty)
			{
				text = Zipper.UnzipString(text);
				GameMenu.SP.SetLoadingStatus("Loading savefile");
				string[] array = text.Split('\n');
				string line = array[0] + "\n" + array[1];
				ImportSaveFileHeader(line);
				for (int i = 2; i < array.Length; i++)
				{
					line = array[i];
					if (line != string.Empty)
					{
						ImportSaveFileLine(line);
					}
				}
			}
			else
			{
				Debug.Log("Couldn't find save data, generating new world.");
				GameMenu.SP.SetLoadingStatus("Creating new empty world");
				CreateEmptyWorld();
				yield return 0;
			}
		}
		float loadTime = Time.realtimeSinceStartup - startedTime2;
		startedTime2 = Time.realtimeSinceStartup;
		GameMenu.SP.SetLoadingStatus("Building world");
		GameMenu.SP.SetLoadingStatus("Clearing spawnpoint");
		Vector3 spawnPos = GetSpawnpoint();
		if (ContainsBlock((ushort)spawnPos.x, (ushort)spawnPos.y, (ushort)spawnPos.z))
		{
			StartCoroutine(DestroyBlock((ushort)spawnPos.x, (ushort)spawnPos.y, (ushort)spawnPos.z));
		}
		if (ContainsBlock((ushort)spawnPos.x, (ushort)(spawnPos.y - 1f), (ushort)spawnPos.z))
		{
			StartCoroutine(DestroyBlock((ushort)spawnPos.x, (ushort)(spawnPos.y - 1f), (ushort)spawnPos.z));
		}
		if (!DedicatedServer.isDedicated)
		{
			GameMenu.SP.SetLoadingStatus("Loading localplayer");
			while (GameManager.SP.localPlayerNode == null)
			{
				yield return new WaitForSeconds(0.1f);
			}
			GameMenu.SP.SetLoadingStatus("Loading spawnpoint chunks..");
			Chunk currentChunk = GetChunkForWorldPos((int)spawnPos.x, (int)spawnPos.y, (int)spawnPos.z);
			ClientAddChunk(currentChunk);
			ClientAddChunk(new Chunk(currentChunk.x, currentChunk.y - 1, currentChunk.z, networkChunkSize));
			ClientAddChunk(new Chunk(currentChunk.x + 1, currentChunk.y, currentChunk.z, networkChunkSize));
			ClientAddChunk(new Chunk(currentChunk.x - 1, currentChunk.y, currentChunk.z, networkChunkSize));
			ClientAddChunk(new Chunk(currentChunk.x, currentChunk.y, currentChunk.z + 1, networkChunkSize));
			ClientAddChunk(new Chunk(currentChunk.x, currentChunk.y, currentChunk.z - 1, networkChunkSize));
			ClientAddChunk(new Chunk(currentChunk.x, currentChunk.y + 1, currentChunk.z, networkChunkSize));
			ClientAddChunk(new Chunk(currentChunk.x + 1, currentChunk.y, currentChunk.z + 1, networkChunkSize));
			ClientAddChunk(new Chunk(currentChunk.x + 1, currentChunk.y, currentChunk.z - 1, networkChunkSize));
			ClientAddChunk(new Chunk(currentChunk.x - 1, currentChunk.y, currentChunk.z + 1, networkChunkSize));
			ClientAddChunk(new Chunk(currentChunk.x - 1, currentChunk.y, currentChunk.z - 1, networkChunkSize));
			while (!clientRegisteredChunksAccepted.Contains(currentChunk))
			{
				yield return new WaitForSeconds(0.1f);
			}
		}
		float num = Time.realtimeSinceStartup - startedTime2;
		GameMenu.SP.SetLoadingStatus("Finished creating world. Load time: " + loadTime + " Generate time:" + num);
		Debug.Log("Finished creating world. Load time: " + loadTime + " Generate time:" + num);
		yield return 0;
		yield return 0;
		GameManager.SP.SetFinishedLoading();
	}

	private void LoadDefaultTerrain()
	{
		TextAsset textAsset = (TextAsset)Resources.Load("defaultImageUVs", typeof(TextAsset));
		ImportTerrainUVDataString(textAsset.text);
		TextAsset textAsset2 = (TextAsset)Resources.Load("defaultBlockData", typeof(TextAsset));
		GenerateBlockData(textAsset2.text);
		terrainMaterial.mainTexture = terrainTexture;
		foreach (KeyValuePair<int, BlockData> item in blockDataHashtable)
		{
			terrainTexturesMinis.Add(item.Key, (Texture2D)Resources.Load("TerrainTextures/" + item.Key, typeof(Texture2D)));
		}
	}

	public Texture2D GetImageByNR(int nr)
	{
		if (terrainTexturesMinis.ContainsKey(nr))
		{
			return terrainTexturesMinis[nr];
		}
		return loadingCubeImage;
	}

	private bool LoadCustomTerrainTexture()
	{
		Debug.Log("GameSettings.customTerrainFolder=" + GameSettings.customTerrainFolder);
		if (GameSettings.customTerrainFolder == string.Empty)
		{
			return false;
		}
		if (!Directory.Exists("Mods/" + GameSettings.customTerrainFolder))
		{
			Debug.LogError("Couldn't find custmTerrainFolder: Mods/" + GameSettings.customTerrainFolder);
			return false;
		}
		if (!File.Exists("Mods/" + GameSettings.customTerrainFolder + "/blocks.txt"))
		{
			Debug.LogError("Couldn't find blocks.txt in Mods/" + GameSettings.customTerrainFolder);
			return false;
		}
		Dictionary<int, Texture2D> dictionary = new Dictionary<int, Texture2D>();
		int num = 0;
		for (int i = 1; i <= 999; i++)
		{
			Texture2D texture2D = new Texture2D(128, 128);
			byte[] array = null;
			if (File.Exists("Mods/" + GameSettings.customTerrainFolder + "/" + i + ".png"))
			{
				array = File.ReadAllBytes("Mods/" + GameSettings.customTerrainFolder + "/" + i + ".png");
			}
			else if (File.Exists("Mods/" + GameSettings.customTerrainFolder + "/" + i + ".jpg"))
			{
				array = File.ReadAllBytes("Mods/" + GameSettings.customTerrainFolder + "/" + i + ".jpg");
			}
			if (array == null)
			{
				break;
			}
			texture2D.LoadImage(array);
			dictionary.Add(i, texture2D);
			num++;
		}
		Texture2D[] array2 = new Texture2D[dictionary.Count];
		for (int j = 1; j <= dictionary.Count; j++)
		{
			array2[j - 1] = dictionary[j];
		}
		Texture2D texture2D2 = (Texture2D)Resources.Load("terainPacked", typeof(Texture2D));
		int num2 = 90;
		Rect[] array3 = texture2D2.PackTextures(array2, num2, 1024);
		if (array3 == null)
		{
			Debug.LogError("Error packing terrain textures!");
			GameSettings.gameHadError = "Error packing terrain textures!";
			Application.LoadLevel(Application.loadedLevel - 1);
		}
		ColorEdgeValues(texture2D2, array3, (int)((float)num2 * 0.5f));
		texture2D2.Apply(updateMipmaps: true);
		Debug.Log("maintex = " + texture2D2.ToString());
		terrainTexturePNG = texture2D2.EncodeToPNG();
		Debug.Log("PNG = " + terrainTexturePNG.Length);
		texture2D2.Compress(highQuality: true);
		terrainMaterial.mainTexture = (terrainTexture = texture2D2);
		StartCoroutine(CompressCustomTextures());
		for (int k = 0; k < num; k++)
		{
			terrainTextureUVDict.Add(k + 1, array3[k]);
		}
		customTerrainData = File.ReadAllText("Mods/" + GameSettings.customTerrainFolder + "/blocks.txt");
		GenerateBlockData(customTerrainData);
		foreach (KeyValuePair<int, BlockData> item in blockDataHashtable)
		{
			int textureRectNr = item.Value.sideTextureDefaults.textureRectNr;
			Texture2D texture2D3 = new Texture2D(128, 128);
			texture2D3.LoadImage(dictionary[textureRectNr].EncodeToPNG());
			terrainTexturesMinis.Add(item.Key, texture2D3);
		}
		customTerrain = true;
		return true;
	}

	private IEnumerator CompressCustomTextures()
	{
		yield return 0;
		Debug.Log("TERRAIN was " + terrainTexturePNG.Length);
		terrainTexturePNG = Zipper.ZipBytes(terrainTexturePNG);
		Debug.Log("TERRAIN COMPRESSED NAAR " + terrainTexturePNG.Length);
	}

	private string ExportTerrainUVDataString()
	{
		string text = string.Empty;
		foreach (KeyValuePair<int, Rect> item in terrainTextureUVDict)
		{
			text = text + Utils.RectToString(item.Value) + "\n";
		}
		return text;
	}

	private void ImportTerrainUVDataString(string inp)
	{
		string[] array = inp.Split('\n');
		int num = 1;
		string[] array2 = array;
		foreach (string myS in array2)
		{
			terrainTextureUVDict.Add(num, Utils.StringToRect(myS));
			num++;
		}
	}

	private void GenerateBlockData(string blockDataString)
	{
		string[] array = blockDataString.Split('\n');
		int num = 1;
		string[] array2 = array;
		foreach (string obj in array2)
		{
			BlockData blockData = new BlockData();
			string[] array3 = obj.Split('#');
			if (array3.Length >= 3)
			{
				blockData.name = array3[0];
				blockData.walkSoundsType = StringToSoundType(array3[1]);
				blockData.blockDataID = num;
				BlockSideTextureInfo blockSideTextureInfo = new BlockSideTextureInfo();
				blockSideTextureInfo.textureRectNr = Utils.SafeIntParse(array3[2], 1);
				blockData.sideTextureDefaults = blockSideTextureInfo;
			}
			for (int j = 3; j < array3.Length; j++)
			{
				string[] array4 = array3[j].Split('=');
				if (array4.Length == 2)
				{
					BlockSides key = StringToSide(array4[0]);
					int textureRectNr = Utils.SafeIntParse(array4[1], 1);
					BlockSideTextureInfo blockSideTextureInfo2 = new BlockSideTextureInfo();
					blockSideTextureInfo2.textureRectNr = textureRectNr;
					blockData.customSidesDict.Add(key, blockSideTextureInfo2);
				}
			}
			blockData.RecalculateUVSize(terrainTextureUVDict);
			blockDataHashtable.Add(num, blockData);
			num++;
		}
		Inventory.SP.LoadItems(blockDataHashtable);
	}

	private BlockSides StringToSide(string str)
	{
		return str switch
		{
			"top" => BlockSides.top, 
			"bottom" => BlockSides.bottom, 
			"left" => BlockSides.left, 
			"right" => BlockSides.right, 
			"front" => BlockSides.front, 
			_ => BlockSides.back, 
		};
	}

	private BlockSoundType StringToSoundType(string str)
	{
		return str switch
		{
			"grass" => BlockSoundType.grass, 
			"wood" => BlockSoundType.wood, 
			"sand" => BlockSoundType.sand, 
			_ => BlockSoundType.concrete, 
		};
	}

	public static Color[] GetColorArray(int size, Color col)
	{
		Color[] array = new Color[size];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = col;
		}
		return array;
	}

	public static void ColorEdgeValues(Texture2D texture, Rect[] rects, int padding)
	{
		for (int i = 0; i < rects.Length; i++)
		{
			Rect rect = rects[i];
			int num = (int)(rect.xMin * (float)texture.width);
			int num2 = (int)(rect.xMax * (float)texture.width) - 1;
			int num3 = (int)(rect.yMax * (float)texture.height) - 1;
			int num4 = (int)(rect.yMin * (float)texture.height);
			for (int j = num4; j <= num3 && j >= 0 && j < texture.height; j++)
			{
				Color pixel = texture.GetPixel(num, j);
				int num5 = Mathf.Clamp(num - padding, 0, texture.width - 1);
				int num6 = num - num5;
				if (num6 > 0)
				{
					texture.SetPixels(num5, j, num6, 1, GetColorArray(num6, pixel));
				}
				pixel = texture.GetPixel(num2, j);
				num6 = Mathf.Clamp(num2 + padding, 0, texture.width - 1) - num2;
				if (num6 > 0)
				{
					texture.SetPixels(num2 + 1, j, num6, 1, GetColorArray(num6, pixel));
				}
			}
			for (int k = Mathf.Clamp(num - padding, 0, texture.width); k <= num2 + padding && k >= 0 && k < texture.width; k++)
			{
				Color pixel2 = texture.GetPixel(k, num3);
				int num7 = Mathf.Clamp(num3 + padding, 0, texture.height - 1) - num3;
				if (num7 > 0)
				{
					texture.SetPixels(k, num3 + 1, 1, num7, GetColorArray(num7, pixel2));
				}
				pixel2 = texture.GetPixel(k, num4);
				int num8 = Mathf.Clamp(num4 - padding, 0, texture.height - 1);
				num7 = num4 - num8;
				if (num7 > 0)
				{
					texture.SetPixels(k, num8, 1, num7, GetColorArray(num7, pixel2));
				}
			}
		}
		texture.Apply();
	}

	public void ChangeTerrainQuality(bool toLow)
	{
		if (toLow)
		{
			terrainMaterial.shader = terrainShaderLow;
		}
		else
		{
			terrainMaterial.shader = terrainShaderNormal;
		}
	}

	public ushort GetWorldSizeX()
	{
		return (ushort)worldSizeX;
	}

	public ushort GetWorldSizeZ()
	{
		return (ushort)worldSizeZ;
	}

	public ushort GetWorldSizeY()
	{
		return (ushort)worldHeight;
	}

	public AudioClip GetRandomAudioClip(BlockSoundType theType)
	{
		BlockSoundTypeSounds[] array = blockSoundTypes;
		foreach (BlockSoundTypeSounds blockSoundTypeSounds in array)
		{
			if (blockSoundTypeSounds.walkSoundsType == theType)
			{
				return blockSoundTypeSounds.audioClips[Random.Range(0, blockSoundTypeSounds.audioClips.Length)];
			}
		}
		Debug.LogError("Couldnt find audioclip!");
		return null;
	}

	public void SetLocalPlayer(Transform trans)
	{
		localPlayerTransform = trans;
	}

	private string GetLevelDataHeaderString()
	{
		string text = 1 + " " + worldSizeX + " " + worldHeight + " " + worldSizeZ + "\n";
		Vector3 vector = GetSpawnpoint();
		string text2 = text;
		return text2 + vector.x + " " + vector.y + " " + vector.z + "\n";
	}

	private IEnumerator GetLevelData(IENumeratorOutput output, bool rightAway, int xStart, int yStart, int zStart, int xEnd, int yEnd, int zEnd)
	{
		string levelData = string.Empty;
		int blocksDone = 0;
		foreach (ushort xPos in xWorldData.Keys)
		{
			if (xWorldData[xPos] == null || xPos < xStart || xPos >= xEnd)
			{
				continue;
			}
			foreach (ushort yPos in xWorldData[xPos].Keys)
			{
				if (xWorldData[xPos][yPos] == null || yPos < yStart || yPos >= yEnd)
				{
					continue;
				}
				List<ushort> list = new List<ushort>(xWorldData[xPos][yPos].Keys);
				list.Sort();
				ushort nextZ = 0;
				bool skippingBlocks = false;
				foreach (ushort zPos in list)
				{
					if (zPos < zStart)
					{
						continue;
					}
					if (zPos >= zEnd)
					{
						break;
					}
					blocksDone++;
					if (!rightAway && blocksDone % 500 == 0)
					{
						yield return 0;
					}
					if ((!skippingBlocks || nextZ < zPos) && xWorldData[xPos] != null && xWorldData[xPos][yPos] != null && xWorldData[xPos][yPos][zPos] != null)
					{
						BlockInstance blockInstance = xWorldData[xPos][yPos][zPos];
						if (blockInstance != null)
						{
							ushort num = Save_CheckSameBlocks(xPos, yPos, zPos, (ushort)zEnd, blockInstance.blockData.blockDataID);
							nextZ = (ushort)(zPos + num);
							skippingBlocks = true;
							string text = levelData;
							levelData = text + "\n" + blockInstance.blockData.blockDataID + " " + blockInstance.position.x + " " + blockInstance.position.y + " " + blockInstance.position.z + " " + num;
						}
					}
				}
			}
		}
		output.SetOutput(levelData);
	}

	private IEnumerator GetCompleteLevelDataString(IENumeratorOutput output, bool rightAway)
	{
		string levelData3 = GetLevelDataHeaderString();
		IENumeratorOutput dataOutput = new IENumeratorOutput();
		yield return StartCoroutine(GetLevelData(dataOutput, rightAway, 0, 0, 0, GetWorldSizeX(), GetWorldSizeY(), GetWorldSizeZ()));
		levelData3 += (string)dataOutput.GetOutput();
		output.SetOutput(levelData3);
	}

	private IEnumerator ServerSendChunkData(Chunk chunk, NetworkPlayer netPlayer)
	{
		if (netPlayer == Network.player)
		{
			StartCoroutine(DoneSendingChunk(chunk.x, chunk.y, chunk.z, 0));
			yield break;
		}
		IENumeratorOutput output = new IENumeratorOutput();
		yield return StartCoroutine(GetLevelData(output, rightAway: true, chunk.x * networkChunkSize, chunk.y * networkChunkSize, chunk.z * networkChunkSize, chunk.x * networkChunkSize + networkChunkSize, chunk.y * networkChunkSize + networkChunkSize, chunk.z * networkChunkSize + networkChunkSize));
		byte[] array = MultiplayerFunctions.StringToBytes((string)output.GetOutput());
		int num = 0;
		if (array.Length != 0)
		{
			num = 1;
			base.networkView.RPC("GetLevelDataString", netPlayer, array, chunk.x, chunk.y, chunk.z);
		}
		base.networkView.RPC("DoneSendingChunk", netPlayer, chunk.x, chunk.y, chunk.z, num);
	}

	public void SendLevelData(NetworkPlayer netPlayer)
	{
		Debug.Log(Time.realtimeSinceStartup + " Start sending levelheader data");
		base.networkView.RPC("GetLevelDataHeader", netPlayer, GetLevelDataHeaderString(), Utils.ColorToString(GameSettings.ambientColor), Utils.ColorToString(GameSettings.fogColor), Utils.ColorToString(GameSettings.skyboxColor), Sun.SP.GetRotation(), GameSettings.rotateSun, GameSettings.rotateSun_Direction, GameSettings.rotateSun_speed, customTerrain, (!customTerrain) ? string.Empty : ExportTerrainUVDataString(), (!customTerrain) ? string.Empty : customTerrainData);
		if (customTerrain)
		{
			base.networkView.RPC("SendCustomTerrain", netPlayer, terrainTexturePNG);
			foreach (KeyValuePair<int, Texture2D> terrainTexturesMini in terrainTexturesMinis)
			{
				base.networkView.RPC("SendTerrainTexture", netPlayer, terrainTexturesMini.Value.EncodeToPNG(), terrainTexturesMini.Key);
			}
		}
		base.networkView.RPC("FinishedSendingLevel", netPlayer);
	}

	[RPC]
	private void SendCustomTerrain(byte[] terrainTexture)
	{
		terrainTexture = Zipper.UnZipBytes(terrainTexture);
		Texture2D texture2D = (Texture2D)Resources.Load("terainPacked", typeof(Texture2D));
		texture2D.LoadImage(terrainTexture);
		if (Options.gameQuality <= 2)
		{
			texture2D.Compress(highQuality: true);
		}
		terrainMaterial.mainTexture = texture2D;
	}

	[RPC]
	private void SendTerrainTexture(byte[] terrainTexture, int nr)
	{
		Texture2D texture2D = new Texture2D(128, 128);
		texture2D.LoadImage(terrainTexture);
		texture2D.Compress(highQuality: true);
		terrainTexturesMinis.Add(nr, texture2D);
	}

	[RPC]
	private void GetLevelDataHeader(string line, string ambientColor, string fogColor, string skyBoxColor, Vector3 sunRotation, bool rotateSun, Vector3 rotateSun_Direction, float rotateSun_speed, bool customTerrain, string terrainUVString, string blockDataString)
	{
		GameSettings.ambientColor = Utils.StringToColor(ambientColor);
		GameSettings.fogColor = Utils.StringToColor(fogColor);
		GameSettings.skyboxColor = Utils.StringToColor(skyBoxColor);
		GameSettings.sunRotation = sunRotation;
		GameSettings.rotateSun = rotateSun;
		GameSettings.rotateSun_Direction = rotateSun_Direction;
		GameSettings.rotateSun_speed = rotateSun_speed;
		Debug.Log("GetLevelDataHeader Skybox=" + skyBoxColor);
		Sun.SP.SetupSun();
		this.customTerrain = customTerrain;
		if (customTerrain)
		{
			ImportTerrainUVDataString(terrainUVString);
			GenerateBlockData(blockDataString);
		}
		else
		{
			LoadDefaultTerrain();
		}
		ImportSaveFileHeader(line);
	}

	[RPC]
	private IEnumerator GetLevelDataString(byte[] bitsss, int chunkX, int chunkY, int chunkZ)
	{
		string text = MultiplayerFunctions.BytesToString(bitsss);
		Chunk chunk = new Chunk(chunkX, chunkY, chunkZ, networkChunkSize);
		if (clientRegisteredChunksAccepted.Contains(chunk))
		{
			Debug.LogError("Accepting chunk twice :S!  " + chunkX + " " + chunkY + " " + chunkZ);
			yield break;
		}
		if (!clientRegisteredChunksRequests.Contains(chunk))
		{
			Debug.LogWarning("Dismissing chunk info, we didnt request " + chunkX + " " + chunkY + " " + chunkZ);
			yield break;
		}
		int linesDone = 0;
		string[] array = text.Split('\n');
		string[] array2 = array;
		foreach (string line in array2)
		{
			ImportSaveFileLine(line);
			if (linesDone % 20 == 0)
			{
				yield return 0;
			}
			linesDone++;
		}
		if (!receivedChunkParts.ContainsKey(chunk))
		{
			receivedChunkParts.Add(chunk, 0);
		}
		Dictionary<Chunk, int> dictionary;
		Dictionary<Chunk, int> dictionary2 = (dictionary = receivedChunkParts);
		Chunk key;
		Chunk key2 = (key = chunk);
		int num = dictionary2[key];
		dictionary[key2] = num + 1;
	}

	[RPC]
	private IEnumerator FinishedSendingLevel()
	{
		GameMenu.SP.SetLoadingStatus("Downloaded level header data");
		yield return 0;
		downloadedLevel = true;
	}

	public IEnumerator SaveWorld(bool rightAway)
	{
		if (isSaving)
		{
			yield break;
		}
		isSaving = true;
		Debug.Log("Starting SaveWorld..");
		float startSave = Time.realtimeSinceStartup;
		IENumeratorOutput output = new IENumeratorOutput();
		yield return StartCoroutine(GetCompleteLevelDataString(output, rightAway));
		if (!output.Failed())
		{
			string levelData = (string)output.GetOutput();
			if (!rightAway)
			{
				yield return 0;
			}
			string text = Zipper.ZipString(levelData);
			float num = Time.realtimeSinceStartup - startSave;
			Debug.Log("SaveWorld (" + num + "): levelData.length=" + levelData.Length + " Zipped:" + text.Length);
			if (!Utils.IsWebplayer())
			{
				WriteWorldData(GameSettings.mapname, text);
			}
			else
			{
				Debug.Log("SaveWorld: Saved to playerprefs: " + GameSettings.mapname);
				PlayerPrefs.SetString(GameSettings.mapname, text);
			}
		}
		else
		{
			Debug.LogError("SaveWorld: Save world failed!");
		}
		float num2 = Time.realtimeSinceStartup - startSave;
		Debug.Log(Time.realtimeSinceStartup + " Saved world in " + num2 + " seconds");
		isSaving = false;
	}

	public static void CreateSaveFolder()
	{
		if (!Directory.Exists("Worlds/"))
		{
			Directory.CreateDirectory("Worlds/");
		}
	}

	public static string[] GetSaveNames()
	{
		string[] directories = Directory.GetDirectories("Worlds/");
		for (int i = 0; i < directories.Length; i++)
		{
			directories[i] = directories[i].Replace("Worlds/", string.Empty);
		}
		return directories;
	}

	public static void WriteWorldData(string worldName, string zippedData)
	{
		Debug.Log("SaveWorld: Saved to file: " + worldName + ".txt");
		CreateSaveFolder();
		if (!Directory.Exists("Worlds/" + worldName + "/"))
		{
			Directory.CreateDirectory("Worlds/" + worldName + "/");
		}
		StreamWriter streamWriter = new StreamWriter("Worlds/" + worldName + "/" + worldName + ".txt", append: false);
		streamWriter.WriteLine(zippedData);
		streamWriter.Close();
		Debug.Log("Saved world to file; Worlds/" + worldName + "/" + worldName + ".txt");
	}

	private ushort Save_CheckSameBlocks(ushort xPos, ushort yPos, ushort zPos, ushort maxZ, int orgBlockID)
	{
		ushort num = 0;
		ushort num2 = (ushort)(zPos + 1);
		while (num2 < maxZ)
		{
			if (!xWorldData[xPos][yPos].ContainsKey(num2))
			{
				return num;
			}
			if (xWorldData[xPos][yPos][num2].blockData.blockDataID == orgBlockID)
			{
				num++;
				num2++;
				continue;
			}
			return num;
		}
		return num;
	}

	public Vector3 GetSpawnpoint()
	{
		if (spawnpoint == Vector3.zero)
		{
			spawnpoint = new Vector3(GetWorldSizeX() / 2, GetWorldSizeY() / 2, GetWorldSizeZ() / 2);
		}
		return spawnpoint;
	}

	public void SetSpawnpoint(Vector3 newSpawn)
	{
		if (DedicatedServer.isDedicated || GameManager.SP.localPlayerNode.isAdmin)
		{
			spawnpoint = newSpawn;
			base.networkView.RPC("SetSpawnpointNetwork", RPCMode.Others, newSpawn);
		}
		else
		{
			Debug.LogError("Cant set spawnpoint!");
		}
	}

	[RPC]
	public void SetSpawnpointNetwork(Vector3 newSpawn, NetworkMessageInfo info)
	{
		Debug.Log("SetSpawnpoint");
		spawnpoint = newSpawn;
	}

	private void ImportSaveFileHeader(string line)
	{
		string[] array = line.Split('\n');
		string[] array2 = array[0].Split(' ');
		worldSizeX = int.Parse(array2[1]);
		worldHeight = int.Parse(array2[2]);
		worldSizeZ = int.Parse(array2[3]);
		array2 = array[1].Split(' ');
		spawnpoint = new Vector3(int.Parse(array2[0]), int.Parse(array2[1]), int.Parse(array2[2]));
	}

	private void ImportSaveFileLine(string line)
	{
		if (!(line == string.Empty))
		{
			string[] array = line.Split(' ');
			int blockTypeNr = int.Parse(array[0]);
			ushort x = (ushort)int.Parse(array[1]);
			ushort y = (ushort)int.Parse(array[2]);
			ushort num = (ushort)int.Parse(array[3]);
			int num2 = int.Parse(array[4]);
			while (num2 >= 0)
			{
				AddBlock(blockTypeNr, x, y, num);
				num2--;
				num++;
			}
		}
	}

	private void CreateEmptyWorld()
	{
		worldHeight = GameSettings.emptyMapHeight;
		worldSizeX = GameSettings.emptyMapX;
		worldSizeZ = GameSettings.emptyMapZ;
		worldFillInHeight = GameSettings.emptyMapFillInHeight;
		worldHeight = Mathf.Clamp(worldHeight, 1, 2048);
		worldSizeX = Mathf.Clamp(worldSizeX, 1, 2048);
		worldSizeZ = Mathf.Clamp(worldSizeZ, 1, 2048);
		if (worldHeight < worldFillInHeight)
		{
			worldFillInHeight = worldHeight;
		}
		worldFillInHeight = Mathf.Clamp(worldFillInHeight, 1, worldHeight);
		spawnpoint = new Vector3(worldSizeX / 2, worldFillInHeight + 1, worldSizeZ / 2);
		for (ushort num = 0; num < worldSizeX; num++)
		{
			for (ushort num2 = 0; num2 < worldSizeZ; num2++)
			{
				for (ushort num3 = 0; num3 < worldFillInHeight; num3++)
				{
					AddBlock(-1, num, num3, num2);
				}
			}
		}
	}

	private List<GameObject> DestroyPreviousCombined(List<GameObject> destroyList, int xStart, int yStart, int zStart, int combineSize)
	{
		string text = xStart + "_" + yStart + "_" + zStart + "#" + combineSize;
		if (!combinedAreaSize.ContainsKey(text))
		{
			Debug.LogError("DOES NOT CONTAIN: " + text);
			return destroyList;
		}
		if (combinedAreaSize[text] > 1)
		{
			combineSize /= 2;
			destroyList = DestroyPreviousCombined(destroyList, xStart, yStart, zStart, combineSize);
			destroyList = DestroyPreviousCombined(destroyList, xStart + combineSize, yStart, zStart, combineSize);
			destroyList = DestroyPreviousCombined(destroyList, xStart, yStart, zStart + combineSize, combineSize);
			destroyList = DestroyPreviousCombined(destroyList, xStart + combineSize, yStart, zStart + combineSize, combineSize);
			destroyList = DestroyPreviousCombined(destroyList, xStart, yStart + combineSize, zStart, combineSize);
			destroyList = DestroyPreviousCombined(destroyList, xStart + combineSize, yStart + combineSize, zStart, combineSize);
			destroyList = DestroyPreviousCombined(destroyList, xStart, yStart + combineSize, zStart + combineSize, combineSize);
			destroyList = DestroyPreviousCombined(destroyList, xStart + combineSize, yStart + combineSize, zStart + combineSize, combineSize);
		}
		else if (combinedAreas.ContainsKey(text))
		{
			GameObject gameObject = combinedAreas[text];
			if (gameObject != null)
			{
				destroyList.Add(gameObject);
				gameObject.transform.position += new Vector3(0.0001f, 0.0001f, 0.0001f);
			}
		}
		combinedAreaSize.Remove(text);
		return destroyList;
	}

	private Vector4 FindSmallestArea(Vector3 pos, int xStart, int yStart, int zStart, int areaSize)
	{
		string text = xStart + "_" + yStart + "_" + zStart + "#" + areaSize;
		if (!combinedAreaSize.ContainsKey(text))
		{
			Debug.LogError("FindSmallestArea SERIOUS ERROR DOES NOT CONTAIN: " + text);
			return Vector4.zero;
		}
		if (combinedAreaSize[text] > 1)
		{
			areaSize /= 2;
			if (pos.x >= (float)(xStart + areaSize))
			{
				xStart += areaSize;
			}
			if (pos.y >= (float)(yStart + areaSize))
			{
				yStart += areaSize;
			}
			if (pos.z >= (float)(zStart + areaSize))
			{
				zStart += areaSize;
			}
			return FindSmallestArea(pos, xStart, yStart, zStart, areaSize);
		}
		return new Vector4(xStart, yStart, zStart, areaSize);
	}

	private void ChangedAreaOfPos(int x, int y, int z)
	{
		Vector3 item = new Vector3(x, y, z);
		Chunk chunkForWorldPos = GetChunkForWorldPos(x, y, z);
		if (!clientRegisteredChunksAccepted.Contains(chunkForWorldPos))
		{
			Debug.LogWarning("Dismissing area update because we dont have this chunk (yet?)");
		}
		else if (!changeAreaQueue.Contains(item))
		{
			changeAreaQueue.Add(new Vector3(x, y, z));
		}
	}

	private int GetBiggestDimension()
	{
		return Mathf.Max(Mathf.Max(worldSizeX, worldSizeZ), worldHeight);
	}

	private IEnumerator ApplyChangePos(Vector3 vect)
	{
		ushort num = (ushort)vect.x;
		ushort num2 = (ushort)vect.y;
		ushort num3 = (ushort)vect.z;
		Vector4 vector = FindSmallestArea(xStart: num / networkChunkSize * networkChunkSize, yStart: num2 / networkChunkSize * networkChunkSize, zStart: num3 / networkChunkSize * networkChunkSize, pos: new Vector3((int)num, (int)num2, (int)num3), areaSize: networkChunkSize);
		StartCoroutine(ApplyAreaChange(num, num2, num3));
		if (num == (ushort)vector.x)
		{
			StartCoroutine(ApplyAreaChange((ushort)(num - 1), num2, num3));
		}
		else if ((float)(int)num == (float)(int)(ushort)vector.x + vector.w - 1f)
		{
			StartCoroutine(ApplyAreaChange((ushort)(num + 1), num2, num3));
		}
		if (num2 == (ushort)vector.y)
		{
			StartCoroutine(ApplyAreaChange(num, (ushort)(num2 - 1), num3));
		}
		else if ((float)(int)num2 == (float)(int)(ushort)vector.y + vector.w - 1f)
		{
			StartCoroutine(ApplyAreaChange(num, (ushort)(num2 + 1), num3));
		}
		if (num3 == (ushort)vector.z)
		{
			StartCoroutine(ApplyAreaChange(num, num2, (ushort)(num3 - 1)));
		}
		else if ((float)(int)num3 == (float)(int)(ushort)vector.z + vector.w - 1f)
		{
			StartCoroutine(ApplyAreaChange(num, num2, (ushort)(num3 + 1)));
		}
		yield break;
	}

	private IEnumerator ApplyAreaChange(ushort x, ushort y, ushort z)
	{
		if (x < 0 || y < 0 || z < 0 || x > worldSizeX || y > worldHeight || z > worldSizeZ)
		{
			yield break;
		}
		Vector4 vector = FindSmallestArea(xStart: x / networkChunkSize * networkChunkSize, yStart: y / networkChunkSize * networkChunkSize, zStart: z / networkChunkSize * networkChunkSize, pos: new Vector3((int)x, (int)y, (int)z), areaSize: networkChunkSize);
		if (vector == Vector4.zero)
		{
			yield break;
		}
		List<GameObject> destroyList = new List<GameObject>();
		destroyList = DestroyPreviousCombined(destroyList, (ushort)vector.x, (ushort)vector.y, (ushort)vector.z, (ushort)vector.w);
		RecombineArea((ushort)vector.x, (ushort)vector.y, (ushort)vector.z, (ushort)vector.w);
		foreach (GameObject item in destroyList)
		{
			if (!(item == null) && !(item.GetComponent<MeshFilter>() == null))
			{
				Object.Destroy(item.GetComponent<MeshFilter>().mesh);
				Object.Destroy(item);
			}
		}
	}

	private Mesh MeshFromGraphicsData(BlockGrahicsData blockGraphic)
	{
		Vector3[] array = new Vector3[blockGraphic.vertexData.Count];
		Vector2[] array2 = new Vector2[blockGraphic.vertexData.Count];
		for (int i = 0; i < blockGraphic.vertexData.Count; i++)
		{
			ref Vector3 reference = ref array[i];
			reference = blockGraphic.vertexData[i].position;
			ref Vector2 reference2 = ref array2[i];
			reference2 = new Vector2(blockGraphic.vertexData[i].u, blockGraphic.vertexData[i].v);
		}
		int[] array3 = new int[blockGraphic.triangles.Count];
		for (int j = 0; j < blockGraphic.triangles.Count; j++)
		{
			array3[j] = blockGraphic.triangles[j];
		}
		Mesh mesh = new Mesh();
		mesh.vertices = array;
		mesh.uv = array2;
		mesh.triangles = array3;
		mesh.RecalculateNormals();
		return mesh;
	}

	public GameObject SpawnLocalPlayerCube(Transform parent, BlockData bData)
	{
		BlockGrahicsData blockGraphic = default(BlockGrahicsData);
		blockGraphic.vertexData = new List<VertexPositionTexture>();
		blockGraphic.triangles = new List<ushort>();
		BlockInstance blockInstance = new BlockInstance();
		blockInstance.blockData = bData;
		CreateBlockGraphics(blockGraphic, blockInstance, 0, 0, 0, alwaysRender: true);
		Mesh mesh = MeshFromGraphicsData(blockGraphic);
		GameObject obj = new GameObject("PlayerCubeLocal");
		obj.layer = 8;
		obj.transform.parent = parent;
		obj.transform.localScale = Vector3.one;
		obj.transform.localRotation = Quaternion.identity;
		obj.transform.localPosition = Vector3.zero;
		MeshFilter meshFilter = (MeshFilter)obj.AddComponent(typeof(MeshFilter));
		obj.AddComponent("MeshRenderer");
		obj.renderer.material = terrainMaterial;
		meshFilter.mesh = mesh;
		Renderer renderer = meshFilter.renderer;
		bool flag = false;
		meshFilter.renderer.castShadows = flag;
		renderer.receiveShadows = flag;
		return obj;
	}

	private void RecombineArea(ushort xStart, ushort yStart, ushort zStart, ushort combineSize)
	{
		if (DedicatedServer.isDedicated)
		{
			Debug.LogError("Dedicated server shouldnt combine!");
			return;
		}
		string key = xStart + "_" + yStart + "_" + zStart + "#" + combineSize;
		bool flag = false;
		if (combineSize > GetOptimalAreaSize(xStart, yStart, zStart, combineSize))
		{
			flag = true;
		}
		if (combinedAreaSize.ContainsKey(key))
		{
			flag = combinedAreaSize[key] != 1;
		}
		else
		{
			combinedAreaSize.Add(key, (!flag) ? 1 : 2);
		}
		if (flag)
		{
			combineSize /= 2;
			RecombineArea(xStart, yStart, zStart, combineSize);
			RecombineArea((ushort)(xStart + combineSize), yStart, zStart, combineSize);
			RecombineArea(xStart, yStart, (ushort)(zStart + combineSize), combineSize);
			RecombineArea((ushort)(xStart + combineSize), yStart, (ushort)(zStart + combineSize), combineSize);
			RecombineArea(xStart, (ushort)(yStart + combineSize), zStart, combineSize);
			RecombineArea((ushort)(xStart + combineSize), (ushort)(yStart + combineSize), zStart, combineSize);
			RecombineArea(xStart, (ushort)(yStart + combineSize), (ushort)(zStart + combineSize), combineSize);
			RecombineArea((ushort)(xStart + combineSize), (ushort)(yStart + combineSize), (ushort)(zStart + combineSize), combineSize);
			return;
		}
		BlockGrahicsData blockGraphic = default(BlockGrahicsData);
		blockGraphic.vertexData = new List<VertexPositionTexture>();
		blockGraphic.triangles = new List<ushort>();
		for (ushort num = xStart; num < (ushort)(xStart + combineSize); num++)
		{
			for (ushort num2 = zStart; num2 < zStart + combineSize; num2++)
			{
				for (ushort num3 = yStart; num3 < yStart + combineSize; num3++)
				{
					BlockInstance blockInstanceQuick = GetBlockInstanceQuick(num, num3, num2);
					if (blockInstanceQuick != null && !blockInstanceQuick.neverVisible)
					{
						CreateBlockGraphics(blockGraphic, blockInstanceQuick, num, num3, num2, alwaysRender: false);
					}
				}
			}
		}
		if (blockGraphic.vertexData.Count > 0)
		{
			Mesh mesh = MeshFromGraphicsData(blockGraphic);
			GameObject gameObject = new GameObject("Chunk_" + xStart + "_" + yStart + "_" + zStart);
			gameObject.transform.parent = base.transform;
			gameObject.transform.localScale = Vector3.one;
			gameObject.transform.localRotation = Quaternion.identity;
			gameObject.transform.localPosition = Vector3.zero;
			gameObject.AddComponent(typeof(MeshFilter));
			gameObject.AddComponent("MeshRenderer");
			gameObject.renderer.material = terrainMaterial;
			MeshFilter obj = (MeshFilter)gameObject.GetComponent(typeof(MeshFilter));
			obj.mesh = mesh;
			Renderer renderer = obj.renderer;
			bool flag2 = true;
			obj.renderer.castShadows = flag2;
			renderer.receiveShadows = flag2;
			combinedAreas[key] = gameObject;
		}
		else
		{
			combinedAreas[key] = null;
		}
	}

	private void AddBlock(int blockTypeNr, ushort x, ushort y, ushort z)
	{
		if (x < 0 || y < 0 || z < 0 || x >= worldSizeX || y >= worldHeight || z >= worldSizeZ)
		{
			Debug.LogError("AddBlock outside world area!" + x + " " + y + " " + z + " worldX=" + worldSizeX + " worldZ=" + worldSizeZ + " worldheight=" + worldHeight);
		}
		else if (ContainsBlock(x, y, z))
		{
			Debug.LogError("SpawnCube we already have a block there: " + x + " " + y + " " + z);
		}
		else
		{
			BlockInstance blockInstance = new BlockInstance();
			if (blockTypeNr == -1)
			{
				blockTypeNr = (((float)(int)y < (float)worldFillInHeight * 0.5f) ? 7 : ((y >= worldFillInHeight - 1) ? 1 : 2));
			}
			blockTypeNr = Mathf.Min(blockTypeNr, blockDataHashtable.Count);
			blockInstance.blockData = GetBlockData(blockTypeNr);
			blockInstance.position = new Vector3((int)x, (int)y, (int)z);
			AddBlockInstance(x, y, z, blockInstance);
			CheckNeverVisible(x, y, z);
			DoNeighbourCheck(x, y, z);
		}
	}

	private bool CheckNeverVisible(ushort x, ushort y, ushort z)
	{
		BlockInstance blockInstanceQuick = GetBlockInstanceQuick(x, y, z);
		if (blockInstanceQuick == null)
		{
			return false;
		}
		if (ContainsBlock(x, (ushort)(y + 1), z) && (ContainsBlock(x, (ushort)(y - 1), z) || y == 0) && ContainsBlock((ushort)(x + 1), y, z) && ContainsBlock((ushort)(x - 1), y, z) && ContainsBlock(x, y, (ushort)(z + 1)) && ContainsBlock(x, y, (ushort)(z - 1)))
		{
			blockInstanceQuick.neverVisible = true;
		}
		blockInstanceQuick.neverVisible = false;
		return blockInstanceQuick.neverVisible;
	}

	private void DoNeighbourCheck(ushort x, ushort y, ushort z)
	{
		CheckNeverVisible(x, (ushort)(y + 1), z);
		CheckNeverVisible(x, (ushort)(y - 1), z);
		CheckNeverVisible((ushort)(x + 1), y, z);
		CheckNeverVisible((ushort)(x - 1), y, z);
		CheckNeverVisible(x, y, (ushort)(z + 1));
		CheckNeverVisible(x, y, (ushort)(z - 1));
	}

	private void SetPlayerMovement(bool allowed)
	{
		if (localPlayerTransform != null)
		{
			FPSWalker component = localPlayerTransform.GetComponent<FPSWalker>();
			if (component != null)
			{
				component.SetMovementLock(!allowed);
			}
		}
	}

	public int GetClosestPlayerDistance(int x, int y, int z)
	{
		List<PlayerNode> playerList = GameManager.SP.GetPlayerList();
		int num = (int)Vector3.Distance(new Vector3(x, y, z), GetSpawnpoint());
		foreach (PlayerNode item in playerList)
		{
			if (item != null && !(item.transform == null))
			{
				int num2 = (int)Vector3.Distance(new Vector3(x, y, z), item.transform.position);
				if (num2 < num)
				{
					num = num2;
				}
			}
		}
		return num;
	}

	public int GetLocalPlayerDistance(int x, int y, int z)
	{
		PlayerNode localPlayerNode = GameManager.SP.localPlayerNode;
		if (localPlayerNode == null || localPlayerNode.transform == null)
		{
			return 100;
		}
		return (int)Vector3.Distance(new Vector3(x, y, z), localPlayerNode.transform.position);
	}

	private void Update()
	{
		if (GameManager.SP.GetFinishedLoading() && !(localPlayerTransform == null))
		{
			Vector3 position = localPlayerTransform.position;
			Vector3 vector = new Vector3((int)position.x, (int)position.y, (int)position.z);
			if (lastCullingPos != vector)
			{
				CheckSun();
				CheckChunks();
				StartCoroutine(ReCalculatePhysics(vector));
				lastCullingPos = vector;
			}
		}
	}

	public void CheckSun()
	{
		Vector3 vector = Vector3.zero;
		for (int i = 0; i < 300; i++)
		{
			Vector3 vector2 = localPlayerTransform.position + i * -Sun.SP.GetForward();
			if (vector2.x > (float)(int)GetWorldSizeX() || vector2.y > (float)(int)GetWorldSizeY() || vector2.z > (float)(int)GetWorldSizeZ())
			{
				break;
			}
			if (vector != vector2)
			{
				vector = vector2;
				if (ContainsBlock((ushort)vector2.x, (ushort)vector2.y, (ushort)vector2.z))
				{
					Sun.SP.SetFlare(enabled: false);
					return;
				}
			}
		}
		Sun.SP.SetFlare(enabled: true);
	}

	public void RunGraphicChanges()
	{
		if (!workingTerrain && changeAreaQueue.Count > 0)
		{
			StartCoroutine(CheckQueue());
		}
	}

	private IEnumerator CheckQueue()
	{
		if (workingTerrain)
		{
			yield break;
		}
		lock (this)
		{
			workingTerrain = true;
			for (int i = changeAreaQueue.Count - 1; i >= 0; i--)
			{
				yield return StartCoroutine(ApplyChangePos(changeAreaQueue[i]));
				changeAreaQueue.Remove(changeAreaQueue[i]);
			}
			workingTerrain = false;
		}
	}

	private IEnumerator ReCalculatePhysics(Vector3 currentPos)
	{
		int num = 4;
		int num2 = 4;
		int num3 = 4;
		int num4 = 5;
		for (int j = -num; j <= num; j++)
		{
			for (int k = -num; k <= num; k++)
			{
				for (int l = -num2; l <= num3; l++)
				{
					if (Mathf.Abs(j) + Mathf.Abs(k) + Mathf.Abs(l) <= num4)
					{
						ushort x = (ushort)(j + (int)currentPos.x);
						ushort y = (ushort)(l + (int)currentPos.y);
						ushort z = (ushort)(k + (int)currentPos.z);
						EnablePhysics(x, y, z);
					}
				}
			}
		}
		if (!((double)lastDisable + 0.2 < (double)Time.time))
		{
			yield break;
		}
		object[] array = new object[physicBlocks.Keys.Count];
		physicBlocks.Keys.CopyTo(array, 0);
		int i = 0;
		object[] array2 = array;
		object[] array3 = array2;
		foreach (object key in array3)
		{
			BlockInstance blockInstance = (BlockInstance)physicBlocks[key];
			if (blockInstance == null)
			{
				physicBlocks.Remove(key);
				continue;
			}
			if (blockInstance.physicTransform == null)
			{
				physicBlocks.Remove(key);
				continue;
			}
			if (Vector3.Distance(localPlayerTransform.position, blockInstance.physicTransform.position) >= 10f || blockInstance.neverVisible)
			{
				physicBlocks.Remove(key);
				Object.Destroy(blockInstance.physicTransform.gameObject);
			}
			i++;
			if (i % 5 == 0)
			{
				yield return 0;
			}
		}
		lastDisable = Time.time;
	}

	private void EnablePhysics(ushort x, ushort y, ushort z)
	{
		BlockInstance blockInstanceQuick = GetBlockInstanceQuick(x, y, z);
		if (blockInstanceQuick != null && !blockInstanceQuick.neverVisible && !blockInstanceQuick.HasPhysics())
		{
			Vector3 vector = new Vector3((int)x, (int)y, (int)z);
			physicBlocks.Add(vector, blockInstanceQuick);
			blockInstanceQuick.physicTransform = (Transform)Object.Instantiate(physicsBlockPrefab, new Vector3((int)x, (int)y, (int)z), Quaternion.identity);
			blockInstanceQuick.physicTransform.parent = thisTrans;
		}
	}

	public void PlayWorldSound(AudioClip clip, int x, int y, int z)
	{
		int value = 50 - GetLocalPlayerDistance(x, y, z);
		value = Mathf.Clamp(value, 0, 50);
		base.audio.PlayOneShot(clip, (float)value / 50f);
	}

	public void OverWriteBlock(int blockTypeNr, ushort x, ushort y, ushort z)
	{
		if (ContainsBlock(x, y, z))
		{
			KillBlock(x, y, z);
		}
		AddBlock(blockTypeNr, x, y, z);
	}

	private void CreateBlockGraphics(BlockGrahicsData blockGraphic, BlockInstance bIns, ushort x, ushort y, ushort z, bool alwaysRender)
	{
		BlockData blockData = bIns.blockData;
		bool flag = false;
		bool num = alwaysRender || GetBlockInstanceQuick(x, (ushort)(y + 1), z) == null;
		bool flag2 = alwaysRender || GetBlockInstanceQuick(x, (ushort)(y - 1), z) == null;
		bool flag3 = alwaysRender || GetBlockInstanceQuick(x, y, (ushort)(z - 1)) == null;
		bool flag4 = alwaysRender || GetBlockInstanceQuick(x, y, (ushort)(z + 1)) == null;
		bool flag5 = alwaysRender || GetBlockInstanceQuick((ushort)(x - 1), y, z) == null;
		flag = alwaysRender || GetBlockInstanceQuick((ushort)(x + 1), y, z) == null;
		float num4;
		float num3;
		float num2;
		float num5 = (num4 = (num3 = (num2 = 0.5f)));
		Rect rect = new Rect(blockData.sideTextureDefaults.uStart, blockData.sideTextureDefaults.vStart, blockData.sideTextureDefaults.uvWidth, blockData.sideTextureDefaults.uvHeight);
		if (num)
		{
			Rect rect2 = rect;
			if (blockData.customSidesDict.ContainsKey(BlockSides.top))
			{
				BlockSideTextureInfo blockSideTextureInfo = blockData.customSidesDict[BlockSides.top];
				rect2 = new Rect(blockSideTextureInfo.uStart, blockSideTextureInfo.vStart, blockSideTextureInfo.uvWidth, blockSideTextureInfo.uvHeight);
			}
			int count = blockGraphic.vertexData.Count;
			blockGraphic.vertexData.Add(new VertexPositionTexture((float)(int)x + -0.5f, (float)(int)y + num2, (float)(int)z + -0.5f, rect2.xMin, rect2.yMin));
			blockGraphic.vertexData.Add(new VertexPositionTexture((float)(int)x + -0.5f, (float)(int)y + num3, (float)(int)z + 0.5f, rect2.xMin, rect2.yMax));
			blockGraphic.vertexData.Add(new VertexPositionTexture((float)(int)x + 0.5f, (float)(int)y + num4, (float)(int)z + -0.5f, rect2.xMax, rect2.yMin));
			blockGraphic.vertexData.Add(new VertexPositionTexture((float)(int)x + 0.5f, (float)(int)y + num5, (float)(int)z + 0.5f, rect2.xMax, rect2.yMax));
			blockGraphic.triangles.Add((ushort)(count + 3));
			blockGraphic.triangles.Add((ushort)(count + 2));
			blockGraphic.triangles.Add((ushort)(count + 1));
			blockGraphic.triangles.Add((ushort)(count + 2));
			blockGraphic.triangles.Add((ushort)count);
			blockGraphic.triangles.Add((ushort)(count + 1));
		}
		if (flag2)
		{
			Rect rect3 = rect;
			if (blockData.customSidesDict.ContainsKey(BlockSides.bottom))
			{
				BlockSideTextureInfo blockSideTextureInfo2 = blockData.customSidesDict[BlockSides.bottom];
				rect3 = new Rect(blockSideTextureInfo2.uStart, blockSideTextureInfo2.vStart, blockSideTextureInfo2.uvWidth, blockSideTextureInfo2.uvHeight);
			}
			int count2 = blockGraphic.vertexData.Count;
			blockGraphic.vertexData.Add(new VertexPositionTexture((float)(int)x + -0.5f, (float)(int)y + -0.5f, (float)(int)z + -0.5f, rect3.xMin, rect3.yMax));
			blockGraphic.vertexData.Add(new VertexPositionTexture((float)(int)x + -0.5f, (float)(int)y + -0.5f, (float)(int)z + 0.5f, rect3.xMin, rect3.yMin));
			blockGraphic.vertexData.Add(new VertexPositionTexture((float)(int)x + 0.5f, (float)(int)y + -0.5f, (float)(int)z + -0.5f, rect3.xMax, rect3.yMax));
			blockGraphic.vertexData.Add(new VertexPositionTexture((float)(int)x + 0.5f, (float)(int)y + -0.5f, (float)(int)z + 0.5f, rect3.xMax, rect3.yMin));
			blockGraphic.triangles.Add((ushort)(count2 + 1));
			blockGraphic.triangles.Add((ushort)count2);
			blockGraphic.triangles.Add((ushort)(count2 + 2));
			blockGraphic.triangles.Add((ushort)(count2 + 3));
			blockGraphic.triangles.Add((ushort)(count2 + 1));
			blockGraphic.triangles.Add((ushort)(count2 + 2));
		}
		if (flag5)
		{
			Rect rect4 = rect;
			if (blockData.customSidesDict.ContainsKey(BlockSides.left))
			{
				BlockSideTextureInfo blockSideTextureInfo3 = blockData.customSidesDict[BlockSides.left];
				rect4 = new Rect(blockSideTextureInfo3.uStart, blockSideTextureInfo3.vStart, blockSideTextureInfo3.uvWidth, blockSideTextureInfo3.uvHeight);
			}
			int count3 = blockGraphic.vertexData.Count;
			blockGraphic.vertexData.Add(new VertexPositionTexture((float)(int)x + -0.5f, (float)(int)y + -0.5f, (float)(int)z + -0.5f, rect4.xMax, rect4.yMin));
			blockGraphic.vertexData.Add(new VertexPositionTexture((float)(int)x + -0.5f, (float)(int)y + -0.5f, (float)(int)z + 0.5f, rect4.xMin, rect4.yMin));
			blockGraphic.vertexData.Add(new VertexPositionTexture((float)(int)x + -0.5f, (float)(int)y + num2, (float)(int)z + -0.5f, rect4.xMax, rect4.yMax));
			blockGraphic.vertexData.Add(new VertexPositionTexture((float)(int)x + -0.5f, (float)(int)y + num3, (float)(int)z + 0.5f, rect4.xMin, rect4.yMax));
			blockGraphic.triangles.Add((ushort)count3);
			blockGraphic.triangles.Add((ushort)(count3 + 1));
			blockGraphic.triangles.Add((ushort)(count3 + 2));
			blockGraphic.triangles.Add((ushort)(count3 + 1));
			blockGraphic.triangles.Add((ushort)(count3 + 3));
			blockGraphic.triangles.Add((ushort)(count3 + 2));
		}
		if (flag)
		{
			Rect rect5 = rect;
			if (blockData.customSidesDict.ContainsKey(BlockSides.right))
			{
				BlockSideTextureInfo blockSideTextureInfo4 = blockData.customSidesDict[BlockSides.right];
				rect5 = new Rect(blockSideTextureInfo4.uStart, blockSideTextureInfo4.vStart, blockSideTextureInfo4.uvWidth, blockSideTextureInfo4.uvHeight);
			}
			int count4 = blockGraphic.vertexData.Count;
			blockGraphic.vertexData.Add(new VertexPositionTexture((float)(int)x + 0.5f, (float)(int)y + -0.5f, (float)(int)z + -0.5f, rect5.xMin, rect5.yMin));
			blockGraphic.vertexData.Add(new VertexPositionTexture((float)(int)x + 0.5f, (float)(int)y + -0.5f, (float)(int)z + 0.5f, rect5.xMax, rect5.yMin));
			blockGraphic.vertexData.Add(new VertexPositionTexture((float)(int)x + 0.5f, (float)(int)y + num4, (float)(int)z + -0.5f, rect5.xMin, rect5.yMax));
			blockGraphic.vertexData.Add(new VertexPositionTexture((float)(int)x + 0.5f, (float)(int)y + num5, (float)(int)z + 0.5f, rect5.xMax, rect5.yMax));
			blockGraphic.triangles.Add((ushort)(count4 + 1));
			blockGraphic.triangles.Add((ushort)count4);
			blockGraphic.triangles.Add((ushort)(count4 + 2));
			blockGraphic.triangles.Add((ushort)(count4 + 3));
			blockGraphic.triangles.Add((ushort)(count4 + 1));
			blockGraphic.triangles.Add((ushort)(count4 + 2));
		}
		if (flag3)
		{
			Rect rect6 = rect;
			if (blockData.customSidesDict.ContainsKey(BlockSides.front))
			{
				BlockSideTextureInfo blockSideTextureInfo5 = blockData.customSidesDict[BlockSides.front];
				rect6 = new Rect(blockSideTextureInfo5.uStart, blockSideTextureInfo5.vStart, blockSideTextureInfo5.uvWidth, blockSideTextureInfo5.uvHeight);
			}
			int count5 = blockGraphic.vertexData.Count;
			blockGraphic.vertexData.Add(new VertexPositionTexture((float)(int)x + -0.5f, (float)(int)y + -0.5f, (float)(int)z + -0.5f, rect6.xMin, rect6.yMin));
			blockGraphic.vertexData.Add(new VertexPositionTexture((float)(int)x + -0.5f, (float)(int)y + num2, (float)(int)z + -0.5f, rect6.xMin, rect6.yMax));
			blockGraphic.vertexData.Add(new VertexPositionTexture((float)(int)x + 0.5f, (float)(int)y + -0.5f, (float)(int)z + -0.5f, rect6.xMax, rect6.yMin));
			blockGraphic.vertexData.Add(new VertexPositionTexture((float)(int)x + 0.5f, (float)(int)y + num4, (float)(int)z + -0.5f, rect6.xMax, rect6.yMax));
			blockGraphic.triangles.Add((ushort)count5);
			blockGraphic.triangles.Add((ushort)(count5 + 1));
			blockGraphic.triangles.Add((ushort)(count5 + 2));
			blockGraphic.triangles.Add((ushort)(count5 + 1));
			blockGraphic.triangles.Add((ushort)(count5 + 3));
			blockGraphic.triangles.Add((ushort)(count5 + 2));
		}
		if (flag4)
		{
			Rect rect7 = rect;
			if (blockData.customSidesDict.ContainsKey(BlockSides.back))
			{
				BlockSideTextureInfo blockSideTextureInfo6 = blockData.customSidesDict[BlockSides.back];
				rect7 = new Rect(blockSideTextureInfo6.uStart, blockSideTextureInfo6.vStart, blockSideTextureInfo6.uvWidth, blockSideTextureInfo6.uvHeight);
			}
			int count6 = blockGraphic.vertexData.Count;
			blockGraphic.vertexData.Add(new VertexPositionTexture((float)(int)x + -0.5f, (float)(int)y + -0.5f, (float)(int)z + 0.5f, rect7.xMax, rect7.yMin));
			blockGraphic.vertexData.Add(new VertexPositionTexture((float)(int)x + -0.5f, (float)(int)y + num3, (float)(int)z + 0.5f, rect7.xMax, rect7.yMax));
			blockGraphic.vertexData.Add(new VertexPositionTexture((float)(int)x + 0.5f, (float)(int)y + -0.5f, (float)(int)z + 0.5f, rect7.xMin, rect7.yMin));
			blockGraphic.vertexData.Add(new VertexPositionTexture((float)(int)x + 0.5f, (float)(int)y + num5, (float)(int)z + 0.5f, rect7.xMin, rect7.yMax));
			blockGraphic.triangles.Add((ushort)(count6 + 1));
			blockGraphic.triangles.Add((ushort)count6);
			blockGraphic.triangles.Add((ushort)(count6 + 2));
			blockGraphic.triangles.Add((ushort)(count6 + 3));
			blockGraphic.triangles.Add((ushort)(count6 + 1));
			blockGraphic.triangles.Add((ushort)(count6 + 2));
		}
	}

	public IEnumerator ConstructBlock(int blockTypeNr, ushort x, ushort y, ushort z)
	{
		if (x < 0 || y < 0 || z < 0 || x >= worldSizeX || y >= worldHeight || z >= worldSizeZ)
		{
			yield break;
		}
		Vector3 vector = GetSpawnpoint();
		if (x == (int)vector.x && (y == (int)vector.y - 1 || y == (int)vector.y) && z == (int)vector.z)
		{
			yield break;
		}
		AddBlock(blockTypeNr, x, y, z);
		if (DedicatedServer.isDedicated)
		{
			yield break;
		}
		if (Network.isServer)
		{
			Chunk chunkForWorldPos = GetChunkForWorldPos(x, y, z);
			if (!clientRegisteredChunksAccepted.Contains(chunkForWorldPos))
			{
				yield break;
			}
		}
		PlayWorldSound(createSound, x, y, z);
		if (GetLocalPlayerDistance(x, y, z) <= 10)
		{
			EnablePhysics(x, y, z);
		}
		yield return 0;
		ChangedAreaOfPos(x, y, z);
	}

	public IEnumerator DestroyBlock(ushort x, ushort y, ushort z)
	{
		if (!ContainsBlock(x, y, z))
		{
			Debug.LogError("Trying to destroy block that doesnt exist!");
			yield break;
		}
		KillBlock(x, y, z);
		if (DedicatedServer.isDedicated)
		{
			yield break;
		}
		if (Network.isServer)
		{
			Chunk chunkForWorldPos = GetChunkForWorldPos(x, y, z);
			if (!clientRegisteredChunksAccepted.Contains(chunkForWorldPos))
			{
				yield break;
			}
		}
		if (GetLocalPlayerDistance(x, y, z) < 10)
		{
			ReCalculatePhysics(new Vector3((int)x, (int)y, (int)z));
		}
		PlayWorldSound(digSound, x, y, z);
		yield return 0;
		ChangedAreaOfPos(x, y, z);
	}

	public void KillBlock(ushort x, ushort y, ushort z)
	{
		BlockInstance blockInstanceQuick = GetBlockInstanceQuick(x, y, z);
		if (blockInstanceQuick != null)
		{
			RemoveBlockInstance(x, y, z);
			Vector3 vector = new Vector3((int)x, (int)y, (int)z);
			physicBlocks.Remove(vector);
			if ((bool)blockInstanceQuick.physicTransform)
			{
				Object.Destroy(blockInstanceQuick.physicTransform.gameObject);
			}
		}
	}

	public bool ContainsBlock(ushort x, ushort y, ushort z)
	{
		if (xWorldData.ContainsKey(x))
		{
			Dictionary<ushort, Dictionary<ushort, BlockInstance>> dictionary = xWorldData[x];
			if (dictionary.ContainsKey(y))
			{
				return dictionary[y].ContainsKey(z);
			}
		}
		return false;
	}

	private void AddBlockInstance(ushort x, ushort y, ushort z, BlockInstance bInstance)
	{
		if (xWorldData.ContainsKey(x))
		{
			Dictionary<ushort, Dictionary<ushort, BlockInstance>> dictionary = xWorldData[x];
			if (dictionary.ContainsKey(y))
			{
				dictionary[y].Add(z, bInstance);
				return;
			}
			Dictionary<ushort, BlockInstance> dictionary2 = new Dictionary<ushort, BlockInstance>();
			dictionary2.Add(z, bInstance);
			dictionary.Add(y, dictionary2);
		}
		else
		{
			Dictionary<ushort, Dictionary<ushort, BlockInstance>> dictionary3 = new Dictionary<ushort, Dictionary<ushort, BlockInstance>>();
			xWorldData.Add(x, dictionary3);
			Dictionary<ushort, BlockInstance> dictionary4 = new Dictionary<ushort, BlockInstance>();
			dictionary4.Add(z, bInstance);
			dictionary3.Add(y, dictionary4);
		}
	}

	public void RemoveBlockInstance(ushort x, ushort y, ushort z)
	{
		if (xWorldData.ContainsKey(x))
		{
			Dictionary<ushort, Dictionary<ushort, BlockInstance>> dictionary = xWorldData[x];
			if (dictionary.ContainsKey(y))
			{
				dictionary[y].Remove(z);
				return;
			}
		}
		Debug.LogError("RemoveBlockInstance = null!" + x + " " + y + " " + z);
		Debug.Break();
	}

	public BlockInstance GetBlockInstanceQuick(ushort x, ushort y, ushort z)
	{
		Dictionary<ushort, Dictionary<ushort, BlockInstance>> value = null;
		if (xWorldData.TryGetValue(x, out value))
		{
			Dictionary<ushort, BlockInstance> value2 = null;
			if (value.TryGetValue(y, out value2))
			{
				BlockInstance value3 = null;
				if (value2.TryGetValue(z, out value3))
				{
					return value3;
				}
			}
		}
		return null;
	}

	public BlockData GetBlockData(ushort x, ushort y, ushort z)
	{
		return GetBlockInstanceQuick(x, y, z)?.blockData;
	}

	private int GetOptimalAreaSize(int xStart, int yStart, int zStart, int areaSize)
	{
		int closestPlayerDistance = GetClosestPlayerDistance(xStart + areaSize / 2, yStart + areaSize / 2, zStart + areaSize / 2);
		if (closestPlayerDistance <= 10)
		{
			return networkChunkSize / 8;
		}
		if (closestPlayerDistance <= 30)
		{
			return networkChunkSize / 4;
		}
		if (closestPlayerDistance <= 50)
		{
			return networkChunkSize / 2;
		}
		return networkChunkSize;
	}

	private Chunk GetChunkForWorldPos(int x, int y, int z)
	{
		return new Chunk(x / networkChunkSize, y / networkChunkSize, z / networkChunkSize, networkChunkSize);
	}

	[RPC]
	private IEnumerator DoneSendingChunk(int chunkX, int chunkY, int chunkZ, int messages)
	{
		Chunk chunk = new Chunk(chunkX, chunkY, chunkZ, networkChunkSize);
		if (clientRegisteredChunksAccepted.Contains(chunk))
		{
			Debug.LogError("DoneSendingChunk Accepting chunk twice :S!  " + chunkX + " " + chunkY + " " + chunkZ);
			yield break;
		}
		if (!clientRegisteredChunksRequests.Contains(chunk))
		{
			Debug.LogWarning("DoneSendingChunk Dismissing chunk info " + chunkX + " " + chunkY + " " + chunkZ);
			yield break;
		}
		float timestart = Time.realtimeSinceStartup;
		while (messages > receivedChunkParts[chunk] && Time.realtimeSinceStartup < timestart + 60f)
		{
			yield return 0;
		}
		RecombineArea((ushort)(chunkX * networkChunkSize), (ushort)(chunkY * networkChunkSize), (ushort)(chunkZ * networkChunkSize), (ushort)networkChunkSize);
		clientRegisteredChunksRequests.Remove(chunk);
		receivedChunkParts.Remove(chunk);
		clientRegisteredChunksAccepted.Add(chunk);
		if (clientWaitingForChunk.x == chunk.x && chunk.y == clientWaitingForChunk.y && chunk.z == clientWaitingForChunk.z)
		{
			SetPlayerMovement(allowed: true);
		}
	}

	private void CheckChunks()
	{
		int num = chunkAddDistance;
		Chunk chunkForWorldPos = GetChunkForWorldPos((int)localPlayerTransform.position.x, (int)localPlayerTransform.position.y, (int)localPlayerTransform.position.z);
		if (chunkForWorldPos.x >= 0 && chunkForWorldPos.y >= 0 && chunkForWorldPos.z >= 0 && !clientRegisteredChunksAccepted.Contains(chunkForWorldPos))
		{
			Debug.LogError(localPlayerTransform.position.y + "Waiting for " + chunkForWorldPos.x + " " + chunkForWorldPos.y + " " + chunkForWorldPos.z);
			SetPlayerMovement(allowed: false);
			clientWaitingForChunk = chunkForWorldPos;
			GameMenu.SP.SetLoadingStatus("Loading current world chunk...");
			ClientAddChunk(chunkForWorldPos);
		}
		if (lastClientChunk.x == chunkForWorldPos.x && lastClientChunk.y == chunkForWorldPos.y && lastClientChunk.z == chunkForWorldPos.z)
		{
			return;
		}
		lastClientChunk = chunkForWorldPos;
		for (int i = Mathf.Max(0, chunkForWorldPos.x - num); i < Mathf.Min(chunkForWorldPos.x + num, 1 + GetWorldSizeX() / networkChunkSize); i++)
		{
			for (int j = Mathf.Max(0, chunkForWorldPos.y - num); j < Mathf.Min(chunkForWorldPos.y + num, 1 + GetWorldSizeY() / networkChunkSize); j++)
			{
				for (int k = Mathf.Max(0, chunkForWorldPos.z - num); k < Mathf.Min(chunkForWorldPos.z + num, 1 + GetWorldSizeZ() / networkChunkSize); k++)
				{
					if (Mathf.Abs(chunkForWorldPos.x - i) + Mathf.Abs(chunkForWorldPos.y - j) + Mathf.Abs(chunkForWorldPos.z - k) <= chunkAddDistance)
					{
						ClientAddChunk(new Chunk(i, j, k, networkChunkSize));
					}
				}
			}
		}
		for (int num2 = clientRegisteredChunksRequests.Count - 1; num2 >= 0; num2--)
		{
			Chunk newChunk = clientRegisteredChunksRequests[num2];
			if (Mathf.Abs(chunkForWorldPos.x - newChunk.x) + Mathf.Abs(chunkForWorldPos.y - newChunk.y) + Mathf.Abs(chunkForWorldPos.z - newChunk.z) > chunkRemoveDistance)
			{
				ClientDeleteChunk(newChunk);
			}
		}
		for (int num3 = clientRegisteredChunksAccepted.Count - 1; num3 >= 0; num3--)
		{
			Chunk newChunk2 = clientRegisteredChunksAccepted[num3];
			if (Mathf.Abs(chunkForWorldPos.x - newChunk2.x) + Mathf.Abs(chunkForWorldPos.y - newChunk2.y) + Mathf.Abs(chunkForWorldPos.z - newChunk2.z) > chunkRemoveDistance)
			{
				ClientDeleteChunk(newChunk2);
			}
		}
	}

	private void ClientAddChunk(Chunk newChunk)
	{
		if (clientRegisteredChunksAccepted.Contains(newChunk) || clientRegisteredChunksRequests.Contains(newChunk))
		{
			return;
		}
		if (newChunk.x < 0 || newChunk.y < 0 || newChunk.z < 0)
		{
			Debug.LogWarning("Nonsense chunk register 1: " + newChunk.x + " " + newChunk.y + " " + newChunk.z);
			return;
		}
		if (GetWorldSizeX() / networkChunkSize < newChunk.x || GetWorldSizeY() / networkChunkSize < newChunk.y || GetWorldSizeZ() / networkChunkSize < newChunk.z)
		{
			Debug.LogWarning("Nonsense chunk register 2: " + newChunk.x + " " + newChunk.y + " " + newChunk.z);
			return;
		}
		clientRegisteredChunksRequests.Add(newChunk);
		receivedChunkParts.Add(newChunk, 0);
		if (Network.isServer)
		{
			Server_RegisterClientChunkLOCAL(newChunk.x, newChunk.y, newChunk.z, Network.player);
			return;
		}
		base.networkView.RPC("Server_RegisterClientChunk", RPCMode.Server, newChunk.x, newChunk.y, newChunk.z);
	}

	private void ClientDeleteChunk(Chunk newChunk)
	{
		bool flag = clientRegisteredChunksRequests.Contains(newChunk);
		bool flag2 = clientRegisteredChunksAccepted.Contains(newChunk);
		if (!flag && !flag2)
		{
			Debug.LogWarning("ClientDeleteChunk: Doesnt have chunk anymore " + newChunk.x + " " + newChunk.y + " " + newChunk.z);
			return;
		}
		if (flag)
		{
			clientRegisteredChunksRequests.Remove(newChunk);
			receivedChunkParts.Remove(newChunk);
		}
		if (flag2)
		{
			clientRegisteredChunksAccepted.Remove(newChunk);
		}
		int num = newChunk.x * networkChunkSize;
		int num2 = num + networkChunkSize;
		int num3 = newChunk.y * networkChunkSize;
		int num4 = num3 + networkChunkSize;
		int num5 = newChunk.z * networkChunkSize;
		int num6 = num5 + networkChunkSize;
		if (Network.isServer)
		{
			Server_UnregisterClientChunkLOCAL(newChunk.x, newChunk.y, newChunk.z, Network.player);
		}
		else
		{
			base.networkView.RPC("Server_UnregisterClientChunk", RPCMode.Server, newChunk.x, newChunk.y, newChunk.z);
		}
		if (!flag2)
		{
			return;
		}
		List<GameObject> destroyList = new List<GameObject>();
		destroyList = DestroyPreviousCombined(destroyList, (ushort)num, (ushort)num3, (ushort)num5, networkChunkSize);
		foreach (GameObject item in destroyList)
		{
			if (!(item == null) && !(item.GetComponent<MeshFilter>() == null))
			{
				Object.Destroy(item.GetComponent<MeshFilter>().mesh);
				Object.Destroy(item);
			}
		}
		if (Network.isServer)
		{
			return;
		}
		foreach (ushort key in xWorldData.Keys)
		{
			if (key < num || key >= num2)
			{
				continue;
			}
			foreach (ushort key2 in xWorldData[key].Keys)
			{
				if (key2 < num3 || key2 >= num4)
				{
					continue;
				}
				List<ushort> list = new List<ushort>(xWorldData[key][key2].Keys);
				list.Sort();
				foreach (ushort item2 in list)
				{
					if (item2 >= num5)
					{
						if (item2 >= num6)
						{
							break;
						}
						KillBlock(key, key2, item2);
					}
				}
			}
		}
	}

	[RPC]
	private void Server_RegisterClientChunk(int x, int y, int z, NetworkMessageInfo info)
	{
		Server_RegisterClientChunkLOCAL(x, y, z, info.sender);
	}

	private void Server_RegisterClientChunkLOCAL(int x, int y, int z, NetworkPlayer player)
	{
		Chunk chunk = new Chunk(x, y, z, networkChunkSize);
		PlayerNode player2 = GameManager.SP.GetPlayer(player);
		List<PlayerNode> value = null;
		if (serverChunkRegistrations.TryGetValue(chunk, out value))
		{
			if (value.Contains(player2))
			{
				Debug.LogError("ERROR: Double chunk add!");
				return;
			}
			value.Add(player2);
		}
		else
		{
			value = new List<PlayerNode>();
			value.Add(player2);
			serverChunkRegistrations.Add(chunk, value);
		}
		StartCoroutine(ServerSendChunkData(chunk, player2.networkPlayer));
	}

	[RPC]
	private void Server_UnregisterClientChunk(int x, int y, int z, NetworkMessageInfo info)
	{
		Server_UnregisterClientChunkLOCAL(x, y, z, info.sender);
	}

	private void Server_UnregisterClientChunkLOCAL(int x, int y, int z, NetworkPlayer player)
	{
		Chunk key = new Chunk(x, y, z, networkChunkSize);
		PlayerNode player2 = GameManager.SP.GetPlayer(player);
		List<PlayerNode> value = null;
		if (serverChunkRegistrations.TryGetValue(key, out value))
		{
			if (!value.Contains(player2))
			{
				Debug.LogError("ERROR: couldnt even remove!");
				return;
			}
			value.Remove(player2);
			if (value.Count == 0)
			{
				serverChunkRegistrations.Remove(key);
			}
		}
		else
		{
			Debug.LogError("pnodes was leeg :S!");
		}
	}

	public void ServerRemovePlayer(PlayerNode pNode)
	{
		foreach (List<PlayerNode> value in serverChunkRegistrations.Values)
		{
			value.Remove(pNode);
		}
	}

	public List<PlayerNode> GetServerPlayerUpdateList(int x, int y, int z)
	{
		Chunk chunkForWorldPos = GetChunkForWorldPos(x, y, z);
		List<PlayerNode> value = null;
		serverChunkRegistrations.TryGetValue(chunkForWorldPos, out value);
		return value;
	}
}
