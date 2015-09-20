using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Hospitality
{
    public class JobDriver_GuestWakeUp : JobDriver_GuestBase
    {
        private static readonly JobDef jobDefBeAwake = DefDatabase<JobDef>.GetNamed("Wait");

        private static readonly string txtWakeUpAngerSelfM = "WakeUpAngerSelfM".Translate();
        private static readonly string txtWakeUpAngerSelfF = "WakeUpAngerSelfF".Translate();
        private static readonly StatDef statOffendGuestChance = DefDatabase<StatDef>.GetNamed("OffendGuestChance");

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return GotoGuest(pawn, Talkee, guest => guest.TryRecruit()&&!guest.Awake(), true);
            yield return TryWakeGuest(pawn, Talkee);
        }

        public static Toil TryWakeGuest(Pawn recruiter, Pawn talkee)
        {
            var toil = new Toil
            {
                initAction = () =>
                {
                    if (!GuestUtility.ViableGuestTarget(talkee, true)) return;
                    if (talkee.guest.interactionMode != PrisonerInteractionMode.AttemptRecruit) return;
                    //if (!recruiter.CanTalkTo(talkee)) return;
                    recruiter.talker.TryTalkTo(new SpeechMessage(), talkee);
                },
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration = 350,
                finishActions = new List<Action> {() => {
                    var success = TryGuestWake(recruiter, talkee);
                    Log.Message(success?"Success":"Failure");                    
                } }
            };
            toil.AddFailCondition(() => !GuestUtility.ViableGuestTarget(talkee, true) || !talkee.TryRecruit());
            return toil;
        }

        public static bool TryGuestWake(Pawn recruiter, Pawn guest)
        {
            if (recruiter == null || guest == null || guest.guest == null) return false;

            recruiter.skills.Learn(SkillDefOf.Social, 35f);

            float chance = recruiter.GetStatValue(statOffendGuestChance)*2;
            if (Rand.Value < chance)
            {
                var isAbrasive = recruiter.story.traits.HasTrait(TraitDefOf.Abrasive);
                int multiplier = isAbrasive ? 2 : 1;
                string multiplierText = multiplier > 1 ? " x" + multiplier : string.Empty;
                //Log.Message("textAnger");
                string textAnger = guest.gender == Gender.Female ? txtWakeUpAngerSelfF : txtWakeUpAngerSelfM;
                Messages.Message(string.Format(textAnger, recruiter.NameStringShort, guest.NameStringShort, chance.ToStringPercent(), multiplierText), MessageSound.Negative);
				guest.Faction.AffectGoodwillWith(Faction.OfColony, -1f + 0.045f * recruiter.skills.GetSkill(SkillDefOf.Social).level);
                for (int i = 0; i < multiplier; i++)
                {
                    guest.needs.mood.thoughts.TryGainThought(ThoughtDef.Named("GuestOffended"));
                }
            }
            else
            {
                //IntVec3 intVec = RCellFinder.RandomWanderDestFor(guest, guest.Position, 10, (p, w) => true, Danger.None);
                guest.jobs.StartJob(new Job(jobDefBeAwake, Rand.Range(2000, 8000)), JobCondition.InterruptForced);
                return true;
            }
            //Log.Message("txtRecruitFail");
            //Messages.Message(string.Format(txtRecruitFail, recruiter, guest, num.ToStringPercent()), MessageSound.Negative);
            return false;
        }
    }
}