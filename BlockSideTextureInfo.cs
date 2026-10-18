using System;

[Serializable]
public class BlockSideTextureInfo
{
	public int textureRectNr;

	[NonSerialized]
	public float uStart;

	[NonSerialized]
	public float vStart;

	[NonSerialized]
	public float uvWidth = 0.124f;

	[NonSerialized]
	public float uvHeight = 0.124f;
}
