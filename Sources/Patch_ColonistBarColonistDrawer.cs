using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace IdeoRework
{
    [HarmonyPatch(typeof(ColonistBarColonistDrawer))]
    [HarmonyPatch("DrawColonist")]
    public static class Patch_ColonistBarColonistDrawer
    {
        static void Postfix(Rect rect, Pawn colonist)
        {
            if (colonist == null || colonist.Dead) return;

            // Use same size as vanilla icons (20x20 at scale 1.0)
            float scale = Find.ColonistBar?.Scale ?? 1f;
            float iconSize = 20f * scale;
            float iconX = rect.x + 1f;
            float iconY = rect.yMax - iconSize - 1f;

            // Draw ideology role icon (covers vanilla's icon)
            var ideologyRole = IdeoRoleManager.GetRole(colonist, isReligion: false);
            if (ideologyRole != null)
            {
                GUI.color = ideologyRole.ideo.Color;
                GUI.DrawTexture(new Rect(iconX, iconY, iconSize, iconSize), ideologyRole.Icon);
                GUI.color = Color.white;
                iconX += iconSize;
            }

            // Draw religion role icon
            var religionRole = IdeoRoleManager.GetRole(colonist, isReligion: true);
            if (religionRole != null)
            {
                GUI.color = religionRole.ideo.Color;
                GUI.DrawTexture(new Rect(iconX, iconY, iconSize, iconSize), religionRole.Icon);
                GUI.color = Color.white;
            }
        }
    }
}
