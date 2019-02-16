using NGTools.UON;
using UnityEngine;

namespace NGToolsEditor.NGConsole
{
	using System;

	[Serializable]
	[Exportable(ExportableAttribute.ArrayOptions.Overwrite)]
	public sealed class ColorMarker : ISerializationCallbackReceiver
	{
		[Exportable("r", "g", "b", "a")]
		public Color		backgroundColor;
		[Exportable(ExportableAttribute.ArrayOptions.Immutable)]
		public GroupFilters	groupFilters = new GroupFilters() { canSelect = false };

		[SerializeField]
		private string	groupFiltersUON;

		public	ColorMarker()
		{
			Random	r = new Random();

			this.backgroundColor.r = (float)r.NextDouble();
			this.backgroundColor.g = (float)r.NextDouble();
			this.backgroundColor.b = (float)r.NextDouble();
			this.backgroundColor.a = .3F;
		}

		void	ISerializationCallbackReceiver.OnAfterDeserialize()
		{
			if (string.IsNullOrEmpty(this.groupFiltersUON) == false)
				this.groupFilters = UON.FromUON(this.groupFiltersUON) as GroupFilters;
		}

		void	ISerializationCallbackReceiver.OnBeforeSerialize()
		{
			this.groupFiltersUON = UON.ToUON(this.groupFilters);
		}
	}
}