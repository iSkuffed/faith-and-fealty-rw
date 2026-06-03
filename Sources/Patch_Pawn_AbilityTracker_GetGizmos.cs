using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace IdeoRework
{
    [HarmonyPatch(typeof(Pawn_AbilityTracker))]
    [HarmonyPatch("GetGizmos")]
    public static class Patch_Pawn_AbilityTracker_GetGizmos
    {
        static void Postfix(ref IEnumerable<Gizmo> __result, Pawn_AbilityTracker __instance)
        {
            var pawn = __instance.pawn;
            if (pawn == null) return;

            var abilities = IdeoAbilityManager.GetAllAbilities(pawn);
            if (abilities.Count == 0) return;

            var original = __result;
            __result = AppendReligionGizmos(original, abilities);
        }

        private static IEnumerable<Gizmo> AppendReligionGizmos(IEnumerable<Gizmo> original, List<Ability> abilities)
        {
            var originalDefs = new HashSet<AbilityDef>();
            foreach (Gizmo g in original)
            {
                if (g is Command_Ability cmd && cmd.Ability != null)
                    originalDefs.Add(cmd.Ability.def);
                yield return g;
            }

            foreach (var ability in abilities)
            {
                // Always show Convert/Reassure variants (different defs)
                // Deduplicate everything else
                bool isVariation = ability.def.defName == "ConvertReligion"
                    || ability.def.defName == "ReassureReligion";

                if (isVariation || !originalDefs.Contains(ability.def))
                {
                    foreach (var gizmo in ability.GetGizmos())
                        yield return gizmo;
                }
            }
        }
    }
}
