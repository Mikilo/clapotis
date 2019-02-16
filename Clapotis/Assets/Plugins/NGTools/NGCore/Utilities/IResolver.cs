﻿using System;
using UnityEngine;

namespace NGTools
{
	/// <summary>
	/// <para>Implement it on a MonoBehaviour to give a more specific way to fetch a GameObject.</para>
	/// <para>e.g. persistent ID generated for units, etc...</para>
	/// <para>You can even avoid using the identifier and only fetching a GameObject based on a fully custom method (e.g based on GameObject's name without an ID).</para>
	/// </summary>
	public interface IResolver
	{
		/// <summary>
		/// Gets a resolver able to fetch a GameObject based on an identifier generated by your custom system.
		/// </summary>
		/// <param name="identifier">An identifier generated by your custom system.</param>
		/// <param name="resolver">A static method able to return a GameObject from an identifier.</param>
		void	GetResolver(out int identifier, out Func<int, GameObject> resolver);
	}
}