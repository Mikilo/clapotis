using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGAssetFinder
{
	internal class CSharpFinder : ObjectFinder
	{
		public	CSharpFinder(NGAssetFinderWindow window) : base(window)
		{
		}

		public override bool	CanFind(Object asset)
		{
			return asset is MonoScript;
		}

		public override void	Find(AssetMatches assetMatches, Object asset, AssetFinder finder, SearchResult result)
		{
			SerializedObject	so = new SerializedObject(asset);
			SerializedProperty	property = so.FindProperty("m_DefaultReferences.Array");

			if (property != null)
			{
				string					lastString = string.Empty;
				SerializedProperty		end = property.GetEndProperty();
				SerializedPropertyType	type = property.propertyType;

				while (property.Next(type == SerializedPropertyType.Generic) == true &&
					   SerializedProperty.EqualContents(property, end) == false)
				{
					type = property.propertyType;

					if (type == SerializedPropertyType.String)
						lastString = property.stringValue;
					else if (type == SerializedPropertyType.ObjectReference)
					{
						++result.potentialMatchesCount;
						if (property.objectReferenceValue == this.window.TargetAsset)
						{
							assetMatches.matches.Add(new Match(asset, property.propertyPath) { nicifiedPath = Utility.NicifyVariableName(lastString) });
							++result.effectiveMatchesCount;
						}
					}
				}
			}
		}
	}
}