using RimWorld;
using RimWorld.SquadAI;
using UnityEngine;
using Verse;

namespace Hospitality
{
    public class ITab_Pawn_Guest : ITab_Pawn_Visitor
    {
        private static readonly string txtRecruitmentDifficulty = "RecruitmentDifficulty".Translate(); // from core
        private static readonly string txtRecruitmentPenalty = "RecruitmentPenalty".Translate();

        public ITab_Pawn_Guest()
        {
            labelKey = "TabGuest";
            size = new Vector2(400f, 300f);
        }

        public override bool IsVisible
        {
            get { return GuestUtility.IsGuest(SelPawn); }
        }


        protected override void FillTab()
        {
            //ConceptDatabase.KnowledgeDemonstrated(ConceptDefOf.PrisonerTab, KnowledgeAmount.GuiFrame);
            Text.Font = GameFont.Small;
            Rect rect = new Rect(0f, 20f, size.x, size.y - 20).ContractedBy(10f);
            var listingStandard = new Listing_Standard(rect);
            {
                var tryImprove = SelPawn.ImproveRelationship();
                var tryRecruit = SelPawn.TryRecruit();

                listingStandard.OverrideColumnWidth = size.x;

                if (SelPawn.guest != null)
                {
                    listingStandard.DoGap();
                    var b0 = listingStandard.DoRadioButton("LeaveAlone".Translate(), !tryRecruit && !tryImprove);
                    var b1 = listingStandard.DoRadioButton("ShouldTryToRecruit".Translate(), tryRecruit);
                    var b2 = listingStandard.DoRadioButton("ImproveRelationship".Translate(), tryImprove);
                    
                    if (b0) SelPawn.guest.interactionMode = PrisonerInteractionMode.NoInteraction;
                    else if (b1) SelPawn.guest.interactionMode = PrisonerInteractionMode.AttemptRecruit;
                    else if (b2) SelPawn.guest.interactionMode = PrisonerInteractionMode.Chat;
                }

                if (SelPawn.Faction != null)
                {
                    listingStandard.DoLabel(txtRecruitmentDifficulty + ": " + MakeDifficultyRelative(SelPawn).ToString("##0"));
                    listingStandard.DoLabel(txtRecruitmentPenalty + ": " + SelPawn.RecruitPenalty().ToString("##0"));
                    listingStandard.DoLabel("Faction goodwill: " + SelPawn.Faction.ColonyGoodwill.ToString("##0"));
                }
                listingStandard.DoGap();

                var state = SelPawn.GetSquadBrain().curState as State_VisitPoint;
                if (state != null && SelPawn.Faction!=null)
                {
                    listingStandard.DoLabel("Hospitality:");
                    listingStandard.DoSlider(SelPawn.Faction.ColonyGoodwill,
                        IncidentWorker_VisitorGroup.MaxAngerAmount(state.startingGoodwill),
                        IncidentWorker_VisitorGroup.MaxPleaseAmount(state.startingGoodwill));
                }
                // Don't show hospitality when arriving or leaving - we don't have correct data (no state)
                //else
                //{
                //    listingStandard.DoLabel("Hospitality:");
                //    listingStandard.DoSlider(SelPawn.Faction.ColonyGoodwill,
                //        IncidentWorker_VisitorGroup.MaxAngerAmount(SelPawn.Faction.ColonyGoodwill),
                //        IncidentWorker_VisitorGroup.MaxPleaseAmount(SelPawn.Faction.ColonyGoodwill));
                //}
            }
            listingStandard.End();
        }

        private static float MakeDifficultyRelative(Pawn guest)
        {
            const int maxStackingThoughts = 40;
            const float thoughtEffectivity = 2f;
            const float factor = 100/(maxStackingThoughts*thoughtEffectivity);
            return Mathf.Clamp((guest.guest.RecruitDifficulty - guest.needs.mood.CurLevel * 100)*factor, 0, 100);
        }
    }
}