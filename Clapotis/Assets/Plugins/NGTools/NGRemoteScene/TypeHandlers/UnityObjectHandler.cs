using System;
#if NETFX_CORE
using System.Reflection;
#endif

namespace NGTools.NGRemoteScene
{
	using UnityEngine;

	public class UnityObject
	{
		public Type		type { get; private set; }
		public int		gameObjectInstanceID { get; private set; }
		public int		instanceID { get; private set; }
		public string	name { get; private set; }

		public static void	Serialize(ByteBuffer buffer, Type fieldType, UnityObject o)
		{
			using (SafeWrapByteBuffer.Get(buffer))
			{
				buffer.AppendUnicodeString((o.instanceID != 0 ? o.type : fieldType).GetShortAssemblyType());
				buffer.Append(o.gameObjectInstanceID);
				buffer.Append(o.instanceID);
				if (o.instanceID != 0)
					buffer.AppendUnicodeString(o.name);
			}
		}

		public static void	Serialize(ByteBuffer buffer, Type fieldType, Object o)
		{
			using (SafeWrapByteBuffer.Get(buffer))
			{
				if (o != null)
				{
					GameObject	gameObject = o as GameObject;

					buffer.AppendUnicodeString(o.GetType().GetShortAssemblyType());

					if (gameObject != null)
						buffer.Append(gameObject.GetInstanceID());
					else
					{
						Component	component = o as Component;

						if (component != null)
							buffer.Append(component.gameObject.GetInstanceID());
						else
							buffer.Append(0);
					}

					int	instanceID = o.GetInstanceID();
					buffer.Append(instanceID);
					if (instanceID != 0)
						buffer.AppendUnicodeString(o.name);
				}
				else
				{
					buffer.AppendUnicodeString(fieldType.GetShortAssemblyType());
					buffer.Append(0);
					buffer.Append(0);
				}
			}
		}

		public static UnityObject	Deserialize(ByteBuffer buffer)
		{
			return new UnityObject(buffer);
		}

		public	UnityObject()
		{
			// Default type if unknown. Should be temporary.
			this.type = typeof(Object);
			this.name = string.Empty;
		}

		public	UnityObject(Type type, int instanceID)
		{
			this.type = type;
			this.gameObjectInstanceID = instanceID;
			this.instanceID = instanceID;
			this.name = string.Empty;
		}

		private	UnityObject(ByteBuffer buffer)
		{
			using (SafeUnwrapByteBuffer.Get(buffer, this.GetError))
			{
				this.type = Type.GetType(buffer.ReadUnicodeString());

				// In case of corrupted data.
				if (this.type == null)
					this.type = typeof(Object);

				this.gameObjectInstanceID = buffer.ReadInt32();

				if (this.gameObjectInstanceID != -1)
				{
					this.instanceID = buffer.ReadInt32();
					if (this.instanceID != 0)
						this.name = buffer.ReadUnicodeString();
				}
			}
		}

		public void	Assign(Type type, int gameObjectInstanceID, int instanceID, string name)
		{
			this.type = type;
			this.gameObjectInstanceID = gameObjectInstanceID;
			this.instanceID = instanceID;
			this.name = name;
		}

		private string	GetError()
		{
			return "UnityObject " + this.name + "(" + this.type + ", " + this.gameObjectInstanceID + ", " + this.instanceID + ")";
		}
	}

	[Priority(10)]
	internal sealed class UnityObjectHandler : TypeHandler
	{
		public	UnityObjectHandler() : base(typeof(UnityObject))
		{
		}

		public override bool	CanHandle(Type type)
		{
			return type == typeof(UnityObject) || typeof(Object).IsAssignableFrom(type) == true;
		}

		public override void	Serialize(ByteBuffer buffer, Type fieldType, object instance)
		{
			UnityObject	unityObject = instance as UnityObject;

			if (unityObject != null)
			{
				UnityObject.Serialize(buffer, fieldType, unityObject);
				return;
			}


			Object	refUnityObject = instance as Object;

			UnityObject.Serialize(buffer, fieldType, refUnityObject);
		}

		public override object	Deserialize(ByteBuffer buffer, Type fieldType)
		{
			return UnityObject.Deserialize(buffer);
		}

		public override object	DeserializeRealValue(NGServerScene manager, ByteBuffer buffer, Type fieldType)
		{
			UnityObject	unityObject = this.Deserialize(buffer, fieldType) as UnityObject;

			if (unityObject.instanceID == 0)
				return null;
			return manager.GetResource(unityObject.type, unityObject.instanceID);
		}
	}
}