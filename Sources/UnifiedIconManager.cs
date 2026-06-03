using RimWorld;
using UnityEngine;
using Verse;

namespace IdeoRework
{
    public static class UnifiedIconManager
    {
        public static void DrawIcons(Rect rect, Pawn pawn)
        {
            if (pawn == null || pawn.Dead) return;

            float scale = Find.ColonistBar?.Scale ?? 1f;
            float iconSize = 16f * scale;
            float iconX = rect.x + 1f;
            float iconY = rect.yMax - iconSize - 1f;

            // Draw ideology role icon
            var ideologyRole = IdeoRoleManager.GetRole(pawn, isReligion: false);
            if (ideologyRole != null)
            {
                GUI.color = ideologyRole.ideo.Color;
                GUI.DrawTexture(new Rect(iconX, iconY, iconSize, iconSize), ideologyRole.Icon);
                GUI.color = Color.white;
                iconX += iconSize;
            }

            // Draw religion role icon
            var religionRole = IdeoRoleManager.GetRole(pawn, isReligion: true);
            if (religionRole != null)
            {
                GUI.color = religionRole.ideo.Color;
                GUI.DrawTexture(new Rect(iconX, iconY, iconSize, iconSize), religionRole.Icon);
                GUI.color = Color.white;
            }
        }
    }
}
