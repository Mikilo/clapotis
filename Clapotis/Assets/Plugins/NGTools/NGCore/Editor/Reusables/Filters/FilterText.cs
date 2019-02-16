using System;
using System.Collections.Generic;

namespace NGToolsEditor
{
	[Serializable]
	public class FilterText : Filter
	{
		public string	text;
	}

	public static class FilterTextExtension
	{
		public static bool	IsKeywordFilteredInBy(this IEnumerable<FilterText> filters, string text)
		{
			bool	isIncluded = false;
			bool	isExcluded = false;
			bool	onlyExclusive = true;

			foreach (FilterText filter in filters)
			{
				if (filter.active == false || string.IsNullOrEmpty(filter.text) == true)
					continue;

				if (filter.type == Filter.Type.Inclusive)
					onlyExclusive = false;

				if (text.Contains(filter.text) == true)
				{
					if (filter.type == Filter.Type.Inclusive)
						isIncluded = true;
					else
						isExcluded = true;
				}
			}

			if (onlyExclusive == true)
				return !isExcluded;
			return isIncluded && !isExcluded;
		}

		public static bool	IsPathFilteredIn(this IEnumerable<FilterText> filters, string path)
		{
			FilterText	closestFilter = null;
			bool		onlyExclusive = true;

			foreach (FilterText filter in filters)
			{
				if (filter.active == false)
					continue;

				if (filter.type == Filter.Type.Inclusive)
					onlyExclusive = false;

				if (path.StartsWith(filter.text) == true)
				{
					if (closestFilter == null || closestFilter.text.Length < filter.text.Length)
						closestFilter = filter;
				}
			}

			if (onlyExclusive == true)
				return closestFilter == null;
			else
			{
				if (closestFilter != null)
					return closestFilter.type == Filter.Type.Inclusive;
				return false;
			}
		}
	}
}