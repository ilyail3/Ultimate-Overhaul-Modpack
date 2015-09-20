using RimWorld;
using Verse;
using Verse.AI;

namespace Hospitality
{
    public class WorkGiver_Recruiter : WorkGiver_Scanner
    {
        private readonly JobDef jobDefRecruit = DefDatabase<JobDef>.GetNamed("GuestRecruit");
        private readonly JobDef jobDefWakeUp = DefDatabase<JobDef>.GetNamed("GuestWakeUp");

        public override bool HasJobOnThing(Pawn pawn, Thing t)
        {
            var guest = t as Pawn;

            if (!GuestUtility.ViableGuestTarget(guest, true)) return false;
            if(guest.guest.interactionMode != PrisonerInteractionMode.AttemptRecruit) return false;
            if (guest.BrokenStateDef != null)return false;
            if (!pawn.CanReserveAndReach(guest, PathEndMode.OnCell, pawn.NormalMaxDanger())) return false;
            if (!guest.Awake()) return false;

            return true;
        }

        public override bool ShouldSkip(Pawn pawn)
        {
            return pawn.story.WorkTagIsDisabled(def.workType.workTags);
        }

        public override Job JobOnThing(Pawn pawn, Thing t)
        {
            return new Job(jobDefRecruit, t);
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
