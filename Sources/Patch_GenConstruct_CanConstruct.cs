using HarmonyLib;
using RimWorld;
using Verse;

namespace IdeoRework
{
    [HarmonyPatch(typeof(GenConstruct))]
    [HarmonyPatch("CanConstruct", new[] { typeof(Thing), typeof(Pawn), typeof(bool), typeof(bool), typeof(JobDef) })]
    public static class Patch_GenConstruct_CanConstruct
    {
        private static bool religionCanBuild;

        static void Prefix(Thing t, Pawn p)
        {
            religionCanBuild = false;
            if (p == null || t == null) return;

            var religion = p.GetReligionIdeo();
            if (religion == null) return;
            if (p.Ideo != null && !p.Ideo.MembersCanBuild(t) && religion.MembersCanBuild(t) && !(t is Blueprint_Install))
                religionCanBuild = true;
        }

        static void Postfix(ref bool __result)
        {
            if (!__result && religionCanBuild)
                __result = true;
        }
    }
}
