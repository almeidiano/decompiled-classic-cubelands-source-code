using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BlockData
{
	public int blockDataID;

	public string name;

	public BlockSoundType walkSoundsType;

	public BlockSideTextureInfo sideTextureDefaults;

	public Dictionary<BlockSides, BlockSideTextureInfo> customSidesDict = new Dictionary<BlockSides, BlockSideTextureInfo>();

	public void RecalculateUVSize(Dictionary<int, Rect> terrainTextureUVDict)
	{
		CalculateUVMapping(sideTextureDefaults, terrainTextureUVDict);
		foreach (KeyValuePair<BlockSides, BlockSideTextureInfo> item in customSidesDict)
		{
			CalculateUVMapping(item.Value, terrainTextureUVDict);
		}
	}

	private void CalculateUVMapping(BlockSideTextureInfo textInfo, Dictionary<int, Rect> terrainTextureUVDict)
	{
		Rect rect = terrainTextureUVDict[textInfo.textureRectNr];
		textInfo.uvWidth = rect.width;
		textInfo.uvHeight = rect.height;
		textInfo.uStart = rect.x;
		textInfo.vStart = rect.y;
	}
}
