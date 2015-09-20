using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Hospitality
{
    public class JobDriver_GuestImproveRelationship : JobDriver_GuestBase
    {
        private static readonly string txtRecruitSuccess = "MessageRecruitSuccess".Translate(); // from core
        private static readonly string txtRecruitFail = "MessageRecruitFail".Translate(); // from core
        private static readonly string txtRecruitFailMood = "RecruitFailMood".Translate();
        private static readonly string txtRecruitAngerSelfM = "RecruitAngerSelfM".Translate();
        private static readonly string txtRecruitAngerSelfF = "RecruitAngerSelfF".Translate();
        private static readonly string txtRecruitPleaseSelfM = "RecruitPleaseSelfM".Translate();
        private static readonly string txtRecruitPleaseSelfF = "RecruitPleaseSelfF".Translate();
        private static readonly string txtRecruitAngerOther = "RecruitAngerOther".Translate();
        private static readonly string txtImproveFactionAnger = "ImproveFactionAnger".Translate();
        private static readonly string txtImproveFactionPlease = "ImproveFactionPlease".Translate();
        
        private static readonly StatDef statOffendGuestChance = DefDatabase<StatDef>.GetNamed("OffendGuestChance");

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return GotoGuest(pawn, Talkee, guest => guest.ImproveRelationship());
            yield return TryImproveRelationship(pawn, Talkee);
        }

        public static Toil TryImproveRelationship(Pawn recruiter, Pawn talkee)
        {
            var toil = new Toil
            {
                initAction = () => {
                    if (!GuestUtility.ViableGuestTarget(talkee)) return;
                    if (!talkee.ImproveRelationship()) return;
                    if (talkee.Faction.ColonyGoodwill >= 100) return;
                    if (!recruiter.CanTalkTo(talkee)) return;
                    recruiter.talker.TryTalkTo(new SpeechMessage(), talkee);
                },
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration = 350,
                finishActions = new List<Action> {() => { var success = TryGuestImproveRelationship(recruiter, talkee); }}
            };
            toil.AddFailCondition(() => !GuestUtility.ViableGuestTarget(talkee) || !talkee.ImproveRelationship());
            return toil;
        }

        public static bool TryGuestImproveRelationship(Pawn recruiter, Pawn guest)
        {
            if (recruiter == null || guest == null || guest.guest == null) return false;

            recruiter.skills.Learn(SkillDefOf.Social, 25f);
            float chance = recruiter.GetStatValue(statOffendGuestChance);
			bool result = false;
            if (Rand.Value < chance)
            {
                //Log.Message("textAnger");
                Messages.Message(string.Format(txtImproveFactionAnger, recruiter.NameStringShort, guest.Faction.name, chance.ToStringPercent()), MessageSound.Negative);
				float impact = -(Rand.Range(3, 6) * (1 - 0.0375f * recruiter.skills.GetSkill(SkillDefOf.Social).level)); //Social skill level 20 reduces negative impact by 75%, Skilllevel 0 results in full impact; linear
                guest.Faction.AffectGoodwillWith(Faction.OfColony, impact);
                guest.needs.mood.thoughts.TryGainThought(ThoughtDef.Named("GuestOffendedRelationship"));
				//Log.Message("Goodwill changed by : " + impact);
            }
            else
            {
                //Log.Message("textPlease");
                Messages.Message(string.Format(txtImproveFactionPlease, recruiter.NameStringShort, guest.Faction.name, (1 - chance).ToStringPercent()), MessageSound.Benefit);
                guest.Faction.AffectGoodwillWith(Faction.OfColony, 0.1f + 0.095f * recruiter.skills.GetSkill(SkillDefOf.Social).level); //Social skilllevel 20 results in +2 relationship, Skilllevel 0 results in +0.1 change, linear
                guest.needs.mood.thoughts.TryGainThought(ThoughtDef.Named("GuestPleasedRelationship"));
                result = true;
				//Log.Message("Goodwill changed by : " + (0.1f + 0.05f * recruiter.skills.GetSkill(SkillDefOf.Social).level));
            }

			if (Rand.Value < GuestUtility.GetDismissiveChance(guest))
			{
				//Log.Message("DismissiveChance SUCCESS - " + guest.Name + " will not talk with you for a while!");
				//Message einbauen
				guest.needs.mood.thoughts.TryGainThought(ThoughtDef.Named("GuestDismissiveAttitude"));
			}
			//else Log.Message("DismissiveChance FAILED - " + guest.Name + " will continue to talk with you!");
			return result;
        }
    }
}