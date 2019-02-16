using NGTools.Network;
using NGTools.NGRemoteScene;
using System;
using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
	public sealed class ClientStaticMember
	{
		public readonly static ClientStaticMember	Empty = new ClientStaticMember();

		public readonly int		declaringTypeIndex;
		public readonly Type	fieldType;
		public readonly string	name = string.Empty;
		public readonly bool	isEditable;

		public object	value;

		private readonly TypeHandlerDrawer	drawer;
		private readonly DataDrawer			dataDrawer;

		public	ClientStaticMember(int typeIndex, NetField netField, bool isEditable, IUnityData unityData)
		{
			this.declaringTypeIndex = typeIndex;
			this.fieldType = netField.fieldType;
			this.name = netField.name;
			this.value = netField.value;
			this.isEditable = isEditable;

			this.drawer = TypeHandlerDrawersManager.CreateTypeHandlerDrawer(netField.handler, this.fieldType);
			this.dataDrawer = new DataDrawer(unityData);
		}

		private	ClientStaticMember()
		{
			this.drawer = TypeHandlerDrawersManager.CreateTypeHandlerDrawer(null, null);
			this.dataDrawer = new DataDrawer(null);
		}

		public float	GetHeight(IDataDrawerTool inspector)
		{
			return this.drawer.GetHeight(this.value);
		}

		public void		Draw(Rect r, IDataDrawerTool inspector)
		{
			this.dataDrawer.Init(this.declaringTypeIndex.ToCachedString(), inspector);

			DataDrawer	drawer = this.dataDrawer.DrawChild(this.name, this.name, this.value);

			try
			{
				this.drawer.Draw(r, drawer);
			}
			catch (Exception ex)
			{
				throw new Exception("Drawing static member \"" + drawer.GetPath() + "\" failed.", ex);
			}
		}
	}
}