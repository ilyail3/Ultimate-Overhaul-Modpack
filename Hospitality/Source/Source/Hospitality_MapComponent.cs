using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Hospitality
{
    public class Hospitality_MapComponent : MapComponent
    {
        public static Hospitality_MapComponent Instance;
        private Dictionary<int, int> bribeCount = new Dictionary<int, int>(); // uses faction.randomKey

        /// <summary>
        /// Call when changing variables. Makes sure the component gets saved.
        /// </summary>
        private void CheckForMapComponent()
        {
            if (!Find.Map.components.Any(c => c is Hospitality_MapComponent))
            {
                Find.Map.components.Add(this);
            }
        }

        public override void ExposeData()
        {
            Scribe_Collections.LookDictionary(ref bribeCount, "bribeCount");
        }

        public Hospitality_MapComponent()
        {
            // Inject MapComponent
            Instance = (Hospitality_MapComponent) Find.Map.components.FirstOrDefault(c => c is Hospitality_MapComponent) ?? this;

            // Inject tab
            InjectTab(typeof (ITab_Pawn_Guest), def => def.race != null && def.race.Humanlike);
        }

        private void InjectTab(Type tabType, Func<ThingDef, bool> qualifier)
        {
            var defs = DefDatabase<ThingDef>.AllDefs.Where(qualifier).ToList();
            defs.RemoveDuplicates();

            foreach (var def in defs)
            {
                if (!def.inspectorTabs.Contains(tabType))
                {
                    def.inspectorTabs.Add(tabType);
                    def.inspectorTabsResolved.Add(ITabManager.GetSharedInstance(tabType));
                    //Log.Message(def.defName+": "+def.inspectorTabsResolved.Select(d=>d.GetType().Name).Aggregate((a,b)=>a+", "+b));
                }
            }
        }

        public int GetBribeCount(Faction faction)
        {
            if (faction == null) throw new NullReferenceException("Faction not set.");
            int result;
            if (bribeCount.TryGetValue(faction.randomKey, out result)) return result;
            
            return 0;
        }

        public void Bribe(Faction faction)
        {
            if (faction == null) throw new NullReferenceException("Faction not set.");

            bribeCount[faction.randomKey] = GetBribeCount(faction) + 1;

            CheckForMapComponent();
        }
    }
}
