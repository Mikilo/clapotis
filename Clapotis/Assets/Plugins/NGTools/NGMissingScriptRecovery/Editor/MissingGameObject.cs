using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;

namespace NGToolsEditor.NGMissingScriptRecovery
{
	using UnityEngine;

	[Serializable]
	internal sealed class MissingGameObject
	{
		public GameObject	gameObject;
		public string		path;

		public	MissingGameObject(GameObject go)
		{
			this.gameObject = go;

			string			assetPath = AssetDatabase.GetAssetPath(this.gameObject);
			StringBuilder	buffer = Utility.GetBuffer();
			Stack<string>	hierarchy = new Stack<string>(4);

			Transform	t = this.gameObject.transform;

			while (t != null)
			{
				hierarchy.Push(t.name);
				t = t.parent;
			}

			while (hierarchy.Count > 0)
			{
				buffer.Append(hierarchy.Pop());
				buffer.Append('/');
			}

			buffer.Length -= 1;

			this.path = assetPath.Substring(0, assetPath.LastIndexOf('/')) + '/' + Utility.ReturnBuffer(buffer);
		}
	}
}