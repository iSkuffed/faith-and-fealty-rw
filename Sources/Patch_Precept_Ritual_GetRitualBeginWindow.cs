using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace IdeoRework
{
    [HarmonyPatch(typeof(Precept_Ritual))]
    [HarmonyPatch("GetRitualBeginWindow")]
    public static class Patch_Precept_Ritual_GetRitualBeginWindow
    {
        static void Postfix(ref Window __result, Precept_Ritual __instance,
            TargetInfo targetInfo, RitualObligation obligation, Action onConfirm,
            Pawn organizer, Dictionary<string, Pawn> forcedForRole, Pawn selectedPawn)
        {
            if (__instance?.ideo == null || !PresetReligions.CreatedReligionIdeos.Contains(__instance.ideo))
                return;

            __result = new Dialog_BeginReligionRitual(
                __instance, targetInfo, obligation, onConfirm,
                organizer, forcedForRole, selectedPawn);
        }
    }
}
