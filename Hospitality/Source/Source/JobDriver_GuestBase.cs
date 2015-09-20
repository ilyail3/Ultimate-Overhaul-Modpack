using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Hospitality
{
    public abstract class JobDriver_GuestBase : JobDriver_ChatWithPrisoner
    {
        public static Toil GotoGuest(Pawn pawn, Pawn talkee, Func<Pawn,bool> condition, bool mayBeSleeping = false)
        {
            var toil = new Toil
            {
                initAction = () => pawn.pather.StartPath(talkee, PathEndMode.Touch),
                defaultCompleteMode = ToilCompleteMode.PatherArrival
            };
            toil.AddFailCondition(() => !GuestUtility.ViableGuestTarget(talkee, mayBeSleeping) || !condition(talkee));
            return toil;
        }
    }
}