using HarmonyLib;
using RimWorld;
using Verse;

namespace IdeoRework
{
    [HarmonyPatch(typeof(Page_ChooseIdeoPreset))]
    [HarmonyPatch("PostOpen")]
    public static class Patch_PageChooseIdeoPreset_PostOpen
    {
        static void Postfix(Page_ChooseIdeoPreset __instance)
        {
            // Skip "Choose your Ideoligion" UI — go directly to our wizard
            Find.WindowStack.Add(new Dialog_TwoStepIdeoWizard(__instance));
            __instance.Close();
        }
    }

    [HarmonyPatch(typeof(Page_ChooseIdeoPreset))]
    [HarmonyPatch("DoCustomize")]
    public static class Patch_PageChooseIdeoPreset_DoCustomize
    {
        [HarmonyPriority(Priority.High)]
        static bool Prefix(Page_ChooseIdeoPreset __instance, bool fluid)
        {
            if (fluid)
                return true;

            Find.WindowStack.Add(new Dialog_TwoStepIdeoWizard(__instance));
            return false;
        }
    }
}
