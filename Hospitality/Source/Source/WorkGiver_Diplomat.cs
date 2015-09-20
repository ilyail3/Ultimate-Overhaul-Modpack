using RimWorld;
using Verse;
using Verse.AI;
using System.Linq;

namespace Hospitality
{
    public class WorkGiver_Diplomat : WorkGiver_Scanner
    {
        private readonly JobDef jobDef = DefDatabase<JobDef>.GetNamed("GuestImproveRelationship");

        public override bool HasJobOnThing(Pawn pawn, Thing t)
        {
            var target = t as Pawn;
            if (!GuestUtility.ViableGuestTarget(target)) return false;
            if (!target.ImproveRelationship()) return false;
            if (target.Faction.ColonyGoodwill >= 100) return false;
            if (target.BrokenStateDef != null) return false;
            if (!pawn.CanReserveAndReach(target, PathEndMode.OnCell, pawn.NormalMaxDanger())) return false;

            return true;
        }

        public override bool ShouldSkip(Pawn pawn)
        {
            return pawn.story.WorkTagIsDisabled(def.workType.workTags);
        }

        public override Job JobOnThing(Pawn pawn, Thing t)
        {
            return new Job(jobDef, t);
        }

        public override ThingRequest PotentialWorkThingRequest
        {
            get
            {
                return ThingRequest.ForGroup(ThingRequestGroup.Pawn);
            }
        }

        public override PathEndMode PathEndMode
        {
            get
            {
                return PathEndMode.OnCell;
            }
        }
    }
}
