using HarmonyLib;
using RimWorld;
using Verse;

namespace IdeoRework
{
    [HarmonyPatch(typeof(RoleRequirement_SameIdeo))]
    [HarmonyPatch("Met")]
    public static class Patch_RoleRequirement_SameIdeo
    {
        static void Postfix(ref bool __result, Pawn p, Precept_Role role)
        {
            if (__result) return;
            if (p == null || role?.ideo == null) return;

            var religionIdeo = p.GetReligionIdeo();
            if (religionIdeo != null && religionIdeo == role.ideo)
                __result = true;
        }
    }
}
