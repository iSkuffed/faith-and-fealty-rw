using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace IdeoRework
{
    [HarmonyPatch(typeof(SituationalThoughtHandler))]
    [HarmonyPatch("UpdateAllMoodThoughts")]
    public static class Patch_SituationalThoughtHandler_UpdateAllMoodThoughts
    {
        static void Postfix(SituationalThoughtHandler __instance)
        {
            var pawn = __instance.pawn;
            if (pawn == null) return;

            var religion = pawn.GetReligionIdeo();
            if (religion == null) return;

            var cachedField = AccessTools.Field(typeof(SituationalThoughtHandler), "cachedThoughts");
            var cached = cachedField?.GetValue(__instance) as List<Thought_Situational>;
            if (cached == null) return;

            foreach (Precept precept in religion.PreceptsListForReading)
            {
                foreach (Thought_Situational thought in precept.SituationThoughtsToAdd(pawn, cached))
                {
                    cached.Add(thought);
                }
            }
        }
    }
}
