using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace IdeoRework
{
    public class IdeoReworkGameComponent : GameComponent
    {
        private List<int> savedReligionIdeoIds = new List<int>();
        private int savedReligionLeaderId = -1;
        private int savedReligionLeaderIdeoId = -1;
        private List<RoleSaveData> savedIdeologyRoles = new List<RoleSaveData>();
        private List<RoleSaveData> savedReligionRoles = new List<RoleSaveData>();
        private List<AbilitySaveData> savedIdeologyAbilities = new List<AbilitySaveData>();
        private List<AbilitySaveData> savedReligionAbilities = new List<AbilitySaveData>();

        public IdeoReworkGameComponent(Game game) { }

        public override void ExposeData()
        {
            base.ExposeData();

            // Populate saved lists from managers before saving
            // Only save during actual gameplay saves, not during InitNewGame
            if (Scribe.mode == LoadSaveMode.Saving && Current.ProgramState == ProgramState.Playing)
            {
                savedReligionIdeoIds.Clear();
                foreach (var ideo in PresetReligions.CreatedReligionIdeos)
                    savedReligionIdeoIds.Add(ideo.id);
                savedReligionLeaderId = ReligionLeaderTracker.ReligionLeader?.thingIDNumber ?? -1;
                var leaderReligion = ReligionLeaderTracker.ReligionLeader?.GetReligionIdeo();
                savedReligionLeaderIdeoId = leaderReligion?.id ?? -1;
                savedIdeologyRoles = IdeoRoleManager.GetIdeologyRoleSaveData();
                savedReligionRoles = IdeoRoleManager.GetReligionRoleSaveData();
                savedIdeologyAbilities = IdeoAbilityManager.GetIdeologyAbilitySaveData();
                savedReligionAbilities = IdeoAbilityManager.GetReligionAbilitySaveData();
            }

            Scribe_Collections.Look(ref savedReligionIdeoIds, "savedReligionIdeoIds", LookMode.Value);
            if (savedReligionIdeoIds == null)
                savedReligionIdeoIds = new List<int>();
            Scribe_Values.Look(ref savedReligionLeaderId, "religionLeaderId", -1);
            Scribe_Values.Look(ref savedReligionLeaderIdeoId, "religionLeaderIdeoId", -1);
            Scribe_Collections.Look(ref savedIdeologyRoles, "savedIdeologyRoles", LookMode.Deep);
            Scribe_Collections.Look(ref savedReligionRoles, "savedReligionRoles", LookMode.Deep);
            Scribe_Collections.Look(ref savedIdeologyAbilities, "savedIdeologyAbilities", LookMode.Deep);
            Scribe_Collections.Look(ref savedReligionAbilities, "savedReligionAbilities", LookMode.Deep);
            if (savedIdeologyRoles == null)
                savedIdeologyRoles = new List<RoleSaveData>();
            if (savedReligionRoles == null)
                savedReligionRoles = new List<RoleSaveData>();
            if (savedIdeologyAbilities == null)
                savedIdeologyAbilities = new List<AbilitySaveData>();
            if (savedReligionAbilities == null)
                savedReligionAbilities = new List<AbilitySaveData>();
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();

            // Always clear — FinalizeInit is the single source of truth for NPC religions
            PresetReligions.CreatedReligionIdeos.Clear();

            // Restore saved religion ideos (player's religion + any previously created NPC religions)
            foreach (var id in savedReligionIdeoIds)
            {
                var ideo = IdeoTrackerHelper.FindIdeoById(id);
                if (ideo != null)
                    PresetReligions.CreatedReligionIdeos.Add(ideo);
            }
            Log.Message($"[IdeoRework] FinalizeInit: restored {PresetReligions.CreatedReligionIdeos.Count} religion ideos from {savedReligionIdeoIds.Count} saved IDs");

            // Ensure ALL NPC factions have a religion from their XML preset
            // This is the single source of truth — no other code creates NPC religions
            var presets = DefDatabase<ReligionPresetDef>.AllDefsListForReading;
            foreach (var faction in Find.FactionManager.AllFactions)
            {
                if (faction.ideos == null || faction.IsPlayer) continue;
                if (faction.def.hidden) continue; // Skip Ancients, AncientsHostile, etc.

                // Skip if faction already has a religion (from saved IDs or previous iteration)
                if (FactionIdeoHelper.FindReligionIdeo(faction.ideos) != null) continue;

                // Find the first matching preset (same logic as GetReligionForFaction)
                ReligionPresetDef matchedPreset = null;
                foreach (var preset in presets)
                {
                    // Skip player presets (no whitelist or blacklist = wizard preset)
                    if ((preset.factionWhitelist == null || preset.factionWhitelist.Count == 0)
                        && (preset.factionBlacklist == null || preset.factionBlacklist.Count == 0))
                        continue;
                    if (preset.factionWhitelist != null && preset.factionWhitelist.Count > 0
                        && !preset.factionWhitelist.Contains(faction.def.defName))
                        continue;
                    if (preset.factionBlacklist != null && preset.factionBlacklist.Count > 0
                        && preset.factionBlacklist.Contains(faction.def.defName))
                        continue;
                    matchedPreset = preset;
                    break;
                }

                if (matchedPreset == null) continue;

                var religionIdeo = PresetReligions.CreateReligionIdeo(matchedPreset, faction.def);
                if (religionIdeo != null)
                {
                    FactionIdeoHelper.AddMinorIdeo(faction.ideos, religionIdeo);
                    Log.Message($"[IdeoRework] FinalizeInit: created religion '{religionIdeo.name}' for faction '{faction.def.defName}'");
                }
            }

            // Save the complete list so it persists correctly
            SaveReligionIdeoIds();

            // Restore religion leader
            if (savedReligionLeaderId >= 0)
            {
                var religionIdeo = IdeoTrackerHelper.FindIdeoById(savedReligionLeaderIdeoId);
                ReligionLeaderTracker.RestoreFromId(savedReligionLeaderId, religionIdeo);
            }

            // Restore role assignments
            RestoreRoles(savedIdeologyRoles, isReligion: false);
            RestoreRoles(savedReligionRoles, isReligion: true);

            // Fix any pawns whose primary ideo or religion tracking was lost on load
            HardOverride.VerifyAndFixAllPlayerPawns();
        }

        private void RestoreRoles(List<RoleSaveData> roles, bool isReligion)
        {
            foreach (var data in roles)
            {
                var pawn = FindPawnById(data.pawnId);
                if (pawn == null) continue;

                var ideo = IdeoTrackerHelper.FindIdeoById(data.ideoId);
                if (ideo == null) continue;

                var role = ideo.RolesListForReading.FirstOrDefault(r => r.def.defName == data.roleDefName);
                if (role == null) continue;

                IdeoRoleManager.AssignRole(pawn, role, isReligion);
            }
        }

        private static Pawn FindPawnById(int pawnId)
        {
            return PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists
                .FirstOrDefault(p => p.thingIDNumber == pawnId);
        }

        public static void SaveReligionIdeoIds()
        {
            var comp = Current.Game?.GetComponent<IdeoReworkGameComponent>();
            if (comp == null) return;
            comp.savedReligionIdeoIds.Clear();
            foreach (var ideo in PresetReligions.CreatedReligionIdeos)
                comp.savedReligionIdeoIds.Add(ideo.id);
        }

        public static void SaveReligionLeaderId()
        {
            var comp = Current.Game?.GetComponent<IdeoReworkGameComponent>();
            if (comp == null) return;
            comp.savedReligionLeaderId = ReligionLeaderTracker.ReligionLeader?.thingIDNumber ?? -1;
            var religionIdeo = ReligionLeaderTracker.ReligionLeader?.GetReligionIdeo();
            comp.savedReligionLeaderIdeoId = religionIdeo?.id ?? -1;
        }
    }

    public static class IdeoTrackerHelper
    {
        public static Ideo FindIdeoById(int id)
        {
            foreach (var ideo in Find.IdeoManager.IdeosListForReading)
            {
                if (ideo.id == id)
                    return ideo;
            }
            return null;
        }
    }
}
