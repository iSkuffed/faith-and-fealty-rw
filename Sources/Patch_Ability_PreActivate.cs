using HarmonyLib;
using RimWorld;
using Verse;

namespace IdeoRework
{
    [HarmonyPatch(typeof(Ability))]
    [HarmonyPatch("PreActivate")]
    public static class Patch_Ability_PreActivate
    {
        static void Postfix(Ability __instance, LocalTargetInfo? target)
        {
            try
            {
                if (__instance.pawn == null || __instance.def.groupDef == null) return;

                // Propagate group cooldown to our managed abilities
                var groupDef = __instance.def.groupDef;
                int ticks = __instance.def.overrideGroupCooldown
                    ? __instance.def.cooldownTicksRange.RandomInRange
                    : groupDef.cooldownTicks;

                var abilities = IdeoAbilityManager.GetAllAbilities(__instance.pawn);
                foreach (var ability in abilities)
                {
                    ability.Notify_GroupStartedCooldown(groupDef, ticks);
                }
            }
            catch (System.Exception ex)
            {
                Log.Warning("[IdeoRework] PreActivate cooldown propagation: " + ex.Message);
            }
        }
    }
}
