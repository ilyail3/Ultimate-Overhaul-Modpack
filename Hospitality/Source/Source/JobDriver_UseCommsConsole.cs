using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Hospitality
{
    /// <summary>
    /// Overridden for escalating silver costs...
    /// </summary>
    public class JobDriver_UseCommsConsole : JobDriver
    {
        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            yield return new Toil
                         {
                             initAction = () => UseConsole(TargetA),
                             defaultCompleteMode = ToilCompleteMode.Instant
                         };

        }

        private void UseConsole(TargetInfo targetA)
        {
            if (!targetA.HasThing || targetA.ThingDestroyed || !(targetA.Thing is Building_CommsConsole)) return;

            if (CurJob.commTarget is Faction)
            {
                TryOpenComms(CurJob.commTarget as Faction, pawn);
            }
            else
            {
                CurJob.commTarget.TryOpenComms(pawn);
            }
        }

        // RimWorld.Faction
        public static void TryOpenComms(Faction commTarget, Pawn negotiator)
        {
            Find.WindowStack.Add(new Dialog_Negotiation(negotiator, commTarget, FactionDialogFor(negotiator, commTarget)));
        }

        // RimWorld.FactionDialogMaker
        public static DiaNode FactionDialogFor(Pawn negotiator, Faction faction)
        {
            string text = (faction.leader != null) ? faction.leader.Name.ToStringFull : faction.name;
            DiaNode root;
            if (faction.ColonyGoodwill < -70f)
            {
                root = new DiaNode("FactionGreetingHostile".Translate(new object[]
                                                                      {
                                                                          text
                                                                      }));
            }
            else if (faction.ColonyGoodwill < 40f)
            {
                string text2 = "FactionGreetingWary".Translate(new object[]
                                                               {
                                                                   text,
                                                                   negotiator.LabelBaseShort
                                                               });
                root = new DiaNode(text2.AdjustedFor(negotiator));
                root.options.Add(OfferGiftOption(negotiator, faction));
            }
            else
            {
                root = new DiaNode("FactionGreetingWarm".Translate(new object[]
                                                                   {
                                                                       text,
                                                                       negotiator.LabelBaseShort
                                                                   }));
                root.options.Add(OfferGiftOption(negotiator, faction));
                root.options.Add(DialogOption("RequestMilitaryAidOption"));
            }
            if (Prefs.DevMode)
            {
                foreach (DiaOption current in DialogOptions("DebugOptions"))
                {
                    root.options.Add(current);
                }
            }
            var diaOption = new DiaOption("(" + "Disconnect".Translate() + ")") {resolveTree = true};
            root.options.Add(diaOption);
            SetField("root", root);
            SetField("faction", faction);
            SetField("negotiator", negotiator);
            return root;
        }

        private static void SetField<T>(string name, T value)
        {
            typeof (FactionDialogMaker).GetField(name, BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, value);
        }

        // RimWorld.FactionDialogMaker
        private static DiaOption OfferGiftOption(Pawn negotiator, Faction faction)
        {
            int cost = GetCost(Hospitality_MapComponent.Instance.GetBribeCount(faction));
            Log.Message(faction.name + ": " + Hospitality_MapComponent.Instance.GetBribeCount(faction) + " = " + cost);
            int num = TradeUtility.AllLaunchableThings.Where(t => t.def == ThingDefOf.Silver).Sum(t => t.stackCount);
            if (num < cost)
            {
                var diaOption = new DiaOption("OfferGift".Translate() + " (" + "NeedSilverLaunchable".Translate(new object[]
                                                                                                                {
                                                                                                                    cost
                                                                                                                }) + ")");
                diaOption.Disable("NotEnoughSilver".Translate());
                return diaOption;
            }
            float goodwillDelta = 12f*negotiator.GetStatValue(StatDefOf.GiftImpact);
            var diaOption2 = new DiaOption("OfferGift".Translate() + " (" + "SilverForGoodwill".Translate(new object[]
                                                                                                          {
                                                                                                              cost,
                                                                                                              goodwillDelta.ToString("#####0")
                                                                                                          }) + ")");
            diaOption2.action = delegate
                                {
                                    TradeUtility.LaunchThingsOfType(ThingDefOf.Silver, cost, null);
                                    faction.AffectGoodwillWith(Faction.OfColony, goodwillDelta);
                                    Hospitality_MapComponent.Instance.Bribe(faction);
                                };
            string text = "SilverGiftSent".Translate(new object[]
                                                     {
                                                         faction.name,
                                                         Mathf.RoundToInt(goodwillDelta)
                                                     });
            diaOption2.link = new DiaNode(text)
                              {
                                  options =
                                  {
                                      new DiaOption("OK".Translate())
                                      {
                                          linkLateBind = () => FactionDialogFor(negotiator, faction)
                                      }
                                  }
                              };
            return diaOption2;
        }

        private static int GetCost(int bribeCount)
        {
            int amount = 300;
            int increase = 100;
            const int increase2 = 100;

            for (int i = 0; i < bribeCount; i++)
            {
                amount += increase;
                increase += increase2;
            }
            return amount;
        }

        private static DiaOption DialogOption(string name)
        {
            return typeof (FactionDialogMaker).GetStaticMethod<DiaOption>(name);
        }

        private static IEnumerable<DiaOption> DialogOptions(string name)
        {
            return typeof (FactionDialogMaker).GetStaticMethod<IEnumerable<DiaOption>>(name);
        }
    }
}