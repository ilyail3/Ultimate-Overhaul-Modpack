using RimWorld;
using RimWorld.SquadAI;
using Verse;

namespace Hospitality
{
    public class TransitionAction_MessageLeaving : TransitionAction_Message
    {
        private static readonly string txtLeavingGreat = "VisitorsLeavingGreat".Translate();
        private static readonly string txtLeavingGood = "VisitorsLeavingGood".Translate();
        private static readonly string txtLeavingNormal = "VisitorsLeavingNormal".Translate();
        private static readonly string txtLeavingBad = "VisitorsLeavingBad".Translate();
        private static readonly string txtLeavingAwful = "VisitorsLeavingAwful".Translate();

        public Faction faction;
        private float goodwill;

        public TransitionAction_MessageLeaving() {}

        public TransitionAction_MessageLeaving(Faction faction)
        {
            this.faction = faction;
            goodwill = faction.ColonyGoodwill;
        }

        public override void DoAction(Transition trans)
        {
            if (faction == null) return;

            var offset = faction.ColonyGoodwill - goodwill;
            //Log.Message("Offset: "+offset);
            //Log.Message("Min: " + (IncidentWorker_VisitorGroup.MaxAngerAmount(goodwill) -goodwill));
            //Log.Message("Max: " + (IncidentWorker_VisitorGroup.MaxPleaseAmount(goodwill)-goodwill));

            if (offset >= IncidentWorker_VisitorGroup.MaxPleaseAmount(goodwill) - goodwill)
                Messages.Message(txtLeavingGreat.Translate(new object[] { faction.name }), MessageSound.Benefit);
            else if (offset > (IncidentWorker_VisitorGroup.MaxPleaseAmount(goodwill) - goodwill)/2)
                Messages.Message(txtLeavingGood.Translate(new object[] { faction.name }), MessageSound.Benefit);
            else if (offset <= IncidentWorker_VisitorGroup.MaxAngerAmount(goodwill) - goodwill)
                Messages.Message(txtLeavingAwful.Translate(new object[] { faction.name }), MessageSound.Negative);
            else if (offset < (IncidentWorker_VisitorGroup.MaxAngerAmount(goodwill) - goodwill)/2)
                Messages.Message(txtLeavingBad.Translate(new object[] { faction.name }), MessageSound.Negative);
            else
                Messages.Message(txtLeavingNormal.Translate(new object[] { faction.name }), MessageSound.Standard);
        }

        public override void ExposeData()
        {
            Scribe_References.LookReference(ref faction, "faction");
            Scribe_Values.LookValue(ref goodwill, "goodwill");
        }
    }
}