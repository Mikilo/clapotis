using NGTools.Network;
using NGTools.NGRemoteScene;
using System;
using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
	public sealed class ClientField
	{
		public readonly int				fieldIndex;
		public readonly Type			fieldType;
		public readonly string			name;
		public readonly bool			isPublic;
		public readonly TypeSignature	typeSignature;

		public object	value;

		public readonly IUnityData	unityData;

		private readonly ClientComponent	parentBehaviour;
		private readonly TypeHandlerDrawer	drawer;
		private readonly DataDrawer			dataDrawer;

		public	ClientField(ClientComponent behaviour, int fieldIndex, NetField netField, IUnityData unityData)
		{
			this.unityData = unityData;
			this.parentBehaviour = behaviour;

			this.fieldIndex = fieldIndex;

			this.fieldType = netField.fieldType ?? TypeHandlersManager.GetClientType(netField.handler != null ? netField.handler.type : null, netField.typeSignature);
			this.name = netField.name;
			this.isPublic = netField.isPublic;
			this.typeSignature = netField.typeSignature;
			this.value = netField.value;

			this.drawer = TypeHandlerDrawersManager.CreateTypeHandlerDrawer(netField.handler, this.fieldType);
			this.dataDrawer = new DataDrawer(this.unityData);
		}

		public float	GetHeight(NGRemoteInspectorWindow inspector)
		{
			return this.drawer.GetHeight(this.value);
		}

		public void		Draw(Rect r, NGRemoteInspectorWindow inspector)
		{
			this.dataDrawer.Init(this.parentBehaviour.parent.instanceID.ToString() + NGServerScene.ValuePathSeparator + this.parentBehaviour.instanceID, inspector);

			DataDrawer	drawer = this.dataDrawer.DrawChild(this.fieldIndex.ToString(), this.name, this.value);

			try
			{
				this.drawer.Draw(r, drawer);
			}
			catch (Exception ex)
			{
				throw new Exception("Drawing field \"" + drawer.GetPath() + "\" failed.", ex);
			}
		}
	}
}