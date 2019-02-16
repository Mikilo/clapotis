using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor
{
	public class TipsHelper
	{
		public const char	Separator = ';';

		private string			key;
		private List<string>	disabledTips = new List<string>();

		public	TipsHelper(string key)
		{
			this.Load(key);
		}

		public void	Load(string key)
		{
			this.key = key;

			string	data = NGEditorPrefs.GetString(key);

			if (string.IsNullOrEmpty(data) == false)
				this.disabledTips.AddRange(data.Split(TipsHelper.Separator));
			else
				this.EraseAll(true);
		}

		public void	Save()
		{
			if (this.disabledTips.Count > 0)
				NGEditorPrefs.SetString(key, string.Join(TipsHelper.Separator.ToString(), this.disabledTips.ToArray()));
			else if (NGEditorPrefs.HasKey(key) == true)
				NGEditorPrefs.DeleteKey(key);
		}

		public void	EraseAll(bool silent = false)
		{
			this.disabledTips.Clear();

			if (NGEditorPrefs.HasKey(key) == true)
				NGEditorPrefs.DeleteKey(key);

			if (silent == false)
				EditorUtility.DisplayDialog(Constants.PackageTitle, "Tips have been reset.", "OK");
		}

		public bool	CanShowTip(string name)
		{
			return this.disabledTips.Contains(name) == false;
		}

		public void	DisableTip(string name)
		{
			this.disabledTips.Add(name);
		}

		public bool	HelpBox(string name, string content, MessageType messageType)
		{
			if (this.CanShowTip(name) == true)
			{
				EditorGUILayout.HelpBox(content, messageType);

				Rect	r = GUILayoutUtility.GetLastRect();
				r.xMin = r.xMax - 16F;
				r.x += 2F;
				r.y -= 2F;
				r.height = 11F;
				if (GUI.Button(r, "X") == true)
					this.DisableTip(name);

				return true;
			}

			return false;
		}
	}
}