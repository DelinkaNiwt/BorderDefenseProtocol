using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace CharacterEditor;

public static class InjectedDefHasher
{
	private delegate void GiveShortHashTakenHashes(Def def, Type defType, HashSet<ushort> takenHashes);

	private delegate void GiveShortHash(Def def, Type defType);

	private static GiveShortHash giveShortHashDelegate;

	internal static void PrepareReflection()
	{
		try
		{
			Dictionary<Type, HashSet<ushort>> takenHashesDictionary = typeof(ShortHashGiver).GetField("takenHashesPerDeftype", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) as Dictionary<Type, HashSet<ushort>>;
			if (takenHashesDictionary == null)
			{
				throw new Exception("taken hashes");
			}
			MethodInfo method = typeof(ShortHashGiver).GetMethod("GiveShortHash", BindingFlags.Static | BindingFlags.NonPublic, null, new Type[3]
			{
				typeof(Def),
				typeof(Type),
				typeof(HashSet<ushort>)
			}, null);
			if (method == null)
			{
				throw new Exception("hashing method");
			}
			GiveShortHashTakenHashes hashDelegate = (GiveShortHashTakenHashes)Delegate.CreateDelegate(typeof(GiveShortHashTakenHashes), method);
			giveShortHashDelegate = delegate(Def def, Type defType)
			{
				HashSet<ushort> hashSet = takenHashesDictionary.TryGetValue(defType);
				if (hashSet == null)
				{
					hashSet = new HashSet<ushort>();
					takenHashesDictionary.Add(defType, hashSet);
				}
				hashDelegate(def, defType, hashSet);
			};
		}
		catch (Exception ex)
		{
			Log.Error("Failed to reflect short hash dependencies: " + ex.Message);
		}
	}

	public static void GiveShortHashToDef(Def newDef, Type defType)
	{
		if (giveShortHashDelegate == null)
		{
			throw new Exception("Hasher not initialized");
		}
		giveShortHashDelegate(newDef, defType);
	}
}
