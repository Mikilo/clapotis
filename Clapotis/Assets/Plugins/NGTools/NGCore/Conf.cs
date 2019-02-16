using System;
using System.IO;

namespace NGTools
{
	public static class Conf
	{
		public enum DebugState
		{
			None,
			Active,
			Verbose
		}

		public const string	DebugModeKeyPref = "NGTools_DebugMode";
		public const string	FileName = "debugmode.txt";

		public static Action	DebugModeChanged = null;

		private static DebugState	debugMode = DebugState.None;
		public static DebugState	DebugMode
		{
			get
			{
				return Conf.debugMode;
			}
			set
			{
				if (Conf.debugMode != value)
				{
					Conf.debugMode = value;
					if (Conf.DebugModeChanged != null)
						Conf.DebugModeChanged();
				}
			}
		}

		static	Conf()
		{
			string	path = Conf.GetPath();

			if (File.Exists(path) == true)
			{
				using (FileStream fs = File.OpenRead(path))
				{
					int	b = fs.ReadByte();

					if (b == -1)
						Conf.DebugMode = DebugState.None;
					else
						Conf.DebugMode = (DebugState)b;
				}
			}
		}

		public static void	Save()
		{
			try
			{
				File.WriteAllBytes(Conf.GetPath(), new byte[] { (byte)Conf.debugMode });
			}
			catch
			{
			}
		}

		private static string	GetPath()
		{
			string	local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

			local = Path.Combine(local, Constants.InternalPackageTitle);
			return Path.Combine(local, Conf.FileName);
		}
	}
}