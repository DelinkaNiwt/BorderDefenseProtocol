using System;
using System.Collections.Generic;
using System.Xml;
using HarmonyLib;
using JetBrains.Annotations;
using Verse;

namespace AlienRace;

public class AlienChanceEntry<T>
{
	[LoadAlias("defName")]
	public T entry;

	public List<AlienChanceEntry<T>> options = new List<AlienChanceEntry<T>>();

	public int count = 1;

	public float chance = 100f;

	public float commonalityMale = -1f;

	public float commonalityFemale = -1f;

	[Unsaved(false)]
	private readonly List<AlienChanceEntry<T>> shuffledOptions = new List<AlienChanceEntry<T>>();

	public bool Approved()
	{
		return (float)Rand.Range(0, 100) < chance;
	}

	public bool Approved(Gender gender)
	{
		if ((gender == Gender.Male && (commonalityMale < 0f || (float)Rand.Range(0, 100) < commonalityMale)) || (gender == Gender.Female && (commonalityFemale < 0f || (float)Rand.Range(0, 100) < commonalityFemale)) || gender == Gender.None)
		{
			return Approved();
		}
		return false;
	}

	public bool Approved(Pawn pawn)
	{
		return Approved(pawn.gender);
	}

	public IEnumerable<T> Select(Pawn pawn)
	{
		if (pawn != null)
		{
			if (!Approved(pawn))
			{
				yield break;
			}
		}
		else if (!Approved())
		{
			yield break;
		}
		if (!object.Equals(entry, default(T)))
		{
			yield return entry;
		}
		if (shuffledOptions.Count != options.Count)
		{
			shuffledOptions.Clear();
			shuffledOptions.AddRange(options);
		}
		shuffledOptions.Shuffle();
		int limit = Math.Min(shuffledOptions.Count, count);
		for (int i = 0; i < limit; i++)
		{
			foreach (T item in shuffledOptions[i].Select(pawn))
			{
				yield return item;
			}
		}
	}

	[UsedImplicitly]
	public void LoadDataFromXmlCustom(XmlNode xmlRoot)
	{
		if (xmlRoot.ChildNodes.Count == 1 && xmlRoot.FirstChild.NodeType == XmlNodeType.Text)
		{
			if (typeof(T).IsSubclassOf(typeof(Def)))
			{
				DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "entry", xmlRoot.FirstChild.Value);
			}
			else
			{
				Utilities.SetFieldFromXmlNode(Traverse.Create(this), xmlRoot, this, "entry");
			}
			return;
		}
		Traverse traverse = Traverse.Create(this);
		foreach (XmlNode xmlNode in xmlRoot.ChildNodes)
		{
			Utilities.SetFieldFromXmlNode(traverse, xmlNode, this, (xmlNode.Name == "defName") ? "entry" : xmlNode.Name);
		}
	}
}
