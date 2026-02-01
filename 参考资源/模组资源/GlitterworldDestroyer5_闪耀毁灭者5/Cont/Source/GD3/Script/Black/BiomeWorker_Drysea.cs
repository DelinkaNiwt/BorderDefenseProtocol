using System;
using RimWorld.Planet;
using RimWorld;
using Verse;

namespace GD3
{
    public class BiomeWorker_Drysea : BiomeWorker
    {
        public override float GetScore(BiomeDef biome, Tile tile, PlanetTile planetTile)
        {
            return -100f;
        }
    }
}
