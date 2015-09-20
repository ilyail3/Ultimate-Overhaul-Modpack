using RimWorld.SquadAI;
using Verse;

namespace Hospitality
{
    /// <summary>
    /// Copied 1:1
    /// </summary>
    public class TransitionAction_SetDefendLocalGroup : TransitionAction
    {
        public override void DoAction(Transition trans)
        {
            State_DefendPoint stateDefendPoint = (State_DefendPoint)trans.target;
            stateDefendPoint.defendPoint = stateDefendPoint.brain.ownedPawns.RandomElement().Position;
        }
    }
}