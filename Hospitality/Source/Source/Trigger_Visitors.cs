using RimWorld.SquadAI;
using Verse;

namespace Hospitality
{
    public class Trigger_VisitorsPleasedMax : Trigger
    {
        private float threshold = 100;
        private const int CheckInterval = 800;

        public Trigger_VisitorsPleasedMax() {}

        public Trigger_VisitorsPleasedMax(float threshold)
        {
            this.threshold = threshold;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.LookValue(ref threshold, "threshold", 100);
        }

        public override bool ActivateOn(Brain brain, TriggerSignal signal)
        {
            if (signal.type == TriggerSignalType.Tick && Find.TickManager.TicksGame%CheckInterval == 0)
            {
                if (brain.faction.ColonyGoodwill >= threshold) return true;
            }
            return false;
        }
    }

    public class Trigger_VisitorsAngeredMax : Trigger
    {
        private float threshold = -100;
        private const int CheckInterval = 800;

        public Trigger_VisitorsAngeredMax() {}

        public Trigger_VisitorsAngeredMax(float threshold)
        {
            this.threshold = threshold;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.LookValue(ref threshold, "threshold", 100);
        }

        public override bool ActivateOn(Brain brain, TriggerSignal signal)
        {
            if (signal.type == TriggerSignalType.Tick && Find.TickManager.TicksGame%CheckInterval == 0)
            {
                if (brain.faction.ColonyGoodwill <= threshold) return true;
            }
            return false;
        }
    }
}
