using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace IdeoRework
{
    public static class CognitiveDissonanceExperimental
    {
        private enum PreceptCategory
        {
            Slavery,
            Cannibalism,
            OrganUse,
            IdeoDiversity,
            Lovin,
            Execution
        }

        private class DissonanceErosion : IExposable
        {
            public Pawn pawn;
            public PreceptCategory category;
            public bool isDoer;
            public float totalErosion;
            public float remainingErosion;
            public int startTick;
            public int durationTicks;
            public bool erodeIdeology;

            public DissonanceErosion()
            {
            }

            public void ExposeData()
            {
                Scribe_References.Look(ref pawn, "pawn");
                Scribe_Values.Look(ref category, "category", PreceptCategory.Slavery);
                Scribe_Values.Look(ref isDoer, "isDoer", false);
                Scribe_Values.Look(ref totalErosion, "totalErosion", 0f);
                Scribe_Values.Look(ref remainingErosion, "remainingErosion", 0f);
                Scribe_Values.Look(ref startTick, "startTick", 0);
                Scribe_Values.Look(ref durationTicks, "durationTicks", 0);
                Scribe_Values.Look(ref erodeIdeology, "erodeIdeology", false);
            }
        }

        private static readonly Dictionary<HistoryEventDef, PreceptCategory> EventToCategory = new Dictionary<HistoryEventDef, PreceptCategory>();

        private static readonly Dictionary<PreceptCategory, string> CategoryToIssueDefName = new Dictionary<PreceptCategory, string>
        {
            { PreceptCategory.Slavery, "Slavery" },
            { PreceptCategory.Cannibalism, "Cannibalism" },
            { PreceptCategory.OrganUse, "OrganUse" },
            { PreceptCategory.IdeoDiversity, "IdeoDiversity" },
            { PreceptCategory.Lovin, "Lovin" },
            { PreceptCategory.Execution, "Execution" },
        };

        private static readonly Dictionary<PreceptCategory, (string severe, string mild)> CategoryThoughtDefs = new Dictionary<PreceptCategory, (string, string)>
        {
            { PreceptCategory.Slavery, ("CD_Exp_Slavery_Severe", "CD_Exp_Slavery_Mild") },
            { PreceptCategory.Cannibalism, ("CD_Exp_Cannibalism_Severe", "CD_Exp_Cannibalism_Mild") },
            { PreceptCategory.OrganUse, ("CD_Exp_OrganUse_Severe", "CD_Exp_OrganUse_Mild") },
            { PreceptCategory.IdeoDiversity, ("CD_Exp_IdeoDiversity_Severe", "CD_Exp_IdeoDiversity_Mild") },
            { PreceptCategory.Lovin, ("CD_Exp_Lovin_Severe", "CD_Exp_Lovin_Mild") },
            { PreceptCategory.Execution, ("CD_Exp_Execution_Severe", "CD_Exp_Execution_Mild") },
        };

        private static List<DissonanceErosion> activeErosions = new List<DissonanceErosion>();
        private static int diversityCheckCounter = 0;

        public static void Reset()
        {
            activeErosions.Clear();
            diversityCheckCounter = 0;
        }

        public static void ExposeData()
        {
            Scribe_Collections.Look(ref activeErosions, "fnfExperimentalErosions", LookMode.Deep);
            Scribe_Values.Look(ref diversityCheckCounter, "fnfExperimentalDiversityCheckCounter", 0);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (activeErosions == null)
                    activeErosions = new List<DissonanceErosion>();

                activeErosions.RemoveAll(erosion => erosion == null || erosion.pawn == null || erosion.pawn.Dead || erosion.pawn.Destroyed);
            }
        }

        private const int DiversityCheckIntervalTicks = 60000;

        static CognitiveDissonanceExperimental()
        {
            MapEventToCategory("SoldSlave", PreceptCategory.Slavery);
            MapEventToCategory("EnslavedPrisoner", PreceptCategory.Slavery);
            MapEventToCategory("EnslavedPrisonerNotPreviouslyEnslaved", PreceptCategory.Slavery);
            MapEventToCategory("QuestPrisonerEnslaved", PreceptCategory.Slavery);

            MapEventToCategory("AteHumanMeat", PreceptCategory.Cannibalism);
            MapEventToCategory("AteHumanMeatDirect", PreceptCategory.Cannibalism);
            MapEventToCategory("AteHumanMeatAsIngredient", PreceptCategory.Cannibalism);

            MapEventToCategory("HarvestedOrgan", PreceptCategory.OrganUse);
            MapEventToCategory("HarvestedOrganFromColonist", PreceptCategory.OrganUse);
            MapEventToCategory("HarvestedOrganFromGuest", PreceptCategory.OrganUse);
            MapEventToCategory("SoldOrgan", PreceptCategory.OrganUse);
            MapEventToCategory("TradedOrgan", PreceptCategory.OrganUse);
            MapEventToCategory("InstalledOrgan", PreceptCategory.OrganUse);

            MapEventToCategory("GotLovin", PreceptCategory.Lovin);
            MapEventToCategory("GotLovin_Spouse", PreceptCategory.Lovin);
            MapEventToCategory("GotLovin_NonSpouse", PreceptCategory.Lovin);
            MapEventToCategory("InitiatedLovin", PreceptCategory.Lovin);

            MapEventToCategory("ExecutedPrisoner", PreceptCategory.Execution);
            MapEventToCategory("ExecutedPrisonerGuilty", PreceptCategory.Execution);
            MapEventToCategory("ExecutedPrisonerInnocent", PreceptCategory.Execution);
            MapEventToCategory("ExecutedGuest", PreceptCategory.Execution);
            MapEventToCategory("ExecutedColonist", PreceptCategory.Execution);
        }

        private static void MapEventToCategory(string eventDefName, PreceptCategory category, bool optionalDlc = false)
        {
            var def = DefDatabase<HistoryEventDef>.GetNamedSilentFail(eventDefName);
            if (def != null)
                EventToCategory[def] = category;
            else if (!optionalDlc)
                Log.Warning("[IdeoRework] CognitiveDissonanceExperimental: HistoryEventDef '" + eventDefName + "' not found.");
        }

        private static PreceptStance GetStanceForIssue(Ideo ideo, string issueDefName)
        {
            if (ideo == null) return PreceptStance.Neutral;

            foreach (var precept in ideo.PreceptsListForReading)
            {
                if (precept.def.issue == null || precept.def.issue.defName != issueDefName)
                    continue;

                bool hasForbid = false;
                bool hasReward = false;

                foreach (var comp in precept.def.comps)
                {
                    if (comp is PreceptComp_UnwillingToDo)
                        hasForbid = true;
                    if (comp is PreceptComp_SelfTookMemoryThought self && self.thought?.stages != null
                        && self.thought.stages.Any(s => s.baseMoodEffect > 0f))
                        hasReward = true;
                }

                if (hasForbid) return PreceptStance.Forbids;
                if (hasReward) return PreceptStance.Rewards;
            }

            return PreceptStance.Neutral;
        }

        private static bool StancesConflict(PreceptStance a, PreceptStance b)
        {
            return (a == PreceptStance.Rewards && b == PreceptStance.Forbids)
                || (a == PreceptStance.Forbids && b == PreceptStance.Rewards);
        }

        // ── Event handling ─────────────────────────────────────────────

        public static void OnHistoryEvent(HistoryEvent ev)
        {
            try
            {
                if (ev.def == null) return;
                if (!EventToCategory.TryGetValue(ev.def, out var category)) return;

                if (!ev.args.TryGetArg("Doer", out Pawn doer)) return;
                if (doer == null || doer.Dead || doer.Destroyed) return;

                if (!CheckAndTrigger(doer, category, true))
                    return;

                var allPawns = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists;
                for (int i = 0; i < allPawns.Count; i++)
                {
                    var pawn = allPawns[i];
                    if (pawn == null || pawn == doer || pawn.Dead || pawn.Destroyed) continue;
                    if (pawn.IsSlave || pawn.IsPrisoner) continue;

                    CheckAndTrigger(pawn, category, false);
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[IdeoRework] CognitiveDissonanceExperimental.OnHistoryEvent: " + ex.Message);
            }
        }

        private static bool CheckAndTrigger(Pawn pawn, PreceptCategory category, bool isDoer)
        {
            var ideology = pawn.Ideo;
            var religion = pawn.GetReligionIdeo();
            if (ideology == null || religion == null) return false;

            string issueName = CategoryToIssueDefName[category];
            var ideoStance = GetStanceForIssue(ideology, issueName);
            var religionStance = GetStanceForIssue(religion, issueName);

            if (!StancesConflict(ideoStance, religionStance))
                return false;

            ApplyMoodThought(pawn, isDoer ? CategoryThoughtDefs[category].severe : CategoryThoughtDefs[category].mild);
            ReplaceCertaintyErosion(pawn, category, isDoer);
            return true;
        }

        // ── Mood thought application ───────────────────────────────────

        private static void ApplyMoodThought(Pawn pawn, string thoughtDefName)
        {
            if (pawn.needs?.mood == null) return;

            var thoughtDef = DefDatabase<ThoughtDef>.GetNamedSilentFail(thoughtDefName);
            if (thoughtDef == null)
            {
                Log.Warning($"[IdeoRework] ThoughtDef '{thoughtDefName}' not found.");
                return;
            }

            var existing = pawn.needs.mood.thoughts.memories.GetFirstMemoryOfDef(thoughtDef);
            if (existing != null)
            {
                existing.Renew();
                return;
            }

            var thought = (Thought_Memory)ThoughtMaker.MakeThought(thoughtDef);
            pawn.needs.mood.thoughts.memories.TryGainMemory(thought);
        }

        // ── Certainty erosion ──────────────────────────────────────────

        private static void ReplaceCertaintyErosion(Pawn pawn, PreceptCategory category, bool isDoer)
        {
            activeErosions.RemoveAll(e => e.pawn == pawn && e.category == category && e.isDoer == isDoer);

            float totalErosion;
            int durationTicks;

            switch (category)
            {
                case PreceptCategory.Slavery:
                case PreceptCategory.Cannibalism:
                    totalErosion = isDoer ? -0.10f : -0.04f;
                    durationTicks = isDoer ? (int)(10f * 60000f) : (int)(4f * 60000f);
                    break;
                case PreceptCategory.OrganUse:
                    totalErosion = isDoer ? -0.08f : -0.015f;
                    durationTicks = isDoer ? (int)(8f * 60000f) : (int)(3f * 60000f);
                    break;
                case PreceptCategory.IdeoDiversity:
                    totalErosion = isDoer ? -0.04f : -0.005f;
                    durationTicks = isDoer ? (int)(1f * 60000f) : (int)(2f * 60000f);
                    break;
                case PreceptCategory.Lovin:
                    totalErosion = isDoer ? -0.05f : -0.01f;
                    durationTicks = isDoer ? (int)(6f * 60000f) : (int)(3f * 60000f);
                    break;
                case PreceptCategory.Execution:
                    totalErosion = isDoer ? -0.07f : -0.015f;
                    durationTicks = isDoer ? (int)(8f * 60000f) : (int)(4f * 60000f);
                    break;
                default:
                    return;
            }

            bool erodeIdeology = Rand.Bool;

            activeErosions.Add(new DissonanceErosion
            {
                pawn = pawn,
                category = category,
                isDoer = isDoer,
                totalErosion = totalErosion,
                remainingErosion = totalErosion,
                startTick = Find.TickManager.TicksGame,
                durationTicks = durationTicks,
                erodeIdeology = erodeIdeology
            });
        }

        private static void ApplyErosion(Pawn pawn, float amount, bool erodeIdeology)
        {
            if (pawn.ideo == null) return;

            if (erodeIdeology)
            {
                float current = pawn.ideo.Certainty;
                float newCertainty = Mathf.Clamp01(current + amount);
                var certaintyField = AccessTools.Field(typeof(Pawn_IdeoTracker), "certaintyInt");
                if (certaintyField != null)
                    certaintyField.SetValue(pawn.ideo, newCertainty);
            }
            else
            {
                float current = pawn.GetReligionCertainty();
                float newCertainty = Mathf.Clamp01(current + amount);
                pawn.SetReligionCertainty(newCertainty);
                ReligionConversionTracker.CheckForConversion(pawn, newCertainty);
            }
        }

        // ── Tick ───────────────────────────────────────────────────────

        public static void Tick(int delta)
        {
            try
            {
                TickErosions(delta);
                TickDiversityOfThought();
            }
            catch (Exception ex)
            {
                Log.Warning("[IdeoRework] CognitiveDissonanceExperimental.Tick: " + ex.Message);
            }
        }

        private static void TickErosions(int delta)
        {
            for (int i = activeErosions.Count - 1; i >= 0; i--)
            {
                var erosion = activeErosions[i];

                if (erosion.pawn == null || erosion.pawn.Dead || erosion.pawn.Destroyed)
                {
                    activeErosions.RemoveAt(i);
                    continue;
                }

                int elapsed = Find.TickManager.TicksGame - erosion.startTick;
                if (elapsed >= erosion.durationTicks)
                {
                    if (erosion.remainingErosion != 0f)
                        ApplyErosion(erosion.pawn, erosion.remainingErosion, erosion.erodeIdeology);
                    activeErosions.RemoveAt(i);
                    continue;
                }

                float erosionThisTick = erosion.totalErosion / (float)erosion.durationTicks * delta;
                erosion.remainingErosion -= erosionThisTick;
                activeErosions[i] = erosion;
                ApplyErosion(erosion.pawn, erosionThisTick, erosion.erodeIdeology);
            }
        }

        // ── Diversity of Thought ───────────────────────────────────────

        private static void TickDiversityOfThought()
        {
            if (diversityCheckCounter++ < DiversityCheckIntervalTicks / 250)
                return;

            diversityCheckCounter = 0;

            try
            {
                var allPawns = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists;
                if (allPawns.Count < 2) return;

                var colonyIdeologies = new HashSet<Ideo>();
                foreach (var p in allPawns)
                {
                    if (p.Ideo != null)
                        colonyIdeologies.Add(p.Ideo);
                }

                if (colonyIdeologies.Count < 2) return;

                string issueName = CategoryToIssueDefName[PreceptCategory.IdeoDiversity];

                for (int i = 0; i < allPawns.Count; i++)
                {
                    var pawn = allPawns[i];
                    if (pawn == null || pawn.Dead || pawn.Destroyed) continue;
                    if (pawn.IsSlave || pawn.IsPrisoner) continue;

                    var ideology = pawn.Ideo;
                    var religion = pawn.GetReligionIdeo();
                    if (ideology == null || religion == null) continue;

                    var ideoStance = GetStanceForIssue(ideology, issueName);
                    var religionStance = GetStanceForIssue(religion, issueName);

                    if (StancesConflict(ideoStance, religionStance))
                    {
                        ApplyMoodThought(pawn, CategoryThoughtDefs[PreceptCategory.IdeoDiversity].mild);
                        ReplaceCertaintyErosion(pawn, PreceptCategory.IdeoDiversity, false);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[IdeoRework] TickDiversityOfThought: " + ex.Message);
            }
        }

        public static void OnNewColonistJoined(Pawn newColonist)
        {
            try
            {
                if (newColonist?.Ideo == null) return;
                if (newColonist.IsSlave || newColonist.IsPrisoner) return;

                var allPawns = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists;

                bool hasDifferentIdeology = false;
                for (int i = 0; i < allPawns.Count; i++)
                {
                    var p = allPawns[i];
                    if (p == newColonist) continue;
                    if (p.Ideo != null && p.Ideo != newColonist.Ideo)
                    {
                        hasDifferentIdeology = true;
                        break;
                    }
                }

                if (!hasDifferentIdeology) return;

                string issueName = CategoryToIssueDefName[PreceptCategory.IdeoDiversity];

                for (int i = 0; i < allPawns.Count; i++)
                {
                    var pawn = allPawns[i];
                    if (pawn == newColonist) continue;
                    if (pawn.Dead || pawn.Destroyed || pawn.IsSlave || pawn.IsPrisoner) continue;

                    var ideology = pawn.Ideo;
                    var religion = pawn.GetReligionIdeo();
                    if (ideology == null || religion == null) continue;

                    var ideoStance = GetStanceForIssue(ideology, issueName);
                    var religionStance = GetStanceForIssue(religion, issueName);

                    if (StancesConflict(ideoStance, religionStance))
                    {
                        ApplyMoodThought(pawn, CategoryThoughtDefs[PreceptCategory.IdeoDiversity].severe);
                        ReplaceCertaintyErosion(pawn, PreceptCategory.IdeoDiversity, true);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[IdeoRework] OnNewColonistJoined: " + ex.Message);
            }
        }

        // ── Cleanup ────────────────────────────────────────────────────

        public static void RemoveAllModifiers()
        {
            foreach (var entry in CategoryThoughtDefs.Values)
            {
                var severeDef = DefDatabase<ThoughtDef>.GetNamedSilentFail(entry.severe);
                if (severeDef != null)
                {
                    RemoveThoughtFromAllPawns(severeDef);
                }

                var mildDef = DefDatabase<ThoughtDef>.GetNamedSilentFail(entry.mild);
                if (mildDef != null)
                {
                    RemoveThoughtFromAllPawns(mildDef);
                }
            }

            activeErosions.Clear();
            diversityCheckCounter = 0;

            Log.Message("[IdeoRework] Experimental Cognitive Dissonance modifiers cleared.");
        }

        private static void RemoveThoughtFromAllPawns(ThoughtDef def)
        {
            var allPawns = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists;
            for (int i = 0; i < allPawns.Count; i++)
            {
                var pawn = allPawns[i];
                if (pawn.needs?.mood != null)
                    pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDef(def);
            }
        }
    }
}
