using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace IdeoRework
{
    public enum PreceptStance
    {
        Neutral,
        Rewards,
        Forbids
    }

    public enum ConflictType
    {
        None,
        Engaged,   // One system has precept, other neutral — only triggers if action engaged
        Opposing   // One rewards, other forbids — immediate dissonance
    }

    public struct ConflictEntry
    {
        public HistoryEventDef eventDef;
        public ConflictType type;
    }

    public static class CognitiveDissonanceTracker
    {
        // ── Engagement tracking (per-colony, rolling window) ───────────────

        private static readonly Dictionary<HistoryEventDef, List<int>> EngagementLog = new Dictionary<HistoryEventDef, List<int>>();
        private static readonly int EngagementWindowTicks = 600000; // ~10 days

        public static void LogEngagement(HistoryEventDef eventDef)
        {
            if (eventDef == null) return;
            if (!EngagementLog.ContainsKey(eventDef))
                EngagementLog[eventDef] = new List<int>();
            EngagementLog[eventDef].Add(Find.TickManager.TicksGame);
        }

        private static int GetRecentEngagementCount(HistoryEventDef eventDef)
        {
            if (!EngagementLog.ContainsKey(eventDef)) return 0;
            int cutoff = Find.TickManager.TicksGame - EngagementWindowTicks;
            EngagementLog[eventDef].RemoveAll(t => t < cutoff);
            return EngagementLog[eventDef].Count;
        }

        // ── Stance detection ───────────────────────────────────────────────

        private static PreceptStance GetStance(Ideo ideo, HistoryEventDef eventDef)
        {
            if (ideo == null || eventDef == null) return PreceptStance.Neutral;

            bool hasReward = false;
            bool hasForbid = false;

            foreach (var precept in ideo.PreceptsListForReading)
            {
                foreach (var comp in precept.def.comps)
                {
                    if (comp is PreceptComp_UnwillingToDo unwilling && unwilling.eventDef == eventDef)
                        hasForbid = true;

                    if (comp is PreceptComp_SelfTookMemoryThought self
                        && self.eventDef == eventDef
                        && self.thought?.stages != null
                        && self.thought.stages.Any(s => s.baseMoodEffect > 0))
                        hasReward = true;
                }
            }

            if (hasForbid) return PreceptStance.Forbids;
            if (hasReward) return PreceptStance.Rewards;
            return PreceptStance.Neutral;
        }

        private static ConflictType Classify(PreceptStance religion, PreceptStance ideology)
        {
            // Opposing: one rewards, other forbids
            if (religion == PreceptStance.Forbids && ideology == PreceptStance.Rewards)
                return ConflictType.Opposing;
            if (ideology == PreceptStance.Forbids && religion == PreceptStance.Rewards)
                return ConflictType.Opposing;

            // Engaged: one has stance, other neutral
            if (religion == PreceptStance.Forbids && ideology == PreceptStance.Neutral)
                return ConflictType.Engaged;
            if (ideology == PreceptStance.Forbids && religion == PreceptStance.Neutral)
                return ConflictType.Engaged;
            if (religion == PreceptStance.Rewards && ideology == PreceptStance.Neutral)
                return ConflictType.Engaged;
            if (ideology == PreceptStance.Rewards && religion == PreceptStance.Neutral)
                return ConflictType.Engaged;

            return ConflictType.None;
        }

        // ── Conflict scanning ──────────────────────────────────────────────

        private static List<ConflictEntry> ScanConflicts(Ideo religionIdeo, Ideo ideologyIdeo)
        {
            var conflicts = new List<ConflictEntry>();
            if (religionIdeo == null || ideologyIdeo == null) return conflicts;

            // Collect all eventDefs referenced by either ideo
            var allEventDefs = new HashSet<HistoryEventDef>();

            CollectEventDefs(religionIdeo, allEventDefs);
            CollectEventDefs(ideologyIdeo, allEventDefs);

            // Classify each eventDef
            foreach (var eventDef in allEventDefs)
            {
                var religionStance = GetStance(religionIdeo, eventDef);
                var ideologyStance = GetStance(ideologyIdeo, eventDef);
                var conflictType = Classify(religionStance, ideologyStance);

                if (conflictType != ConflictType.None)
                {
                    conflicts.Add(new ConflictEntry
                    {
                        eventDef = eventDef,
                        type = conflictType
                    });
                }
            }

            return conflicts;
        }

        private static void CollectEventDefs(Ideo ideo, HashSet<HistoryEventDef> eventDefs)
        {
            foreach (var precept in ideo.PreceptsListForReading)
            {
                foreach (var comp in precept.def.comps)
                {
                    if (comp is PreceptComp_UnwillingToDo unwilling && unwilling.eventDef != null)
                        eventDefs.Add(unwilling.eventDef);

                    if (comp is PreceptComp_SelfTookMemoryThought self && self.eventDef != null)
                        eventDefs.Add(self.eventDef);

                    if (comp is PreceptComp_KnowsMemoryThought knows && knows.eventDef != null)
                        eventDefs.Add(knows.eventDef);
                }
            }
        }

        // ── Dissonance stage calculation ───────────────────────────────────

        public static int CalculateDissonanceStage(Ideo religionIdeo, Ideo ideologyIdeo)
        {
            var conflicts = ScanConflicts(religionIdeo, ideologyIdeo);
            if (conflicts.Count == 0) return -1;

            int opposingCount = conflicts.Count(c => c.type == ConflictType.Opposing);
            int engagedCount = 0;
            int activeOpposing = 0;

            foreach (var conflict in conflicts)
            {
                int recentEngagements = GetRecentEngagementCount(conflict.eventDef);
                if (recentEngagements > 0)
                {
                    if (conflict.type == ConflictType.Opposing)
                        activeOpposing += Math.Min(recentEngagements, 3); // Cap at 3
                    else
                        engagedCount += Math.Min(recentEngagements, 3);
                }
            }

            // Score calculation
            int score = opposingCount * 2      // Opposing conflicts always distressing
                      + activeOpposing * 3     // Active opposing engagement worse
                      + engagedCount * 2;      // Engaged conflicts only matter when active

            if (score <= 0) return -1;  // No thought
            if (score <= 3) return 0;   // Mild
            if (score <= 7) return 1;   // Moderate
            return 2;                   // Severe
        }

        // ── Mood thought application ───────────────────────────────────────

        public static void ApplyMoodThought(Pawn pawn, int stage)
        {
            if (pawn.needs?.mood == null) return;

            var dissonanceDef = DefDatabase<ThoughtDef>.GetNamedSilentFail("CognitiveDissonance");
            if (dissonanceDef == null) return;

            var existing = pawn.needs.mood.thoughts.memories.GetFirstMemoryOfDef(dissonanceDef);

            if (stage < 0)
            {
                if (existing != null)
                    pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDef(dissonanceDef);
                return;
            }

            if (existing != null)
            {
                existing.SetForcedStage(stage);
                existing.Renew();
            }
            else
            {
                var thought = (Thought_Memory)ThoughtMaker.MakeThought(dissonanceDef);
                thought.SetForcedStage(stage);
                thought.permanent = true;
                pawn.needs.mood.thoughts.memories.TryGainMemory(thought);
            }
        }

        // ── Certainty erosion ──────────────────────────────────────────────

        public static void ApplyCertaintyErosion(Pawn pawn, int stage, int tickDelta)
        {
            if (stage < 0) return;

            float erosionPerDay = stage switch
            {
                0 => -0.002f,   // -0.2%/day
                1 => -0.008f,   // -0.8%/day
                2 => -0.02f,    // -2.0%/day
                _ => 0f
            };

            float erosion = erosionPerDay * tickDelta / 60000f;

            // Erode ideology certainty (bypass floating text from OffsetCertainty)
            if (pawn.ideo != null)
            {
                var certaintyField = AccessTools.Field(typeof(Pawn_IdeoTracker), "certaintyInt");
                if (certaintyField != null)
                {
                    float current = pawn.ideo.Certainty;
                    float newCertainty = Mathf.Clamp01(current + erosion);
                    certaintyField.SetValue(pawn.ideo, newCertainty);
                }
            }

            // Erode religion certainty
            pawn.SetReligionCertainty(Mathf.Clamp01(pawn.GetReligionCertainty() + erosion));
        }

        // ── Main tick method ───────────────────────────────────────────────

        public static void Tick(int tickDelta)
        {
            try
            {
                var allPawns = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists;
                for (int i = 0; i < allPawns.Count; i++)
                {
                    var pawn = allPawns[i];
                    if (pawn == null || pawn.Dead) continue;

                    var religionIdeo = pawn.GetReligionIdeo();
                    var ideologyIdeo = pawn.Ideo;
                    if (religionIdeo == null || ideologyIdeo == null) continue;

                    int stage = CalculateDissonanceStage(religionIdeo, ideologyIdeo);
                    ApplyMoodThought(pawn, stage);
                    ApplyCertaintyErosion(pawn, stage, tickDelta);
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[IdeoRework] CognitiveDissonanceTracker.Tick: " + ex.Message);
            }
        }
    }
}
