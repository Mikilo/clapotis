using System;
using System.Text;

namespace NGTools.NGGameConsole
{
	using UnityEngine;

	public class ObjectCountData : DataConsole
	{
		[Serializable]
		public class ObjectTarget
		{
			[Header("Depending on what numbers you are looking for.")]
			public Source		source;
			public ObjectTypes	type;
		}

		public enum Source
		{
			Object,
			Resources,
		}

		public enum ObjectTypes
		{
			Object,
			GameObject,
			Material,
			Mesh,
			Shader,
		}

		private static Type[]	types = { typeof(Object), typeof(GameObject), typeof(Material), typeof(Mesh), typeof(Shader) };

		public ObjectTarget[]	targets;
		[Header("Interval in second.")]
		public float		refreshInterval = 5F;

		private float	nextRefresh = 0F;
		private int		objectCount = 0;

		public override bool	HasUpdateData()
		{
			return true;
		}

		public override void	UpdateData()
		{
			if (this.nextRefresh < Time.time)
			{
				this.nextRefresh = Time.time + this.refreshInterval;

				StringBuilder buffer = Utility.GetBuffer();

				buffer.Length = 0;

				for (int i = 0; i < this.targets.Length; i++)
				{
					if (this.targets[i].source == Source.Object)
						this.objectCount = Object.FindObjectsOfType(ObjectCountData.types[(int)this.targets[i].type]).Length;
					else if (this.targets[i].source == Source.Resources)
						this.objectCount = Resources.FindObjectsOfTypeAll(ObjectCountData.types[(int)this.targets[i].type]).Length;
					buffer.AppendLine(ObjectCountData.types[(int)this.targets[i].type].Name + " Count : " + this.objectCount);
				}

				if (buffer.Length >= Environment.NewLine.Length)
					buffer.Length -= Environment.NewLine.Length;
				this.label.text = Utility.ReturnBuffer(buffer);
			}
		}

		public override void	FullGUI()
		{
			if (string.IsNullOrEmpty(this.label.text) == false)
				GUILayout.Label(this.label.text, this.fullStyle);
		}

		public override string	Copy()
		{
			return this.label.text;
		}
	}
}