using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace IdeoRework
{
    [HarmonyPatch(typeof(RitualUtility))]
    [HarmonyPatch("AllRolesForPawn")]
    public static class Patch_RitualUtility_AllRolesForPawn
    {
        static void Postfix(ref IEnumerable<Precept_Role> __result, Pawn p)
        {
            if (p == null) return;

            var religionIdeo = p.GetReligionIdeo();
            if (religionIdeo == null) return;

            var existing = new HashSet<Precept_Role>(__result);
            var religionRoles = religionIdeo.RolesListForReading
                .Where(r => !existing.Contains(r));
            __result = __result.Concat(religionRoles);
        }
    }
}
