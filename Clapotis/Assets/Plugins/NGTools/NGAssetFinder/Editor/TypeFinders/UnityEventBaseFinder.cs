using System;
using System.Collections;
using System.Reflection;
using UnityEngine.Events;

namespace NGToolsEditor.NGAssetFinder
{
	internal sealed class UnityEventBaseFinder : TypeFinder
	{
		private static FieldInfo	m_persistantCalls = UnityAssemblyVerifier.TryGetField(typeof(UnityEventBase), "m_PersistentCalls", BindingFlags.NonPublic | BindingFlags.Instance);
		private static FieldInfo	PersistentCallGroup;
		private static FieldInfo	PersistentCall;

		static	UnityEventBaseFinder()
		{
			Type	type = UnityAssemblyVerifier.TryGetType(typeof(UnityEventBase).Assembly, "UnityEngine.Events.PersistentCallGroup");

			if (type != null)
				UnityEventBaseFinder.PersistentCallGroup = UnityAssemblyVerifier.TryGetField(type, "m_Calls", BindingFlags.NonPublic | BindingFlags.Instance);

			type = UnityAssemblyVerifier.TryGetType(typeof(UnityEventBase).Assembly, "UnityEngine.Events.PersistentCall");
			if (type != null)
				UnityEventBaseFinder.PersistentCall = UnityAssemblyVerifier.TryGetField(type, "m_Target", BindingFlags.NonPublic | BindingFlags.Instance);

			if (UnityEventBaseFinder.PersistentCallGroup == null || UnityEventBaseFinder.PersistentCall == null)
				UnityEventBaseFinder.m_persistantCalls = null;
		}

		public	UnityEventBaseFinder(NGAssetFinderWindow window) : base(window)
		{
		}

		public override bool	CanFind(Type type)
		{
			return typeof(UnityEventBase).IsAssignableFrom(type);
		}

		internal override void	Find(Type type, object instance, Match match, IMatchCounter matchCounter)
		{
			if (UnityEventBaseFinder.m_persistantCalls == null)
				return;

			IList	l = (IList)UnityEventBaseFinder.PersistentCallGroup.GetValue(UnityEventBaseFinder.m_persistantCalls.GetValue(instance));

			for (int i = 0; i < l.Count; i++)
			{
				if ((UnityEngine.Object)UnityEventBaseFinder.PersistentCall.GetValue(l[i]) == this.window.TargetAsset)
				{
					match.arrayIndexes.Add(i);
					match.valid = true;
				}
			}

			matchCounter.AddPotentialMatchCounter(l.Count);
			if (match.valid == true)
				matchCounter.AddEffectiveMatchCounter(match.arrayIndexes.Count);
		}

		internal override UnityEngine.Object	Get(Type type, Match match, int index)
		{
			if (UnityEventBaseFinder.m_persistantCalls == null)
				return null;

			object	instance = match.Value;
			IList	l = (IList)UnityEventBaseFinder.PersistentCallGroup.GetValue(UnityEventBaseFinder.m_persistantCalls.GetValue(instance));

			if (index < l.Count)
				return (UnityEngine.Object)UnityEventBaseFinder.PersistentCall.GetValue(l[index]);
			return null;
		}

		internal override void	Set(Type type, UnityEngine.Object reference, Match match, int index)
		{
			if (UnityEventBaseFinder.m_persistantCalls == null)
				return;

			object	instance = match.Value;
			IList	l = (IList)UnityEventBaseFinder.PersistentCallGroup.GetValue(UnityEventBaseFinder.m_persistantCalls.GetValue(instance));

			if (index < l.Count)
				UnityEventBaseFinder.PersistentCall.SetValue(l[index], reference);
		}

		internal override Type	GetType(Type type)
		{
			return typeof(UnityEngine.Object);
		}
	}
}