using System;
using UnityEngine;

namespace NGTools
{
	public class SubClassesOfAttribute : PropertyAttribute
	{
		public readonly Type	type;

		public	SubClassesOfAttribute(Type type)
		{
			this.type = type;
		}
	}
}