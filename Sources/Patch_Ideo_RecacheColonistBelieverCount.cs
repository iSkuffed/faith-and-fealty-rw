using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace IdeoRework
{
    // Vanilla counts believers by pawn.Ideo, which is never a religion, and recaches role activity
    // with that count (0) before any postfix runs — deactivating religion roles and dropping holders.
    // So for religions: role recache is suppressed during vanilla's loop, then redone here with the
    // real count (free colonists whose religion is this ideo).
    [HarmonyPatch(typeof(Ideo))]
    [HarmonyPatch("RecacheColonistBelieverCount")]
    public static class Patch_Ideo_RecacheColonistBelieverCount
    {
        // Religion whose vanilla recache is in progress; Patch_Prep_RoleSingle_RecacheActivity skips it.
        public static Ideo RecachingReligion;

        private static readonly AccessTools.FieldRef<Ideo, int> CountRef =
            AccessTools.FieldRefAccess<Ideo, int>("colonistBelieverCountCached");

        static void Prefix(Ideo __instance)
        {
            if (PresetReligions.CreatedReligionIdeos.Contains(__instance))
                RecachingReligion = __instance;
        }

        static void Postfix(Ideo __instance, ref int __result)
        {
            if (RecachingReligion != __instance) return;
            RecachingReligion = null;

            // Same early-out as vanilla: nothing was recached.
            if (Current.ProgramState != ProgramState.Playing || Find.WindowStack.IsOpen<Dialog_ConfigureIdeo>())
                return;

            int count = 0;
            foreach (var pawn in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists_NoCryptosleep)
            {
                if (pawn.GetReligionIdeo() == __instance && !pawn.IsSlave && !pawn.IsQuestLodger())
                    count++;
            }
            CountRef(__instance) = count;
            __result = count;

            foreach (var role in __instance.RolesListForReading)
                role.RecacheActivity();
        }

        static Exception Finalizer(Exception __exception)
        {
            RecachingReligion = null;
            return __exception;
        }
    }
}
