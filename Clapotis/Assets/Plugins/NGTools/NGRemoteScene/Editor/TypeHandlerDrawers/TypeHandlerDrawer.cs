using NGTools.Network;
using NGTools.NGRemoteScene;
using System;
using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
	public abstract class TypeHandlerDrawer
	{
		protected readonly TypeHandler	typeHandler;

		protected	TypeHandlerDrawer(TypeHandler typeHandler)
		{
			this.typeHandler = typeHandler;
		}

		public virtual float	GetHeight(object value)
		{
			return Constants.SingleLineHeight;
		}

		public abstract void	Draw(Rect r, DataDrawer data);

		/// <summary>
		/// Checks if the given packet can be sent and sends it.
		/// </summary>
		/// <param name="unityData"></param>
		/// <param name="valuePath"></param>
		/// <param name="value"></param>
		/// <param name="type"></param>
		protected bool	AsyncUpdateCommand(IUnityData unityData, string valuePath, object value, Type type)
		{
			return unityData.AddPacket(new ClientUpdateFieldValuePacket(valuePath, this.typeHandler.Serialize(type, value), this.typeHandler), p =>
			{
				if (p.CheckPacketStatus() == true)
				{
					ServerUpdateFieldValuePacket	packet = p as ServerUpdateFieldValuePacket;

					unityData.UpdateFieldValue(packet.fieldPath, packet.rawValue);
				}
			});
		}

		protected bool	AsyncUpdateCommand(IUnityData unityData, string valuePath, object value, Type type, TypeHandler customTypeHandler)
		{
			return unityData.AddPacket(new ClientUpdateFieldValuePacket(valuePath, customTypeHandler.Serialize(type, value), customTypeHandler), p =>
			{
				if (p.CheckPacketStatus() == true)
				{
					ServerUpdateFieldValuePacket packet = p as ServerUpdateFieldValuePacket;

					unityData.UpdateFieldValue(packet.fieldPath, packet.rawValue);
				}
			});
		}
	}
}