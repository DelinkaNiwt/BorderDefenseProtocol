using System;
using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;

namespace GD3
{
    public class ScriptTree
    {
        public string title;

        public string dialogue;

        public string graphic = null;

        public float drawSize = 1f;

        public float drawOffset = 0f;

        public List<ScriptButton> buttons = new List<ScriptButton>();

        public MechanoidScriptDef Parent
        {
            get
            {
                return DefDatabase<MechanoidScriptDef>.AllDefs.First(d => d.scriptTree.Contains(this));
            }
        }
    }
}
