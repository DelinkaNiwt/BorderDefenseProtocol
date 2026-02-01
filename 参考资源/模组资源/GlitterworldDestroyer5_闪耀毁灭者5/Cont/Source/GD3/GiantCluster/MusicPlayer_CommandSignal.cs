using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.Sound;

namespace GD3
{
    public class MusicPlayer_CommandSignal : ThingWithComps, BossMusic
    {
        public SongDef Music => GDDefOf.CommandSignal;

        public bool IsPlaying
        {
            get
            {
                if (!Spawned)
                {
                    return false;
                }
                Quest quest = GDUtility.GetQuestOfThing(this);
                if (quest != null && quest.State == QuestState.Ongoing)
                {
                    return true;
                }
                return false;
            }
        }
    }
}
