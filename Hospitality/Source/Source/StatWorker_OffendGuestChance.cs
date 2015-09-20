using System.Text;
using RimWorld;
using Verse;

namespace Hospitality
{
    public class StatWorker_OffendGuestChance : StatWorker
    {
        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            var pawn = req.Thing as Pawn;
            if (pawn == null || pawn.story==null) return 0;
            var isAbrasive = pawn.story.traits.HasTrait(TraitDefOf.Abrasive);
            var abrasiveFactor = isAbrasive ? 0.5f : 1f;
            return 1 - abrasiveFactor*base.GetValueUnfinalized(req, applyPostProcess);
        }
        public override string GetExplanation(StatRequest req, ToStringNumberSense numberSense)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(base.GetExplanation(req, numberSense));

            var pawn = req.Thing as Pawn;
            if (pawn == null || pawn.story == null) return stringBuilder.ToString();
            var isAbrasive = pawn.story.traits.HasTrait(TraitDefOf.Abrasive);
            var abrasiveFactor = isAbrasive ? 0.5f : 1f;

            if (isAbrasive)
            {
                stringBuilder.AppendLine();
                stringBuilder.AppendLine(string.Format("    {0}: x{1}", TraitDefOf.Abrasive.label.CapitalizeFirst(), abrasiveFactor.ToStringPercent()));
            }
            return stringBuilder.ToString();
        }
    }
}