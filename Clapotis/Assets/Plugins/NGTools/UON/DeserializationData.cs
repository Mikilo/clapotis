using System.Collections.Generic;

namespace NGTools.UON
{
	public class DeserializationData
	{
		internal readonly Dictionary<string, string>	entries = new Dictionary<string, string>(16);

		/// <summary>Fetches the string value of the given <paramref name="fieldName"/>.</summary>
		/// <param name="fieldName">The field name.</param>
		/// <returns>Value of the field saved in UON.</returns>
		public string	GetRawValue(string fieldName)
		{
			string	value;

			if (this.entries.TryGetValue(fieldName, out value) == true)
				return value;
			return null;
		}
	}
}