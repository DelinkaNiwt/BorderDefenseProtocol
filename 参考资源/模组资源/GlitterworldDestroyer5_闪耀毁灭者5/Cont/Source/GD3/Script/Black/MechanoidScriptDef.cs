using System;
using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;

namespace GD3
{
    public class MechanoidScriptDef : Def
    {
        public int ID;

        public List<ScriptTree> scriptTree;

        public string priceKind = "None";

        public int priceNum = 500;

        public QuestScriptDef questNeedToFinish = null;

        public ScriptTree failed;

        public string branch = null;

        public int to = -1;

        public bool Ended
        {
            get
            {
                if (Find.World.GetComponent<MissionComponent>().script_Finished.Count == 0)
                {
                    return false;
                }
                if (Find.World.GetComponent<MissionComponent>().script_Finished.Contains(ID))
                {
                    return true;
                }
                return false;
            }
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string item in base.ConfigErrors())
            {
                yield return item;
            }
            if (DefDatabase<MechanoidScriptDef>.AllDefs.ToList().Any((MechanoidScriptDef m) => m != this && m.ID == ID && ID != -1))
            {
                yield return "Mechanoid Script Def: Same ID " + ID;
            }
        }
    }
}
