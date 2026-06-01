using HarmonyLib;
using Verse;

namespace IdeoRework
{
    [StaticConstructorOnStartup]
    public static class IdeoReworkMod
    {
        static IdeoReworkMod()
        {
            new Harmony("skuffed.ideorework").PatchAll();
        }
    }
}
