using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.SquadAI;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Hospitality
{
    public class IncidentWorker_VisitorGroup : IncidentWorker_NeutralGroup
    {
        public static float MaxPleaseAmount(float current)
        {
            // if current standing is 100, 10 can be gained
            // if current standing is -100, 50 can be gained
            // then clamped.
            return Mathf.Clamp(current + 30 - Offset(current), -100, 100);
        }

        public static float MaxAngerAmount(float current)
        {
            return Mathf.Clamp(current-30, -100, 100);            
        }

        private static float Offset(float current)
        {
            return Mathf.Lerp(-20, 20, Mathf.InverseLerp(-100, 100, current));
        }

        protected override bool StorytellerCanUseNowSub()
        {
            return GetRandomSpotThing() != null;
        }

        public override bool TryExecute(IncidentParms parms)
        {
            if (!TryResolveParms(parms))
            {
                return false;
            }
            List<Pawn> list = SpawnPawns(parms);
            if (list.Count == 0)
            {
                return false;
            }
            string label;
            string text;
            if (list.Count == 1)
            {
                label = "LetterLabelSingleVisitorArrives".Translate();
                text =
                    "SingleVisitorArrives".Translate(new object[]
                    {list[0].story.adulthood.title.ToLower(), parms.faction.name, list[0].Name});
                text = text.AdjustedFor(list[0]);
            }
            else
            {
                label = "LetterLabelGroupVisitorsArrive".Translate();
                text = "GroupVisitorsArrive".Translate(new object[] {parms.faction.name});
            }
            Find.LetterStack.ReceiveLetter(label, text, LetterType.Good, list[0]);
            IntVec3 chillSpot;
            if (TryFindRandomVisitSpot(list[0], out chillSpot))
            {
                StateGraph stateGraph = VisitColonyGraph(parms.faction, chillSpot);
                BrainMaker.MakeNewBrain(parms.faction, stateGraph, list);

                list.ForEach(p => p.guest.interactionMode = PrisonerInteractionMode.Chat);
                return true;
            }
            return false;
        }

        private static bool TryFindRandomVisitSpot(Pawn searcher, out IntVec3 result)
        {
            bool[] desperate = {false}; // hack to make it referable
            Predicate<IntVec3> validator = delegate(IntVec3 c) {
                if (!c.Standable())
                {
                    return false;
                }
                if (!desperate[0] && !Find.RoofGrid.Roofed(c))
                {
                    return false;
                }
                if (!desperate[0] && !c.CanReachColony())
                {
                    return false;
                }
                if (!searcher.CanReach(c, PathEndMode.OnCell, desperate[0] ? Danger.Deadly : Danger.None))
                {
                    return false;
                }
                Room room = c.GetRoom();
                return room != null && room.CellCount >= 25;
            };

            for (int i = 0; i < 400; i++)
            {
                var thing = GetRandomSpotThing();
                if (thing == null)
                {
                    break;
                }
                if (CellFinder.TryFindRandomCellNear(thing.Position, 16, validator, out result))
                {
                    return true;
                }
            }
            desperate[0] = true;
            return CellFinderLoose.TryGetRandomCellWith(validator,1000, out result);
        }

        private static Thing GetRandomSpotThing()
        {
            CompGatherSpot spot;
            GatherSpotLister.activeSpots.TryRandomElement(out spot);
            Thing thing = spot != null ? spot.parent : null;
            if (thing == null)
            {
                Building building;
                Find.ListerBuildings.allBuildingsColonistCombatTargets.TryRandomElement(out building);
                thing = building;
            }
            if (thing == null && Find.ListerBuildings.allBuildingsColonist.Count > 0)
            {
                thing = Find.ListerBuildings.allBuildingsColonist.RandomElement();
            }
            if (thing == null && Find.ListerPawns.FreeColonistsCount > 0)
            {
                thing = Find.ListerPawns.FreeColonists.RandomElement();
            }
            if (thing == null && Find.ListerPawns.ColonistsAndPrisoners.Any())
            {
                thing = Find.ListerPawns.ColonistsAndPrisoners.RandomElement();
            }
            return thing;
        }

        public static StateGraph VisitColonyGraph(Faction faction, IntVec3 chillSpot)
        {
            var graph = new StateGraph();
            State stateArrival = graph.AttachSubgraph(TravelStatePairGraph(chillSpot));
            var stateVisit = new State_VisitPoint(chillSpot, faction.ColonyGoodwill);
            graph.states.Add(stateVisit);
            var stateLeave = graph.AttachSubgraph(TravelStatePairGraph(IntVec3.Invalid));
            var t1 = new Transition(stateArrival, stateVisit);
            t1.triggers.Add(new Trigger_Memo("TravelArrived"));
            graph.transitions.Add(t1);
            var stateWounded = new State_TakeWoundedGuest();
            graph.states.Add(stateWounded);
            var t2 = new Transition(stateVisit, stateWounded);
            t2.preActions.Add(
                new TransitionAction_Message(
                    "MessageVisitorsTakingWounded".Translate(new object[]
                    {faction.def.pawnsPlural.CapitalizeFirst(), faction.name})));
            t2.triggers.Add(new Trigger_WoundedGuestPresent());
            graph.transitions.Add(t2);
            var t3 = new Transition(stateVisit, stateLeave);
            t3.preActions.Add(new TransitionAction_MessageLeaving(faction));
            t3.preActions.Add(new TransitionAction_WakeAll());
            t3.triggers.Add(new Trigger_TicksPassed(Rand.Range(15000, 30000)));
            t3.triggers.Add(new Trigger_Memo("VisitorsAnnoyed")); // not used yet
            t3.triggers.Add(new Trigger_VisitorsPleasedMax(MaxPleaseAmount(faction.ColonyGoodwill)));
            t3.triggers.Add(new Trigger_VisitorsAngeredMax(MaxAngerAmount(faction.ColonyGoodwill)));
            graph.transitions.Add(t3);
            return graph;
        }

        private static StateGraph TravelStatePairGraph(IntVec3 travelDest, int defendTicks = 5000)
        {
            var stateGraph = new StateGraph();
            var travel = new State_Travel(travelDest);
            stateGraph.StartingState = travel;
            var adhocDefend = new State_DefendPoint();
            stateGraph.states.Add(adhocDefend);
            var transition = new Transition(travel, adhocDefend);
            transition.triggers.Add(new Trigger_PawnHarmed());
            transition.preActions.Add(new TransitionAction_SetDefendLocalGroup());
            stateGraph.transitions.Add(transition);
            var transition2 = new Transition(adhocDefend, travel);
            transition2.triggers.Add(new Trigger_TicksPassed(defendTicks));
            stateGraph.transitions.Add(transition2);
            var transition3 = new Transition(adhocDefend, adhocDefend);
            transition3.triggers.Add(new Trigger_PawnHarmed());
            stateGraph.transitions.Add(transition3);
            return stateGraph;
        }
    }
}
