using System;

namespace NGTools.NGRemoteScene
{
	[Priority(0)]
	internal sealed class EnumHandler : TypeHandler
	{
		public	EnumHandler() : base(typeof(EnumInstance))
		{
		}

		public override bool	CanHandle(Type type)
		{
			return type.IsEnum() == true || type == typeof(Enum) || type == typeof(EnumInstance);
		}

		public override void	Serialize(ByteBuffer buffer, Type fieldType, object instance)
		{
			EnumInstance.Serialize(buffer, fieldType, instance);
		}

		public override object	Deserialize(ByteBuffer buffer, Type fieldType)
		{
			return EnumInstance.Deserialize(buffer);
		}

		public override object	DeserializeRealValue(NGServerScene manager, ByteBuffer buffer, Type fieldType)
		{
			Int32	count = buffer.ReadInt32();
			buffer.Position += count;
			return buffer.ReadInt32();
		}
	}

	internal class EnumInstance
	{
		public enum IsFlag
		{
			Unset,
			Value,
			Flag
		}

		public string	type;
		public int		value;

		private IsFlag	flag;

		public static void	Serialize(ByteBuffer buffer, Type type, object v)
		{
			buffer.AppendUnicodeString(type.GetShortAssemblyType());
			buffer.Append((Int32)v);
		}

		public static EnumInstance	Deserialize(ByteBuffer buffer)
		{
			return new EnumInstance(buffer);
		}

		private	EnumInstance(ByteBuffer buffer)
		{
			this.flag = IsFlag.Unset;
			this.type = buffer.ReadUnicodeString();
			this.value = buffer.ReadInt32();
		}

		public IsFlag	GetFlag()
		{
			return this.flag;
		}

		public void	SetFlag(IsFlag flag)
		{
			this.flag = flag;
		}
	}
}