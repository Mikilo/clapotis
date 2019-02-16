using System;
using System.Collections.Generic;

namespace NGToolsEditor.NGConsole
{
	public interface IStreams
	{
		event Action<StreamLog>	StreamAdded;
		event Action<StreamLog>	StreamDeleted;

		List<StreamLog>	Streams { get; }
		int				WorkingStream { get; }

		void			FocusStream(int i);
		void			DeleteStream(int i);
	}
}