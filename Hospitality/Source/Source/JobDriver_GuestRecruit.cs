using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Hospitality
{
    public class JobDriver_GuestRecruit : JobDriver_GuestBase
    {
        private static readonly string labelRecruitSuccess = "LetterLabelMessageRecruitSuccess".Translate(); // from core
        private static readonly string labelRecruitFactionAnger = "LetterLabelRecruitFactionAnger".Translate();
        private static readonly string labelRecruitFactionPlease = "LetterLabelRecruitFactionPlease".Translate();
        private static readonly string labelRecruitFactionChiefAnger = "LetterLabelRecruitFactionChiefAnger".Translate();
        private static readonly string labelRecruitFactionChiefPlease = "LetterLabelRecruitFactionChiefPlease".Translate();
        private static readonly string txtRecruitSuccess = "MessageRecruitSuccess".Translate(); // from core
        private static readonly string txtRecruitFail = "MessageRecruitFail".Translate(); // from core
        private static readonly string txtRecruitFailMood = "RecruitFailMood".Translate();
        private static readonly string txtRecruitAngerSelfM = "RecruitAngerSelfM".Translate();
        private static readonly string txtRecruitAngerSelfF = "RecruitAngerSelfF".Translate();
        private static readonly string txtRecruitPleaseSelfM = "RecruitPleaseSelfM".Translate();
        private static readonly string txtRecruitPleaseSelfF = "RecruitPleaseSelfF".Translate();
        private static readonly string txtRecruitAngerOther = "RecruitAngerOther".Translate();
        private static readonly string txtRecruitFactionAnger = "RecruitFactionAnger".Translate();
        private static readonly string txtRecruitFactionPlease = "RecruitFactionPlease".Translate();
        private static readonly string txtRecruitFactionAngerLeaderless = "RecruitFactionAngerLeaderless".Translate();
        private static readonly string txtRecruitFactionPleaseLeaderless = "RecruitFactionPleaseLeaderless".Translate();
        
        private static readonly StatDef statOffendGuestChance = DefDatabase<StatDef>.GetNamed("OffendGuestChance");
        private static readonly StatDef statRecruitEffectivity = DefDatabase<StatDef>.GetNamed("RecruitEffectivity");

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return GotoGuest(pawn, Talkee, guest => guest.TryRecruit());
            yield return TryRecruitGuest(pawn, Talkee);
            yield return RiskAnger(pawn, Talkee);
        }

        public static Toil RiskAnger(Pawn pawn, Pawn guest)
        {
            return new Toil
                   {
                       initAction = () => CheckAnger(pawn, guest),
                       defaultCompleteMode = ToilCompleteMode.Instant
                   };
        }

        private static void CheckAnger(Pawn recruiter, Pawn guest)
        {
            if (guest.Faction == null || recruiter == null || guest.Faction==Faction.OfColony) return;
            var allies = Find.ListerPawns.PawnsInFaction(guest.Faction).ToArray(); // copy, since source can change!
            foreach (var ally in allies)
            {
                if (ally != guest && !ally.Dead && ally.SpawnedInWorld && ally.CanSee(recruiter) && ally.CanSee(guest))
                {
                    float chance = recruiter.GetStatValue(statOffendGuestChance);
                    if (Rand.Value < chance)
                    {
                        //Log.Message("txtRecruitAngerOther");
                        Messages.Message(string.Format(txtRecruitAngerOther, recruiter.NameStringShort, guest.NameStringShort, chance.ToStringPercent(), ally.NameStringShort), MessageSound.Negative);
						ally.Faction.AffectGoodwillWith(Faction.OfColony, -1f + 0.045f * recruiter.skills.GetSkill(SkillDefOf.Social).level); //Skill based influence -0.1 ... -1
                        ally.needs.mood.thoughts.TryGainThought(ThoughtDef.Named("GuestAngered"));
                    }
                }
            }
        }

        public static Toil TryRecruitGuest(Pawn recruiter, Pawn talkee)
        {
            var toil = new Toil
            {
                initAction = () => {
                    if (!GuestUtility.ViableGuestTarget(talkee)) return;
                    if (talkee.guest.interactionMode!=PrisonerInteractionMode.AttemptRecruit) return;
                    if (!recruiter.CanTalkTo(talkee)) return;
                    recruiter.talker.TryTalkTo(new SpeechMessage(), talkee);
                },
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration = 350,
                finishActions = new List<Action> {() => { var success = TryGuestRecruit(recruiter, talkee); }}
            };
            toil.AddFailCondition(() => !GuestUtility.ViableGuestTarget(talkee) || !talkee.TryRecruit());
            return toil;
        }

        public static bool TryGuestRecruit(Pawn recruiter, Pawn guest)
        {
            if (recruiter == null || guest == null || guest.guest == null) return false;

            recruiter.skills.Learn(SkillDefOf.Social, 35f); 
            float recruitDifficulty = guest.guest.RecruitDifficulty;
            if (guest.needs.mood.CurLevel*100 >= recruitDifficulty)
            {
                float recruitChance = recruiter.GetStatValue(StatDefOf.RecruitPrisonerChance);
                recruitChance *= 1f - recruitDifficulty/100f;
                if (recruitChance < 0.011f)
                {
                    recruitChance = 0.011f;
                }
                if (DebugSettings.instantRecruit)
                {
                    recruitChance = 1f;
                }
                if (Rand.Value <= recruitChance)
                {
                    //Log.Message("txtRecruitSuccess");
                    Find.LetterStack.ReceiveLetter(labelRecruitSuccess,
                        string.Format(txtRecruitSuccess, recruiter, guest, recruitChance.ToStringPercent()),
                        LetterType.Good, guest);
                    //if (guest.JailerFaction != null)

                    if (guest.Faction != recruiter.Faction)
                    {
                        if (guest.Faction != null)
                        {
                            guest.Faction.AffectGoodwillWith(Faction.OfColony, -guest.RecruitPenalty());
                            if (guest.RecruitPenalty() >= 1)
                            {
                                //Log.Message("txtRecruitFactionAnger");
                                var message = "";
                                if (guest.Faction.leader != null)
                                {
                                    message = string.Format(txtRecruitFactionAnger, guest.Faction.leader.Name, guest.Faction.name, guest.NameStringShort, (-guest.RecruitPenalty()).ToStringByStyle(ToStringStyle.Integer, ToStringNumberSense.Offset));
                                    Find.LetterStack.ReceiveLetter(labelRecruitFactionChiefAnger, message, LetterType.BadNonUrgent);
                                }
                                else
                                {
                                    message = string.Format(txtRecruitFactionAngerLeaderless, guest.Faction.name, guest.NameStringShort, (-guest.RecruitPenalty()).ToStringByStyle(ToStringStyle.Integer, ToStringNumberSense.Offset));
                                    Find.LetterStack.ReceiveLetter(labelRecruitFactionAnger, message, LetterType.BadNonUrgent);
                                }
                                    
                                
                            }
                            else if (guest.RecruitPenalty() <= -1)
                            {
                                //Log.Message("txtRecruitFactionPlease");
                                var message = "";
                                if (guest.Faction.leader != null)
                                {
                                    message = string.Format(txtRecruitFactionPlease, guest.Faction.leader.Name, guest.Faction.name, guest.NameStringShort, (-guest.RecruitPenalty()).ToStringByStyle(ToStringStyle.Integer, ToStringNumberSense.Offset));
                                    Find.LetterStack.ReceiveLetter(labelRecruitFactionChiefPlease, message, LetterType.Good);
                                }
                                else
                                {
                                    message = string.Format(txtRecruitFactionPleaseLeaderless, guest.Faction.name, guest.NameStringShort, (-guest.RecruitPenalty()).ToStringByStyle(ToStringStyle.Integer, ToStringNumberSense.Offset));
                                    Find.LetterStack.ReceiveLetter(labelRecruitFactionPlease, message, LetterType.Good);
                                }
                            }
                        }
                        //guest.mindState.duty = null;
                        AdoptGuest(guest, recruiter.Faction);
                    }
                    var taleParams = new object[] {recruiter, guest};
                    TaleRecorder.RecordTale(TaleDef.Named("Recruited"), taleParams);
                    return true;
                }
                Messages.Message(string.Format(txtRecruitFail, recruiter, guest, recruitChance.ToStringPercent()), MessageSound.Negative);
            }

            //Messages.Message(string.Format(txtRecruitFailMood, recruiter.Nickname, guest), MessageSound.Negative);
            //var difference = recruitDifficulty - guest.needs.mood.CurLevel;
            //var thought = ThoughtDef.Named(difference >= 10 ? "GuestOffended" : "GuestConvinced");
            // guest.needs.mood.thoughts.TryGainThought(thought);

            float chance = recruiter.GetStatValue(statOffendGuestChance);
            if (Rand.Value < chance)
            {
                var isAbrasive = recruiter.story.traits.HasTrait(TraitDefOf.Abrasive);
                int multiplier = isAbrasive ? 2 : 1;
                string multiplierText = multiplier > 1 ? " x" + multiplier : string.Empty;
                //Log.Message("textAnger");
                string textAnger = recruiter.gender == Gender.Female ? txtRecruitAngerSelfF : txtRecruitAngerSelfM;
                Messages.Message(string.Format(textAnger, recruiter.NameStringShort, guest.NameStringShort, chance.ToStringPercent(), multiplierText), MessageSound.Negative);
				guest.Faction.AffectGoodwillWith(Faction.OfColony, -1f + 0.045f * recruiter.skills.GetSkill(SkillDefOf.Social).level);
                for (int i = 0; i < multiplier; i++)
                {
                    guest.needs.mood.thoughts.TryGainThought(ThoughtDef.Named("GuestOffended"));                    
                }
            }
            else
            {
                var statValue = recruiter.GetStatValue(statRecruitEffectivity);
                var floor = Mathf.FloorToInt(statValue);
                int multiplier = floor + (Rand.Value < statValue - floor?1:0);
                string multiplierText = multiplier > 1 ? " x" + multiplier : string.Empty;
                //Log.Message("textPlease");
                string textPlease = recruiter.gender == Gender.Female ? txtRecruitPleaseSelfF : txtRecruitPleaseSelfM;
                Messages.Message(string.Format(textPlease, recruiter.NameStringShort, guest.NameStringShort, (1 - chance).ToStringPercent(), multiplierText), MessageSound.Benefit);
                for (int i = 0; i < multiplier; i++)
                {
                    guest.needs.mood.thoughts.TryGainThought(ThoughtDef.Named("GuestConvinced"));
                }
            }
            //Log.Message("txtRecruitFail");
            //Messages.Message(string.Format(txtRecruitFail, recruiter, guest, num.ToStringPercent()), MessageSound.Negative);
            return false;
        }

        private static void AdoptGuest(Pawn guest, Faction newFaction)
        {
            // Clear mind
            guest.pather.StopDead();
            guest.jobs.StopAll();

            // Clear reservations
            Find.Reservations.ReleaseAllClaimedBy(guest);

            guest.SetFaction(newFaction);
            guest.mindState.Reset();
            Find.ListerPawns.UpdateRegistryForPawn(guest);
        }
    }
}