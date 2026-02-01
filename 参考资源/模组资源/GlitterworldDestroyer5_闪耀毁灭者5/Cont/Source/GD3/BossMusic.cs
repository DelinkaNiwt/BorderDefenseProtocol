using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace GD3
{
    public interface BossMusic
    {
        SongDef Music { get; }

        bool IsPlaying { get; }
    }
}
