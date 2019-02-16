using NGTools;
using System;

namespace NGToolsEditor.NGStaticInspector
{
	using UnityEngine;

	public static class TypeDrawerManager
	{
		public static TypeDrawer	GetDrawer(string path, string label, Type type)
		{
			if (type.IsValueType == true)
			{
				if (type == typeof(Int32))
					return new Int32TypeDrawer(path, label, type);
				if (type == typeof(Single))
					return new SingleTypeDrawer(path, label, type);
				if (type == typeof(Boolean))
					return new BooleanTypeDrawer(path, label, type);
				if (type.IsEnum == true)
					return new EnumTypeDrawer(path, label, type);
				if (type == typeof(Byte))
					return new ByteTypeDrawer(path, label, type);
				if (type == typeof(SByte))
					return new SByteTypeDrawer(path, label, type);
				if (type == typeof(Char))
					return new CharTypeDrawer(path, label, type);
				if (type == typeof(Double))
					return new DoubleTypeDrawer(path, label, type);
				if (type == typeof(Int16))
					return new Int16TypeDrawer(path, label, type);
				if (type == typeof(Int64))
					return new Int64TypeDrawer(path, label, type);
				if (type == typeof(UInt16))
					return new UInt16TypeDrawer(path, label, type);
				if (type == typeof(UInt32))
					return new UInt32TypeDrawer(path, label, type);
				if (type == typeof(UInt64))
					return new UInt64TypeDrawer(path, label, type);
			}

			if (type == typeof(String))
				return new StringTypeDrawer(path, label, type);

			if (type == typeof(Color))
				return new ColorTypeDrawer(path, label, type);
			if (type == typeof(Rect))
				return new RectTypeDrawer(path, label, type);
			if (type == typeof(Vector2))
				return new Vector2TypeDrawer(path, label, type);
			if (type == typeof(Vector3))
				return new Vector3TypeDrawer(path, label, type);
			if (type == typeof(Vector4))
				return new Vector4TypeDrawer(path, label, type);
			if (type == typeof(Bounds))
				return new BoundsTypeDrawer(path, label, type);
			if (type == typeof(AnimationCurve))
				return new AnimationCurveTypeDrawer(path, label, type);

			if (typeof(Object).IsAssignableFrom(type) == true)
				return new ObjectTypeDrawer(path, label, type);

			if (type.IsUnityArray() == true)
				return new CollectionTypeDrawer(path, label, type);

			if (type.IsClass == true || type.IsStruct() == true)
				return new ClassTypeDrawer(path, label, type);

			return new UnsupportedTypeDrawer(path, label, type);
		}
	}
}