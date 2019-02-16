using System;
using System.Reflection;
using System.Text;

namespace NGTools.NGRemoteScene
{
	public sealed class ClientClass
	{
		public sealed class Field
		{
			public readonly Type			fieldType;
			public readonly string			name;
			public readonly bool			isPublic;
			public readonly TypeHandler		handler;
			public readonly TypeSignature	typeSignature;

			public object	value;

			public	Field(NetField field)
			{
				this.fieldType = field.fieldType;
				this.name = field.name;
				this.isPublic = field.isPublic;
				this.handler = field.handler;
				this.typeSignature = field.typeSignature;
				this.value = field.value;
			}

			public override string	ToString()
			{
				return (this.isPublic == true ? "public ": "private ") + (this.fieldType ?? this.handler.type) + ' ' + this.name;
			}
		}

		public Field[]	fields { get; private set; }

		public static void	Serialize(ByteBuffer buffer, Type fieldType, object instance)
		{
			using (SafeWrapByteBuffer.Get(buffer))
			{
				if (instance == null)
				{
					buffer.Append(-1);
					return;
				}

				FieldInfo[]	fields = fieldType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

				int	lengthPosition = buffer.Length;
				int	countFieldsAppended = 0;
				buffer.Append(0);

				for (int i = 0; i < fields.Length; i++)
				{
					if (Utility.CanExposeFieldInInspector(fields[i]) == false)
						continue;

					NetField.Serialize(buffer, instance, new FieldModifier(fields[i]));
					++countFieldsAppended;
				}

				if (countFieldsAppended > 0)
				{
					int	restoreLengthPosition = buffer.Length;
					buffer.Length = lengthPosition;
					buffer.Append(countFieldsAppended);
					buffer.Length = restoreLengthPosition;
				}
			}
		}

		public static ClientClass	Deserialize(ByteBuffer buffer)
		{
			return new ClientClass(buffer);
		}

		private	ClientClass(ByteBuffer buffer)
		{
			using (SafeUnwrapByteBuffer.Get(buffer, this.GetError))
			{
				int	length = buffer.ReadInt32();

				if (length > -1)
				{
					this.fields = new Field[length];
					for (int i = 0; i < this.fields.Length; i++)
						this.fields[i] = new Field(NetField.Deserialize(buffer));
				}
			}
		}

		public void	SetAll(Field[] fields)
		{
			this.fields = fields;
		}

		public void	SetValue(string field, object value)
		{
			for (int i = 0; i < this.fields.Length; i++)
			{
				if (this.fields[i].name == field)
				{
					this.fields[i].value = value;
					return;
				}
			}

			throw new ArgumentException("Field \"" + field + "\" was not found.");
		}

		public object	GetValue(string field)
		{
			for (int i = 0; i < this.fields.Length; i++)
			{
				if (this.fields[i].name == field)
					return this.fields[i].value;
			}

			throw new ArgumentException("Field \"" + field + "\" was not found.");
		}

		public Type		GetType(string field)
		{
			for (int i = 0; i < this.fields.Length; i++)
			{
				if (this.fields[i].name == field)
					return this.fields[i].fieldType;
			}

			throw new ArgumentException("Field \"" + field + "\" was not found.");
		}

		private void	Initialize(int fieldCount)
		{
			this.fields = new Field[fieldCount];
		}

		private string	GetError()
		{
			if (this.fields != null)
			{
				StringBuilder	buffer = Utility.GetBuffer("Class with " + this.fields.Length + " fields");

				for (int i = 0; i < this.fields.Length; i++)
				{
					if (this.fields[i] != null)
					{
						buffer.AppendLine();
						buffer.Append(this.fields[i].ToString());
					}
				}

				return Utility.ReturnBuffer(buffer);
			}

			return "Class empty";
		}

		public override string	ToString()
		{
			return "GenericClass()";
		}
	}

	[Priority(1000)]
	internal sealed class ClassHandler : TypeHandler
	{
		public	ClassHandler() : base(typeof(ClientClass))
		{
		}

		public override bool	CanHandle(Type type)
		{
			return (type == typeof(ClientClass) || type.IsClass() == true || type.IsStruct() == true || type.IsInterface() == true) && type.IsUnityArray() == false;
		}

		public override void	Serialize(ByteBuffer buffer, Type fieldType, object instance)
		{
			ClientClass.Serialize(buffer, fieldType, instance);
		}

		public override object	Deserialize(ByteBuffer buffer, Type fieldType)
		{
			return ClientClass.Deserialize(buffer);
		}
	}
}