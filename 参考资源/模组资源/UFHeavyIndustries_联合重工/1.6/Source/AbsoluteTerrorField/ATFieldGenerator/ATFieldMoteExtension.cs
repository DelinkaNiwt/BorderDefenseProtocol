using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ATFieldGenerator;

public class ATFieldMoteExtension : DefModExtension
{
	public Color colorStart = Color.white;

	public Color colorEnd = new Color(1f, 1f, 1f, 0f);

	public List<Color> colors;

	public float duration = 0.4f;
}
