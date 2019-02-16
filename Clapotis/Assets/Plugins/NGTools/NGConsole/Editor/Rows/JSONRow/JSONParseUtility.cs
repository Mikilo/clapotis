namespace NGToolsEditor.NGConsole
{
	public static class JSONParseUtility
	{
		public static bool	IsBlankChar(char c)
		{
			// Skip blank characters.
			return c == ' ' ||
				   c == '\t' ||
				   c == '\r' ||
				   c == '\n';
		}

		public static int	IsOpener(char c)
		{
			for (int j = 0; j < JSONRow.Openers.Length; j++)
			{
				// Find an object or array.
				if (c == JSONRow.Openers[j][0])
					return j;
			}

			return -1;
		}

		public static int	IsCloser(char c)
		{
			for (int j = 0; j < JSONRow.Closers.Length; j++)
			{
				// Find an object or array.
				if (c == JSONRow.Closers[j][0])
					return j;
			}

			return -1;
		}

		public static int	DigestString(string raw, ref int i, int max)
		{
			int	startOffset = i;

			if (raw[i] == '"')
			{
				bool	backslashed = false;

				++i;

				for (; i < max; i++)
				{
					if (raw[i] == '"' && backslashed == false)
					{
						++i;
						break;
					}

					if (raw[i] == '\\')
						backslashed = !backslashed;
					else
						backslashed = false;
				}

				// Make sure even if the string is not properly ended with a quote.
				--i;
			}
			else
			{
				for (; i < max; i++)
				{
					if (raw[i] == JSONRow.JSONKeySeparator || raw[i] == JSONRow.JSONValueSeparator || JSONParseUtility.IsCloser(raw[i]) != -1)
					{
						--i;
						break;
					}
				}
			}

			return startOffset;
		}
	}
}