// File auto-generated by ExposerGenerator.
using System.Reflection;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	internal sealed class RendererExposer : ComponentExposer
	{
		public	RendererExposer() : base(typeof(Renderer))
		{
		}

		private PropertyInfo[]	cachedProperty;

		public override PropertyInfo[]	GetPropertyInfos()
		{
			if (this.cachedProperty == null)
			{
				ComponentExposer.Property.Clear();

				string	unityVersion = Application.unityVersion;

				if ((unityVersion[0] == '2' && "2018.2".CompareTo(unityVersion) <= 0) ||
					unityVersion.StartsWith("2018.1") ||
					unityVersion.StartsWith("2017.4") ||
					unityVersion.StartsWith("2017.3") ||
					unityVersion.StartsWith("2017.2") ||
					unityVersion.StartsWith("2017.1") ||
					unityVersion.StartsWith("5.6") ||
					unityVersion.StartsWith("5.5") ||
					unityVersion.StartsWith("5.4") ||
					unityVersion.StartsWith("5.3") ||
					unityVersion.StartsWith("5.2") ||
					unityVersion.StartsWith("5.1") ||
					unityVersion.StartsWith("5.0") ||
					unityVersion.StartsWith("4.7") ||
					unityVersion.StartsWith("4.6") ||
					unityVersion.StartsWith("4.5"))
				{
					ComponentExposer.Property.Add(this.type.GetProperty("enabled"));
				}

				if (unityVersion.StartsWith("4.7") ||
					unityVersion.StartsWith("4.6") ||
					unityVersion.StartsWith("4.5"))
				{
					ComponentExposer.Property.Add(this.type.GetProperty("castShadows"));
				}

				if ((unityVersion[0] == '2' && "2018.2".CompareTo(unityVersion) <= 0) ||
					unityVersion.StartsWith("2018.1") ||
					unityVersion.StartsWith("2017.4") ||
					unityVersion.StartsWith("2017.3") ||
					unityVersion.StartsWith("2017.2") ||
					unityVersion.StartsWith("2017.1") ||
					unityVersion.StartsWith("5.6") ||
					unityVersion.StartsWith("5.5") ||
					unityVersion.StartsWith("5.4") ||
					unityVersion.StartsWith("5.3") ||
					unityVersion.StartsWith("5.2") ||
					unityVersion.StartsWith("5.1") ||
					unityVersion.StartsWith("5.0") ||
					unityVersion.StartsWith("4.7") ||
					unityVersion.StartsWith("4.6") ||
					unityVersion.StartsWith("4.5"))
				{
					ComponentExposer.Property.Add(this.type.GetProperty("receiveShadows"));
				}

				if ((unityVersion[0] == '2' && "2018.2".CompareTo(unityVersion) <= 0) ||
					unityVersion.StartsWith("2018.1") ||
					unityVersion.StartsWith("2017.4") ||
					unityVersion.StartsWith("2017.3") ||
					unityVersion.StartsWith("2017.2") ||
					unityVersion.StartsWith("2017.1") ||
					unityVersion.StartsWith("5.6") ||
					unityVersion.StartsWith("5.5") ||
					unityVersion.StartsWith("5.4") ||
					unityVersion.StartsWith("5.3") ||
					unityVersion.StartsWith("5.2") ||
					unityVersion.StartsWith("5.1") ||
					unityVersion.StartsWith("5.0") ||
					unityVersion.StartsWith("4.7") ||
					unityVersion.StartsWith("4.6") ||
					unityVersion.StartsWith("4.5"))
				{
					ComponentExposer.Property.Add(this.type.GetProperty("sharedMaterials"));
				}

				if ((unityVersion[0] == '2' && "2018.2".CompareTo(unityVersion) <= 0) ||
					unityVersion.StartsWith("2018.1") ||
					unityVersion.StartsWith("2017.4") ||
					unityVersion.StartsWith("2017.3") ||
					unityVersion.StartsWith("2017.2") ||
					unityVersion.StartsWith("2017.1") ||
					unityVersion.StartsWith("5.6") ||
					unityVersion.StartsWith("5.5") ||
					unityVersion.StartsWith("5.4") ||
					unityVersion.StartsWith("5.3") ||
					unityVersion.StartsWith("5.2") ||
					unityVersion.StartsWith("5.1") ||
					unityVersion.StartsWith("5.0") ||
					unityVersion.StartsWith("4.7") ||
					unityVersion.StartsWith("4.6") ||
					unityVersion.StartsWith("4.5"))
				{
					ComponentExposer.Property.Add(this.type.GetProperty("lightmapIndex"));
				}

				if (unityVersion.StartsWith("4.7") ||
					unityVersion.StartsWith("4.6") ||
					unityVersion.StartsWith("4.5"))
				{
					ComponentExposer.Property.Add(this.type.GetProperty("lightmapTilingOffset"));
				}

				if (unityVersion.StartsWith("5.3") ||
					unityVersion.StartsWith("5.2") ||
					unityVersion.StartsWith("5.1") ||
					unityVersion.StartsWith("5.0") ||
					unityVersion.StartsWith("4.7") ||
					unityVersion.StartsWith("4.6") ||
					unityVersion.StartsWith("4.5"))
				{
					ComponentExposer.Property.Add(this.type.GetProperty("useLightProbes"));
				}

				if (unityVersion.StartsWith("4.7") ||
					unityVersion.StartsWith("4.6") ||
					unityVersion.StartsWith("4.5"))
				{
					ComponentExposer.Property.Add(this.type.GetProperty("lightProbeAnchor"));
				}

				if ((unityVersion[0] == '2' && "2018.2".CompareTo(unityVersion) <= 0) ||
					unityVersion.StartsWith("2018.1") ||
					unityVersion.StartsWith("2017.4") ||
					unityVersion.StartsWith("2017.3") ||
					unityVersion.StartsWith("2017.2") ||
					unityVersion.StartsWith("2017.1") ||
					unityVersion.StartsWith("5.6") ||
					unityVersion.StartsWith("5.5") ||
					unityVersion.StartsWith("5.4") ||
					unityVersion.StartsWith("5.3") ||
					unityVersion.StartsWith("5.2") ||
					unityVersion.StartsWith("5.1") ||
					unityVersion.StartsWith("5.0") ||
					unityVersion.StartsWith("4.7") ||
					unityVersion.StartsWith("4.6") ||
					unityVersion.StartsWith("4.5"))
				{
					ComponentExposer.Property.Add(this.type.GetProperty("sortingLayerName"));
				}

				if ((unityVersion[0] == '2' && "2018.2".CompareTo(unityVersion) <= 0) ||
					unityVersion.StartsWith("2018.1") ||
					unityVersion.StartsWith("2017.4") ||
					unityVersion.StartsWith("2017.3") ||
					unityVersion.StartsWith("2017.2") ||
					unityVersion.StartsWith("2017.1") ||
					unityVersion.StartsWith("5.6") ||
					unityVersion.StartsWith("5.5") ||
					unityVersion.StartsWith("5.4") ||
					unityVersion.StartsWith("5.3") ||
					unityVersion.StartsWith("5.2") ||
					unityVersion.StartsWith("5.1") ||
					unityVersion.StartsWith("5.0") ||
					unityVersion.StartsWith("4.7") ||
					unityVersion.StartsWith("4.6") ||
					unityVersion.StartsWith("4.5"))
				{
					ComponentExposer.Property.Add(this.type.GetProperty("sortingLayerID"));
				}

				if ((unityVersion[0] == '2' && "2018.2".CompareTo(unityVersion) <= 0) ||
					unityVersion.StartsWith("2018.1") ||
					unityVersion.StartsWith("2017.4") ||
					unityVersion.StartsWith("2017.3") ||
					unityVersion.StartsWith("2017.2") ||
					unityVersion.StartsWith("2017.1") ||
					unityVersion.StartsWith("5.6") ||
					unityVersion.StartsWith("5.5") ||
					unityVersion.StartsWith("5.4") ||
					unityVersion.StartsWith("5.3") ||
					unityVersion.StartsWith("5.2") ||
					unityVersion.StartsWith("5.1") ||
					unityVersion.StartsWith("5.0") ||
					unityVersion.StartsWith("4.7") ||
					unityVersion.StartsWith("4.6") ||
					unityVersion.StartsWith("4.5"))
				{
					ComponentExposer.Property.Add(this.type.GetProperty("sortingOrder"));
				}

				if ((unityVersion[0] == '2' && "2018.2".CompareTo(unityVersion) <= 0) ||
					unityVersion.StartsWith("2018.1") ||
					unityVersion.StartsWith("2017.4") ||
					unityVersion.StartsWith("2017.3") ||
					unityVersion.StartsWith("2017.2") ||
					unityVersion.StartsWith("2017.1") ||
					unityVersion.StartsWith("5.6") ||
					unityVersion.StartsWith("5.5") ||
					unityVersion.StartsWith("5.4") ||
					unityVersion.StartsWith("5.3") ||
					unityVersion.StartsWith("5.2") ||
					unityVersion.StartsWith("5.1") ||
					unityVersion.StartsWith("5.0"))
				{
					ComponentExposer.Property.Add(this.type.GetProperty("shadowCastingMode"));
				}

				if ((unityVersion[0] == '2' && "2018.2".CompareTo(unityVersion) <= 0) ||
					unityVersion.StartsWith("2018.1") ||
					unityVersion.StartsWith("2017.4") ||
					unityVersion.StartsWith("2017.3") ||
					unityVersion.StartsWith("2017.2") ||
					unityVersion.StartsWith("2017.1") ||
					unityVersion.StartsWith("5.6") ||
					unityVersion.StartsWith("5.5") ||
					unityVersion.StartsWith("5.4") ||
					unityVersion.StartsWith("5.3") ||
					unityVersion.StartsWith("5.2"))
				{
					ComponentExposer.Property.Add(this.type.GetProperty("realtimeLightmapIndex"));
				}

				if ((unityVersion[0] == '2' && "2018.2".CompareTo(unityVersion) <= 0) ||
					unityVersion.StartsWith("2018.1") ||
					unityVersion.StartsWith("2017.4") ||
					unityVersion.StartsWith("2017.3") ||
					unityVersion.StartsWith("2017.2") ||
					unityVersion.StartsWith("2017.1") ||
					unityVersion.StartsWith("5.6") ||
					unityVersion.StartsWith("5.5") ||
					unityVersion.StartsWith("5.4") ||
					unityVersion.StartsWith("5.3") ||
					unityVersion.StartsWith("5.2") ||
					unityVersion.StartsWith("5.1") ||
					unityVersion.StartsWith("5.0"))
				{
					ComponentExposer.Property.Add(this.type.GetProperty("lightmapScaleOffset"));
				}

				if ((unityVersion[0] == '2' && "2018.2".CompareTo(unityVersion) <= 0) ||
					unityVersion.StartsWith("2018.1") ||
					unityVersion.StartsWith("2017.4") ||
					unityVersion.StartsWith("2017.3") ||
					unityVersion.StartsWith("2017.2") ||
					unityVersion.StartsWith("2017.1") ||
					unityVersion.StartsWith("5.6") ||
					unityVersion.StartsWith("5.5"))
				{
					ComponentExposer.Property.Add(this.type.GetProperty("motionVectorGenerationMode"));
				}

				if ((unityVersion[0] == '2' && "2018.2".CompareTo(unityVersion) <= 0) ||
					unityVersion.StartsWith("2018.1") ||
					unityVersion.StartsWith("2017.4") ||
					unityVersion.StartsWith("2017.3") ||
					unityVersion.StartsWith("2017.2") ||
					unityVersion.StartsWith("2017.1") ||
					unityVersion.StartsWith("5.6") ||
					unityVersion.StartsWith("5.5") ||
					unityVersion.StartsWith("5.4") ||
					unityVersion.StartsWith("5.3") ||
					unityVersion.StartsWith("5.2") ||
					unityVersion.StartsWith("5.1") ||
					unityVersion.StartsWith("5.0"))
				{
					ComponentExposer.Property.Add(this.type.GetProperty("realtimeLightmapScaleOffset"));
				}

				if ((unityVersion[0] == '2' && "2018.2".CompareTo(unityVersion) <= 0) ||
					unityVersion.StartsWith("2018.1") ||
					unityVersion.StartsWith("2017.4") ||
					unityVersion.StartsWith("2017.3") ||
					unityVersion.StartsWith("2017.2") ||
					unityVersion.StartsWith("2017.1") ||
					unityVersion.StartsWith("5.6") ||
					unityVersion.StartsWith("5.5") ||
					unityVersion.StartsWith("5.4"))
				{
					ComponentExposer.Property.Add(this.type.GetProperty("lightProbeUsage"));
				}

				if ((unityVersion[0] == '2' && "2018.2".CompareTo(unityVersion) <= 0) ||
					unityVersion.StartsWith("2018.1") ||
					unityVersion.StartsWith("2017.4") ||
					unityVersion.StartsWith("2017.3") ||
					unityVersion.StartsWith("2017.2") ||
					unityVersion.StartsWith("2017.1") ||
					unityVersion.StartsWith("5.6") ||
					unityVersion.StartsWith("5.5") ||
					unityVersion.StartsWith("5.4"))
				{
					ComponentExposer.Property.Add(this.type.GetProperty("lightProbeProxyVolumeOverride"));
				}

				if ((unityVersion[0] == '2' && "2018.2".CompareTo(unityVersion) <= 0) ||
					unityVersion.StartsWith("2018.1") ||
					unityVersion.StartsWith("2017.4") ||
					unityVersion.StartsWith("2017.3") ||
					unityVersion.StartsWith("2017.2") ||
					unityVersion.StartsWith("2017.1") ||
					unityVersion.StartsWith("5.6") ||
					unityVersion.StartsWith("5.5") ||
					unityVersion.StartsWith("5.4") ||
					unityVersion.StartsWith("5.3") ||
					unityVersion.StartsWith("5.2") ||
					unityVersion.StartsWith("5.1") ||
					unityVersion.StartsWith("5.0"))
				{
					ComponentExposer.Property.Add(this.type.GetProperty("probeAnchor"));
				}

				if ((unityVersion[0] == '2' && "2018.2".CompareTo(unityVersion) <= 0) ||
					unityVersion.StartsWith("2018.1") ||
					unityVersion.StartsWith("2017.4") ||
					unityVersion.StartsWith("2017.3") ||
					unityVersion.StartsWith("2017.2") ||
					unityVersion.StartsWith("2017.1") ||
					unityVersion.StartsWith("5.6") ||
					unityVersion.StartsWith("5.5") ||
					unityVersion.StartsWith("5.4") ||
					unityVersion.StartsWith("5.3") ||
					unityVersion.StartsWith("5.2") ||
					unityVersion.StartsWith("5.1") ||
					unityVersion.StartsWith("5.0"))
				{
					ComponentExposer.Property.Add(this.type.GetProperty("reflectionProbeUsage"));
				}

				if ((unityVersion[0] == '2' && "2018.2".CompareTo(unityVersion) <= 0) ||
					unityVersion.StartsWith("2018.1") ||
					unityVersion.StartsWith("2017.4") ||
					unityVersion.StartsWith("2017.3") ||
					unityVersion.StartsWith("2017.2"))
				{
					ComponentExposer.Property.Add(this.type.GetProperty("allowOcclusionWhenDynamic"));
				}

				if ((unityVersion[0] == '2' && "2018.2".CompareTo(unityVersion) <= 0) ||
					unityVersion.StartsWith("2018.1"))
				{
					ComponentExposer.Property.Add(this.type.GetProperty("renderingLayerMask"));
				}

				if (unityVersion.StartsWith("5.4"))
				{
					ComponentExposer.Property.Add(this.type.GetProperty("motionVectors"));
				}

				if (ComponentExposer.Property.Count == 0)
					this.cachedProperty = ComponentExposer.EmptyArrayProperty;
				else
					this.cachedProperty = ComponentExposer.Property.ToArray();
			}

			return this.cachedProperty;
		}
	}
}