using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace IdeoRework
{
    [HarmonyPatch(typeof(Precept_RoleSingle))]
    [HarmonyPatch("RecacheActivity")]
    public static class Patch_Prep_RoleSingle_RecacheActivity
    {
        // Cached reflection accessors (AccessTools.Field/Method resolve via binding-flag lookups
        // that are too costly to repeat every tick per role) for protected members of Precept_Role.
        private static readonly AccessTools.FieldRef<Precept_Role, bool> ActiveRef =
            AccessTools.FieldRefAccess<Precept_Role, bool>("active");

        private static readonly Func<Precept_Role, Pawn, bool> ValidatePawnDelegate =
            AccessTools.MethodDelegate<Func<Precept_Role, Pawn, bool>>(AccessTools.Method(typeof(Precept_Role), "ValidatePawn"));

        // Single prefix that replaces the method for religion roles
        static bool Prefix(Precept_RoleSingle __instance)
        {
            if (!PresetReligions.CreatedReligionIdeos.Contains(__instance.ideo))
                return true; // Let vanilla handle non-religion roles

            // Vanilla's believer recache calls this before the religion count is known; the
            // believer-count postfix re-runs it with the real count.
            if (Patch_Ideo_RecacheColonistBelieverCount.RecachingReligion == __instance.ideo)
                return false;

            // For religion roles, run our own RecacheActivity without notifications
            int colonistBelieverCountCached = __instance.ideo.ColonistBelieverCountCached;

            // Deactivation check (same as vanilla, but no notifications)
            if (__instance.Active && __instance.def.deactivationBelieverCount >= 0
                && colonistBelieverCountCached <= __instance.def.deactivationBelieverCount
                && !__instance.def.leaderRole)
            {
                ActiveRef(__instance) = false;

                // Unassign pawn (vanilla behavior)
                if (__instance.ChosenPawnValue != null)
                {
                    __instance.Notify_PawnUnassigned(__instance.ChosenPawnValue);
                    IdeoRoleManager.UnassignRole(__instance.ChosenPawnValue, isReligion: true);
                    __instance.chosenPawn.pawn = null;
                }
            }

            // Activation check (same as vanilla, but no notifications)
            if (!__instance.Active && __instance.def.activationBelieverCount >= 0
                && (colonistBelieverCountCached >= __instance.def.activationBelieverCount
                    || __instance.def.leaderRole))
            {
                ActiveRef(__instance) = true;
            }

            // Final validation (same as vanilla): drop the pawn if they no longer qualify
            // (lost faction/colonist status, died/destroyed, or no longer meet role requirements).
            if (__instance.ChosenPawnValue != null && !ValidatePawnDelegate(__instance, __instance.ChosenPawnValue))
            {
                __instance.Notify_PawnUnassigned(__instance.ChosenPawnValue);
                IdeoRoleManager.UnassignRole(__instance.ChosenPawnValue, isReligion: true);
                __instance.chosenPawn.pawn = null;
            }

            return false; // Skip vanilla method
        }
    }
}
