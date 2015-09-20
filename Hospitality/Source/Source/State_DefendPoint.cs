using RimWorld;
using RimWorld.SquadAI;
using Verse;
using Verse.AI;

namespace Hospitality
{
    /// <summary>
    /// Copied 1:1
    /// </summary>
    internal class State_DefendPoint : State
    {
        public float defendRadius = 28f;
        public IntVec3 defendPoint;

        public override IntVec3 FlagLoc
        {
            get
            {
                return defendPoint;
            }
        }

        public State_DefendPoint()
        {
        }

        public State_DefendPoint(IntVec3 defendPoint)
        {
            this.defendPoint = defendPoint;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.LookValue(ref defendPoint, "defendPoint", new IntVec3());
        }

        public override void UpdateAllDuties()
        {
            foreach (Pawn p in brain.ownedPawns) {
                p.mindState.duty = new PawnDuty(DutyDefOf.Defend, defendPoint, -1f)
                {
                    focusSecond = defendPoint,
                    radius = defendRadius
                };
            }
        }
    }
}