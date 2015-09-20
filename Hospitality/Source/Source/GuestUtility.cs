using System;
using System.Reflection;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Hospitality
{
    internal static class GuestUtility
    {
        private static readonly StatDef statRecruitRelationshipDamage = DefDatabase<StatDef>.GetNamed("RecruitRelationshipDamage");

        public static bool IsGuest(Pawn pawn)
        {
            if (pawn == null) return false;
            if (pawn.Dead) return false;
            if (!pawn.SpawnedInWorld) return false;
            if (!pawn.RaceProps.Humanlike) return false;
            if (pawn.IsPrisonerOfColony || pawn.Faction == Faction.OfColony) return false;
            if (pawn.guest == null) return false;
            if (pawn.HostileTo(Faction.OfColony)) return false;
            return true;
        }

        public static float RecruitPenalty(this Pawn guest)
        {
            return guest.GetStatValue(statRecruitRelationshipDamage);
        }

        public static bool ImproveRelationship(this Pawn guest)
        {
            if (guest.guest == null) return false;
            return guest.guest.interactionMode == PrisonerInteractionMode.Chat;
        }

        public static bool TryRecruit(this Pawn guest)
        {
            if (guest.guest == null) return false;
            return guest.guest.interactionMode == PrisonerInteractionMode.AttemptRecruit;
        }

        public static bool CanTalkTo(this Pawn talker, Pawn talkee)
        {
            return TalkUtility.CanTalk(talker)
                   && (talker.Position - talkee.Position).LengthHorizontalSquared <= 36.0
                   && GenSight.LineOfSight(talker.Position, talkee.Position, true);
        }


        public static bool ViableGuestTarget(Pawn guest, bool sleepingIsOk = false)
        {
			return !(guest==null || guest.Dead || guest.Destroyed || (!sleepingIsOk&&!guest.Awake()) || !IsGuest(guest) || !Find.AreaHome[guest.Position] || HasDismissiveThought(guest));
        }

        public static T GetStaticMethod<T>(this Type type, string name)
        {
            return (T)type.GetMethod(name,BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null); 
        }

		public static float GetDismissiveChance(Pawn guest)
		{
			float dismissiveChance = 0.0f;
			List<ThoughtDef> thoughts = guest.needs.mood.thoughts.DistinctThoughtDefs;
			for (int i = 0; i < thoughts.Count; i++)
			{
				if (thoughts[i].defName == "GuestPleasedRelationship") dismissiveChance += guest.needs.mood.thoughts.MoodOffsetOfThoughtGroup(thoughts[i]);
				if (thoughts[i].defName == "GuestOffendedRelationship") dismissiveChance -= guest.needs.mood.thoughts.MoodOffsetOfThoughtGroup(thoughts[i]);
			}
			//Log.Message("dismissiveChance : " + (dismissiveChance / 50));
			return dismissiveChance / 20; //TODO: find balance - /30 results in 50% after 5 talks (3 moodeffect each)
		}

		public static bool HasDismissiveThought(Pawn guest)
		{
			//Log.Message("HasDismissiveThought : " + guest.needs.mood.thoughts.DistinctThoughtDefs.Contains(ThoughtDef.Named("GuestDismissiveAttitude")));
			return guest.needs.mood.thoughts.DistinctThoughtDefs.Contains(ThoughtDef.Named("GuestDismissiveAttitude"));
		}
    }
}