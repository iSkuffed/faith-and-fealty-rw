using HarmonyLib;
using RimWorld;
using Verse;

namespace IdeoRework
{
    [HarmonyPatch(typeof(Precept_RoleSingle))]
    [HarmonyPatch("RecacheActivity")]
    public static class Patch_Prep_RoleSingle_RecacheActivity
    {
        // Single prefix that replaces the method for religion roles
        static bool Prefix(Precept_RoleSingle __instance)
        {
            if (!PresetReligions.CreatedReligionIdeos.Contains(__instance.ideo))
                return true; // Let vanilla handle non-religion roles

            // For religion roles, run our own RecacheActivity without notifications
            int colonistBelieverCountCached = __instance.ideo.ColonistBelieverCountCached;

            // Deactivation check (same as vanilla, but no notifications)
            if (__instance.Active && __instance.def.deactivationBelieverCount >= 0 
                && colonistBelieverCountCached <= __instance.def.deactivationBelieverCount 
                && !__instance.def.leaderRole)
            {
                var activeField = AccessTools.Field(typeof(Precept_Role), "active");
                activeField.SetValue(__instance, false);
                
                // Unassign pawn (vanilla behavior)
                if (__instance.ChosenPawnValue != null)
                {
                    __instance.Notify_PawnUnassigned(__instance.ChosenPawnValue);
                    var chosenPawnField = AccessTools.Field(typeof(Precept_RoleSingle), "chosenPawn");
                    var chosenPawn = chosenPawnField.GetValue(__instance) as IdeoRoleInstance;
                    if (chosenPawn != null)
                        chosenPawn.pawn = null;
                }
            }

            // Activation check (same as vanilla, but no notifications)
            if (!__instance.Active && __instance.def.activationBelieverCount >= 0 
                && (colonistBelieverCountCached >= __instance.def.activationBelieverCount 
                    || __instance.def.leaderRole))
            {
                var activeField = AccessTools.Field(typeof(Precept_Role), "active");
                activeField.SetValue(__instance, true);
            }

            return false; // Skip vanilla method
        }
    }
}
