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
            // During load, vanilla validates role holders (Precept_RoleSingle.ExposeData PostLoadInit)
            // before the pawn's religion is restored, so check the religion id read from the save.
            else if (religionIdeo == null && Scribe.mode == LoadSaveMode.PostLoadInit
                     && Patch_PawnIdeoTracker_ExposeData.PendingReligionIdeoId(p) == role.ideo.id)
                __result = true;
        }
    }
}
