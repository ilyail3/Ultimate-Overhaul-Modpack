using RimWorld;
using RimWorld.SquadAI;
using Verse;
using Verse.AI;

namespace Hospitality
{
    internal class State_VisitPoint : State
    {
        public IntVec3 point;
        public float radius = 45f;
        public float startingGoodwill;
        public override IntVec3 FlagLoc { get { return point; } }
        public State_VisitPoint() {}

        public State_VisitPoint(IntVec3 point, float colonyGoodwill)
        {
            startingGoodwill = colonyGoodwill;
            this.point = point;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.LookValue(ref point, "point", default(IntVec3));
        }

        public override void UpdateAllDuties()
        {
            foreach (Pawn p in brain.ownedPawns)
            {
                p.mindState.duty = new PawnDuty(DefDatabase<DutyDef>.GetNamed("Relax"), point, -1f)
                {
                    focusSecond = point,
                    radius = radius
                };
            }
        }
    }
}
