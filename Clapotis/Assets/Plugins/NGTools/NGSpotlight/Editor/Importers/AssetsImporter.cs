using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Video;

namespace NGToolsEditor.NGSpotlight
{
	public static class AssetsImporter
	{
		[SpotlightUpdatingResult]
		private static void	LazyImport()
		{
			NGSpotlightWindow.UpdatingResult -= AssetsImporter.LazyImport;

			if (HQ.Settings != null)
				AssetPreview.SetPreviewTextureCacheSize(HQ.Settings.Get<SpotlightSettings>().maxResult + 50); // Keep a pretty good margin, so scrolling should not have any flickering.

			NGSpotlightWindow.AddFilter(new DefaultFilter(0, 1 << 0, UtilityResources.GameObjectIcon, typeof(SceneGameObjectDrawer), ":go", "Game Object in scenes."));
			NGSpotlightWindow.AddFilter(new DefaultFilter(0, 1 << 0, ".prefab", typeof(PrefabDrawer), ":p", "Prefabs."));
			// Menu Item family : 1 << 1
			// Extension file family : 1 << 2
			NGSpotlightWindow.AddFilter(new DefaultFilter(0, 1 << 3, ".cs", typeof(CSharpDrawer), ":cs", "C# files."));
			NGSpotlightWindow.AddFilter(new DefaultFilter(0, 1 << 4, ".unity", typeof(SceneDrawer), ":s", "Unity scenes."));
			NGSpotlightWindow.AddFilter(new DefaultFilter(0, 1 << 5, ".mat", typeof(Material), ":m", "Materials."));
			NGSpotlightWindow.AddFilter(new DefaultFilter(0, 1 << 6, ".shader", typeof(Shader), ":sh", "Shaders."));
			NGSpotlightWindow.AddFilter(new DefaultFilter(0, 1 << 7, InternalEditorUtility.GetIconForFile(".png"), typeof(Texture), ":t", "Textures."));
			NGSpotlightWindow.AddFilter(new DefaultFilter(0, 1 << 8, InternalEditorUtility.GetIconForFile(".3df"), typeof(Mesh), ":me", "Meshes."));
			NGSpotlightWindow.AddFilter(new DefaultFilter(0, 1 << 9, InternalEditorUtility.GetIconForFile(".aac"), typeof(AudioClip), ":ac", "Audio clips."));
			NGSpotlightWindow.AddFilter(new DefaultFilter(0, 1 << 10, InternalEditorUtility.GetIconForFile(".ttf"), typeof(Font), ":f", "Fonts."));
			NGSpotlightWindow.AddFilter(new DefaultFilter(0, 1 << 11, InternalEditorUtility.GetIconForFile(".asf"), typeof(VideoPlayer), ":v", "Videos."));

			string[]	fullAssetPaths = AssetDatabase.GetAllAssetPaths();

			for (int i = 0; i < fullAssetPaths.Length; i++)
			{
				if ((fullAssetPaths[i].StartsWith("Assets/") == false &&
					 fullAssetPaths[i].StartsWith("ProjectSettings/") == false &&
					 fullAssetPaths[i].StartsWith("Library/") == false) ||
					Directory.Exists(fullAssetPaths[i]) == true)
				{
					continue;
				}

				string	ext = Path.GetExtension(fullAssetPaths[i]).ToLower();

				if (ext == ".cs")
					NGSpotlightWindow.AddEntry("cs", new CSharpDrawer(fullAssetPaths[i]));
				else if (ext == ".prefab")
					NGSpotlightWindow.AddEntry("prefab", new PrefabDrawer(fullAssetPaths[i]));
				else if (ext == ".mat")
					NGSpotlightWindow.AddEntry("mat", new DefaultAssetDrawer(fullAssetPaths[i], typeof(Material)));
				else if (ext == ".shader")
					NGSpotlightWindow.AddEntry("shader", new DefaultAssetDrawer(fullAssetPaths[i], typeof(Shader)));
				else if (ext == ".ai" ||
						 ext == ".apng" ||
						 ext == ".png" ||
						 ext == ".bmp" ||
						 ext == ".cdr" ||
						 ext == ".dib" ||
						 ext == ".eps" ||
						 ext == ".exif" ||
						 ext == ".gif" ||
						 ext == ".ico" ||
						 ext == ".icon" ||
						 ext == ".j" ||
						 ext == ".j2c" ||
						 ext == ".j2k" ||
						 ext == ".jas" ||
						 ext == ".jiff" ||
						 ext == ".jng" ||
						 ext == ".jp2" ||
						 ext == ".jpc" ||
						 ext == ".jpe" ||
						 ext == ".jpeg" ||
						 ext == ".jpf" ||
						 ext == ".jpg" ||
						 ext == ".jpw" ||
						 ext == ".jpx" ||
						 ext == ".jtf" ||
						 ext == ".mac" ||
						 ext == ".omf" ||
						 ext == ".qif" ||
						 ext == ".qti" ||
						 ext == ".qtif" ||
						 ext == ".tex" ||
						 ext == ".tfw" ||
						 ext == ".tga" ||
						 ext == ".tif" ||
						 ext == ".tiff" ||
						 ext == ".wmf" ||
						 ext == ".psd" ||
						 ext == ".exr" ||
						 ext == ".hdr")
				{
					NGSpotlightWindow.AddEntry("texture", new DefaultAssetDrawer(fullAssetPaths[i], typeof(Texture), true));
				}
				else if (ext == ".3df" ||
						 ext == ".3dm" ||
						 ext == ".3dmf" ||
						 ext == ".3ds" ||
						 ext == ".3dv" ||
						 ext == ".3dx" ||
						 ext == ".blend" ||
						 ext == ".c4d" ||
						 ext == ".lwo" ||
						 ext == ".lws" ||
						 ext == ".ma" ||
						 ext == ".max" ||
						 ext == ".mb" ||
						 ext == ".mesh" ||
						 ext == ".obj" ||
						 ext == ".vrl" ||
						 ext == ".wrl" ||
						 ext == ".wrz" ||
						 ext == ".fbx")
				{
					NGSpotlightWindow.AddEntry("mesh", new DefaultAssetDrawer(fullAssetPaths[i], typeof(Mesh), true));
				}
				else if (ext == ".aac" ||
						 ext == ".aif" ||
						 ext == ".aiff" ||
						 ext == ".au" ||
						 ext == ".mid" ||
						 ext == ".midi" ||
						 ext == ".mp3" ||
						 ext == ".mpa" ||
						 ext == ".ra" ||
						 ext == ".ram" ||
						 ext == ".wma" ||
						 ext == ".wav" ||
						 ext == ".wave" ||
						 ext == ".ogg")
				{
					NGSpotlightWindow.AddEntry("audioclip", new DefaultAssetDrawer(fullAssetPaths[i], typeof(AudioClip), true));
				}
				else if (ext == ".ttf" ||
						 ext == ".otf" ||
						 ext == ".fon" ||
						 ext == ".fnt")
				{
					NGSpotlightWindow.AddEntry("font", new DefaultAssetDrawer(fullAssetPaths[i], typeof(Font)));
				}
				else if (ext == ".asf" ||
						 ext == ".asx" ||
						 ext == ".avi" ||
						 ext == ".dat" ||
						 ext == ".divx" ||
						 ext == ".dvx" ||
						 ext == ".mlv" ||
						 ext == ".m2l" ||
						 ext == ".m2t" ||
						 ext == ".m2ts" ||
						 ext == ".m2v" ||
						 ext == ".m4e" ||
						 ext == ".m4v" ||
						 ext == ".mjp" ||
						 ext == ".mov" ||
						 ext == ".movie" ||
						 ext == ".mp21" ||
						 ext == ".mp4" ||
						 ext == ".mpe" ||
						 ext == ".mpeg" ||
						 ext == ".mpg" ||
						 ext == ".mpv2" ||
						 ext == ".ogm" ||
						 ext == ".qt" ||
						 ext == ".rm" ||
						 ext == ".rmvb" ||
						 ext == ".wmw" ||
						 ext == ".xvid")
				{
					NGSpotlightWindow.AddEntry("video", new DefaultAssetDrawer(fullAssetPaths[i], typeof(VideoPlayer), true));
				}
				else if (ext == ".unity")
					NGSpotlightWindow.AddEntry("unity", new SceneDrawer(fullAssetPaths[i]));
				else
					NGSpotlightWindow.AddEntry("assets", new DefaultAssetDrawer(fullAssetPaths[i], typeof(Object)));
			}
		}
	}
}