using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace ScriptableObjects
{
	/// <summary>
	/// Abstract class for making reload-proof singletons out of ScriptableObjects
	/// Returns the asset created on the editor, or null if there is none
	/// Based on https://www.youtube.com/watch?v=VBA1QCoEAX4
	/// </summary>
	/// <typeparam name="T">Singleton type</typeparam>

	public abstract class BadSingletonScriptableObject<T> : ScriptableObject where T : ScriptableObject
	{
		static T _instance = null;

		private static Stopwatch stopwatch = new Stopwatch();

		public static T Instance
		{
			get
			{
				if (_instance == null)
				{
					// SO might not be added to SOs manager or might be requested before the manager has awoken.
					stopwatch.Start();

					if (_instance == null)
					{
						_instance = Resources.FindObjectsOfTypeAll<T>().FirstOrDefault();
					}

					if (_instance == null)
					{
						_instance = Resources.LoadAll<T>("ScriptableObjectsSingletons").FirstOrDefault();
					}

					if (_instance == null)
					{
						UnityEngine.Debug.LogErrorFormat("SingletonScriptableObject instance for {0} not found!", typeof(T));
					}

					stopwatch.Stop();

					if (stopwatch.ElapsedMilliseconds > 2)
					{
						UnityEngine.Debug.LogWarning($"{typeof(T).FullName} SO took {stopwatch.ElapsedMilliseconds} ms to find! " +
						                              $"Try to serialize a reference to this SO singleton instead!");
					}

					stopwatch.Reset();
				}

				return _instance;
			}
		}
	}
}