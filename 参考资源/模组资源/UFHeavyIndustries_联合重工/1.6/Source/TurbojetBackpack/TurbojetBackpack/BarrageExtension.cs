using System.Collections.Generic;
using Verse;

namespace TurbojetBackpack;

public class BarrageExtension : DefModExtension
{
	public float curveVarianceMin = 2f;

	public float curveVarianceMax = 10f;

	public List<TailData> tailLayers = new List<TailData>();

	public int burstCount = 8;

	public float randomFireRadius = 3.9f;

	public bool extraStun = false;

	public float stunRadius = 0f;

	public int stunAmount = 15;

	public bool extraFire = false;

	public float fireRadius = 0f;

	public float fireChance = 0.5f;

	public int fireDamageAmount = 10;
}
