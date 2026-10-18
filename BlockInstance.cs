using UnityEngine;

public class BlockInstance
{
	public BlockData blockData;

	public Vector3 position;

	public bool neverVisible;

	public Transform physicTransform;

	public bool HasPhysics()
	{
		return physicTransform != null;
	}
}
