using NGTools;
using NGTools.NGRemoteScene;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
	[CustomPropertyDrawer(typeof(ListingShaders))]
	internal sealed class ListingShadersDrawer : PropertyDrawer
	{
		private SerializedProperty	shaders;
		private SerializedProperty	properties;

		public override void	OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (this.shaders == null)
			{
				this.shaders = property.FindPropertyRelative("shaders");
				this.properties = property.FindPropertyRelative("properties");
			}

			if (this.shaders.arraySize == 0)
			{
				position.width -= 75F;
				EditorGUI.LabelField(position, "Shaders", "No shader referenced.");

				position.x += position.width;
				position.width = 75F;
			}
			else
			{
				position.width -= 150F;

				EditorGUI.LabelField(position, "Shaders", this.shaders.arraySize + " (" + this.properties.arraySize + "B)");

				position.x += position.width;
				position.width = 75F;

				using (BgColorContentRestorer.Get(GeneralStyles.HighlightActionButton))
				{
					if (GUI.Button(position, "Clear") == true)
					{
						this.shaders.ClearArray();
						this.properties.ClearArray();
						this.shaders.serializedObject.ApplyModifiedProperties();
					}
				}

				position.x += position.width;
			}

			using (BgColorContentRestorer.Get(GeneralStyles.HighlightActionButton))
			{
				if (GUI.Button(position, "Scan") == true &&
					((Event.current.modifiers & Constants.ByPassPromptModifier) != 0 || EditorUtility.DisplayDialog("Confirm", "Referencing all shaders might take a while. It will scan all the files in the Assets folder.", LC.G("Yes"), LC.G("No")) == true))
				{
					try
					{
						this.ReferenceAll();
					}
					catch (Exception ex)
					{
						InternalNGDebug.LogException("The scan failed seeking for all shaders.", ex);
					}
				}
			}
		}

		private void	ReferenceAll()
		{
			try
			{
				string[]	assets = Directory.GetFiles(Application.dataPath, "*", SearchOption.AllDirectories);

				for (int i = 0; i < assets.Length; i++)
				{
					if (assets[i].EndsWith(".meta") == true || assets[i].EndsWith(".shader") == false)
						continue;

					string	p = assets[i].Substring(Application.dataPath.Length - "Assets".Length);
					AssetDatabase.LoadAssetAtPath<Shader>(p);

					EditorUtility.DisplayProgressBar(Constants.PackageTitle, "Scanning assets (" + i + " / " + assets.Length + ")", (float)i / (float)assets.Length);
				}

				Shader[]		shaders = Resources.FindObjectsOfTypeAll<Shader>();
				List<Shader>	references = new List<Shader>();

				for (int i = 0; i < shaders.Length; i++)
				{
					if ((shaders[i].hideFlags & HideFlags.DontSave) == 0)
						references.Add(shaders[i]);
				}

				NGServerScene	scene = this.shaders.serializedObject.targetObject as NGServerScene;

				//scene.shaderReferences.shaders = references.ToArray();

				EditorUtility.DisplayProgressBar(Constants.PackageTitle, "Saving shaders...", 0.99F);
				this.shaders.arraySize = references.Count;
				for (int i = 0; i < references.Count; i++)
					this.shaders.GetArrayElementAtIndex(i).objectReferenceValue = references[i];

				this.shaders.serializedObject.ApplyModifiedProperties();

				EditorUtility.DisplayProgressBar(Constants.PackageTitle, "Saving shaders' properties...", 1F);
				ByteBuffer	buffer = Utility.GetBBuffer();
				buffer.Append(references.Count);

				for (int i = 0; i < references.Count; i++)
					NGShaderUtility.SerializeShader(references[i], buffer);

				scene.shaderReferences.properties = Utility.ReturnBBuffer(buffer);
				//byte[]	raw = Utility.ReturnBBuffer(buffer);

				//this.properties.arraySize = raw.Length;
				//for (int i = 0; i < raw.Length; i++)
				//	this.properties.GetArrayElementAtIndex(i).intValue = raw[i];
				this.shaders.serializedObject.Update();
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}
		}
	}
}