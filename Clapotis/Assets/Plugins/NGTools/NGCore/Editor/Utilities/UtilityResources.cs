using System;
using UnityEditor;
using UnityEditorInternal;

namespace NGToolsEditor
{
	using UnityEngine;

	public static class UtilityResources
	{
		private static Texture2D	ngIcon;
		public static Texture2D		NGIcon
		{
			get
			{
				if (UtilityResources.ngIcon == null)
				{
					UtilityResources.ngIcon = new Texture2D(16, 16, TextureFormat.RGBA32, false)
					{
						hideFlags = HideFlags.DontSave,
						alphaIsTransparency = true,
						filterMode = FilterMode.Point
					};
					UtilityResources.ngIcon.LoadImage(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAABGdBTUEAALGPC/xhBQAAAAlwSFlzAAAOwQAADsEBuJFr7QAAAAd0SU1FB+EHFBMBLEk0PtQAAAAYdEVYdFNvZnR3YXJlAHBhaW50Lm5ldCA0LjEuNWRHWFIAAABbSURBVDhP7Y9RCgAgCEO9eUcvlMy1lL6DHkht0zD5LDqUU2msRWaixhwraBSANlgzPOSNfJbwEGrllodBW5g3T7+n36ka3NvyyaarBvOSrcxHDpPXVJ089D4iA3ziVSYHbbS0AAAAAElFTkSuQmCC"));
				}

				return UtilityResources.ngIcon;
			}
		}

		private static Texture2D	infoIcon;
		public static Texture2D		InfoIcon { get { return UtilityResources.infoIcon ?? (UtilityResources.infoIcon = Utility.GetConsoleIcon("console.infoicon.sml")); } }

		private static Texture2D	warningIcon;
		public static Texture2D		WarningIcon { get { return UtilityResources.warningIcon ?? (UtilityResources.warningIcon = Utility.GetConsoleIcon("console.warnicon.sml")); } }

		private static Texture2D	errorIcon;
		public static Texture2D		ErrorIcon { get { return UtilityResources.errorIcon ?? (UtilityResources.errorIcon = Utility.GetConsoleIcon("console.erroricon.sml")); } }

		private static Texture2D	unityIcon;
		public static Texture2D		UnityIcon { get { return UtilityResources.unityIcon ?? (UtilityResources.unityIcon = InternalEditorUtility.GetIconForFile(".unity")); } }

		private static Texture2D	materialIcon;
		public static Texture2D		MaterialIcon { get { return UtilityResources.materialIcon ?? (UtilityResources.materialIcon = InternalEditorUtility.GetIconForFile(".mat")); } }

		private static Texture2D	shaderIcon;
		public static Texture2D		ShaderIcon { get { return UtilityResources.shaderIcon ?? (UtilityResources.shaderIcon = InternalEditorUtility.GetIconForFile(".shader")); } }
		
		private static Texture2D	cameraIcon;
		public static Texture2D		CameraIcon { get { return UtilityResources.cameraIcon ?? (UtilityResources.cameraIcon = AssetPreview.GetMiniTypeThumbnail(typeof(Camera))); } }
		
		private static Texture2D	gameObjectIcon;
		public static Texture2D		GameObjectIcon { get { return UtilityResources.gameObjectIcon ?? (UtilityResources.gameObjectIcon = AssetPreview.GetMiniTypeThumbnail(typeof(GameObject))); } }

		private static Texture2D	prefabIcon;
		public static Texture2D		PrefabIcon { get { return UtilityResources.prefabIcon ?? (UtilityResources.prefabIcon = InternalEditorUtility.GetIconForFile(".prefab")); } }

		private static Texture2D	assetIcon;
		public static Texture2D		AssetIcon { get { return UtilityResources.assetIcon ?? (UtilityResources.assetIcon = InternalEditorUtility.GetIconForFile(".default")); } }

		private static Texture2D	folderIcon;
		public static Texture2D		FolderIcon { get { return UtilityResources.folderIcon ?? (UtilityResources.folderIcon = Utility.GetIcon(AssetDatabase.LoadAssetAtPath("Assets", typeof(Object)))); } }

		private static Texture2D	cSharpIcon;
		public static Texture2D		CSharpIcon { get { return UtilityResources.cSharpIcon ?? (UtilityResources.cSharpIcon = InternalEditorUtility.GetIconForFile(".cs")); } }

		private static Texture2D	javascriptIcon;
		public static Texture2D		JavascriptIcon { get { return UtilityResources.javascriptIcon ?? (UtilityResources.javascriptIcon = InternalEditorUtility.GetIconForFile(".js")); } }
	}
}