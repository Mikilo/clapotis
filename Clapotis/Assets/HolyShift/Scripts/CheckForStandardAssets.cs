using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class CheckForStandardAssets : MonoBehaviour
{
#if UNITY_EDITOR
	// Use this for initialization
	void Awake()
	{
		var guids = UnityEditor.AssetDatabase.FindAssets("FXWater4Advanced", null);
		Debug.Assert(guids.Length > 0, "Please add Unity's Standard Assets to make water works! https://www.assetstore.unity3d.com/en/#!/content/32351");
	}
#endif
}
