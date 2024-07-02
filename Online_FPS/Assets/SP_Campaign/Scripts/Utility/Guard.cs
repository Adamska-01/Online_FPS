using System;
using System.Collections.Generic;


namespace FPS.Utility
{
	public static class Guard
	{
		/// <summary>
		/// Throws <see cref="ArgumentNullException"/> if the given <paramref name="values"/> is null or length 0.
		/// </summary>
		/// <param name="values"></param>
		/// <param name="name"></param>
		/// <exception cref="ArgumentNullException">description</exception>
		public static void AgainstIListWithNullElements<T>(IList<T>? values, string name) where T : class?
		{
			AgainstNullOrEmpty(values, name);

			for (var i = 0; i < values!.Count; i++)
				AgainstNull(values[i], name);
		}

		/// <summary>
		/// Throws <see cref="ArgumentNullException"/> if the given <paramref name="values"/> is null or length 0.
		/// </summary>
		/// <param name="values"></param>
		/// <param name="name"></param>
		/// <exception cref="ArgumentNullException">description</exception>
		public static void AgainstNullOrEmpty<T>(IList<T>? values, string name)
		{
			AgainstNull(values, name);

			if (values!.Count == 0)
				throw new ArgumentNullException(name);
		}

		/// <summary>
		/// Throws <see cref="ArgumentNullException"/> if the given <paramref name="value"/> is null or empty.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="name"></param>
		/// <exception cref="ArgumentNullException">description</exception>
		public static void AgainstNullOrEmpty(string? value, string name)
		{
			if (string.IsNullOrEmpty(value))
				throw new ArgumentNullException(name);
		}

		/// <summary>
		/// Throws <see cref="ArgumentNullException"/> if the given <paramref name="value"/> is null.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value">value to check for null.</param>
		/// <param name="name">name of value</param>
		/// <exception cref="ArgumentNullException">description</exception>
		public static void AgainstNull<T>(T? value, string name)
		{
			if (value == null)
				throw new ArgumentNullException(name);
		}

		/// <summary>
		/// Throws <see cref="ArgumentNullException"/> if the given <paramref name="value"/> is null.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value">value to check for null.</param>
		/// <param name="name">name of value</param>
		/// <returns><paramref name="value"/></returns>
		/// <exception cref="ArgumentNullException">description</exception>
		public static T AgainstNullAssignment<T>(T? value, string name)
		{
			AgainstNull(value, name);

			return value!;
		}
	}
}
