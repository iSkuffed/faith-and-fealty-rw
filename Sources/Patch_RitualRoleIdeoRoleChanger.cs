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
}
