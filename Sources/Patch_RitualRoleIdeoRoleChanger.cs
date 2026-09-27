using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace IdeoRework
{
    [HarmonyPatch(typeof(RitualRoleIdeoRoleChanger))]
    [HarmonyPatch("AppliesToPawn")]
    public static class Patch_RitualRoleIdeoRoleChanger
    {
        static void Postfix(ref bool __result, Pawn p)
        {
            if (__result) return;
            if (p == null) return;

            var religionIdeo = p.GetReligionIdeo();
            if (religionIdeo == null) return;

            if (religionIdeo.GetRole(p) != null ||
                religionIdeo.RolesListForReading.Any(r => r.RequirementsMet(p)))
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(RitualRoleAssignments))]
    [HarmonyPatch("UpdateRoleChangeTargetRole")]
    public static class Patch_RitualRoleAssignments_UpdateRoleChangeTargetRole
    {
        static bool Prefix(RitualRoleAssignments __instance, Pawn pawn)
        {
            if (pawn == null) return true;

            var religionRole = IdeoRoleManager.GetRole(pawn, isReligion: true);
            if (religionRole != null)
            {
                __instance.SetRoleChangeSelection(null);
                return false;
            }

            return true;
        }
    }
}
