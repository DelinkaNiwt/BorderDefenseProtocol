using Verse;
using Verse.AI.Group;
using RimWorld;

namespace GD3
{
    public class LordJob_WaitAtPoint : LordJob
    {
        public IntVec3 point;

        public LordJob_WaitAtPoint()
        {
        }

        public LordJob_WaitAtPoint(IntVec3 point)
        {
            this.point = point;
        }

        public override StateGraph CreateGraph()
        {
            StateGraph stateGraph = new StateGraph();
            LordToil_Wait lordToil_Wait = new LordToil_Wait();
            stateGraph.AddToil(lordToil_Wait);
            stateGraph.StartingToil = lordToil_Wait;
            return stateGraph;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref point, "point");
        }
    }
}