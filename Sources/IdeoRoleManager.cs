using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace IdeoRework
{
    public static class IdeoRoleManager
    {
        private static Dictionary<int, RoleData> ideologyRoles = new Dictionary<int, RoleData>();
        private static Dictionary<int, RoleData> religionRoles = new Dictionary<int, RoleData>();

        public struct RoleData
        {
            public string roleDefName;
            public int ideoId;
            public bool isLeader;
        }

        public static void AssignRole(Pawn pawn, Precept_Role role, bool isReligion)
        {
            var dict = isReligion ? religionRoles : ideologyRoles;

            var toRemove = new List<int>();
            foreach (var kvp in dict)
            {
                if (kvp.Value.roleDefName == role.def.defName
                    && kvp.Value.ideoId == role.ideo.id
                    && kvp.Key != pawn.thingIDNumber)
                {
                    toRemove.Add(kvp.Key);
                }
            }
            foreach (var key in toRemove)
            {
                dict.Remove(key);
                var prevPawn = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists
                    .FirstOrDefault(p => p.thingIDNumber == key);
                if (prevPawn != null)
                {
                    IdeoAbilityManager.ClearAbilities(prevPawn, isReligion);
                    if (isReligion && ReligionLeaderTracker.ReligionLeader == prevPawn)
                        ReligionLeaderTracker.Clear();
                    else if (!isReligion && Faction.OfPlayer.leader == prevPawn)
                        Faction.OfPlayer.leader = null;
                }
                Log.Message($"[IdeoRework] AssignRole: Evicted stale previous holder (thingId={key}) for {role.def.defName} ({role.ideo.name})");
            }

            if (toRemove.Count > 0)
                Log.Message($"[IdeoRework] AssignRole: Evicted {toRemove.Count} previous holder(s) for {role.def.defName} ({role.ideo.name})");

            Log.Message($"[IdeoRework] AssignRole: {pawn.LabelShortCap} <- {role.def.defName} ({role.ideo.name}) isReligion={isReligion}");

            var data = new RoleData
            {
                roleDefName = role.def.defName,
                ideoId = role.ideo.id,
                isLeader = role.def.leaderRole
            };

            if (isReligion)
            {
                religionRoles[pawn.thingIDNumber] = data;
                if (role.def.leaderRole)
                    ReligionLeaderTracker.SetReligionLeader(pawn, role.ideo);
            }
            else
            {
                ideologyRoles[pawn.thingIDNumber] = data;
                if (role.def.leaderRole)
                    Faction.OfPlayer.leader = pawn;
            }

            IdeoAbilityManager.InitializeAbilities(pawn, role, isReligion);
        }

        public static void UnassignRole(Pawn pawn, bool isReligion)
        {
            Log.Message($"[IdeoRework] UnassignRole: {pawn?.LabelShortCap ?? "null"} isReligion={isReligion}");

            if (isReligion)
            {
                religionRoles.Remove(pawn.thingIDNumber);
                IdeoAbilityManager.ClearAbilities(pawn, isReligion: true);
                if (ReligionLeaderTracker.ReligionLeader == pawn)
                    ReligionLeaderTracker.Clear();
            }
            else
            {
                ideologyRoles.Remove(pawn.thingIDNumber);
                IdeoAbilityManager.ClearAbilities(pawn, isReligion: false);
            }
        }

        public static Precept_Role GetRole(Pawn pawn, bool isReligion)
        {
            var dict = isReligion ? religionRoles : ideologyRoles;
            if (!dict.TryGetValue(pawn.thingIDNumber, out var data))
                return null;

            var ideo = Find.IdeoManager.IdeosListForReading.FirstOrDefault(i => i.id == data.ideoId);
            return ideo?.RolesListForReading.FirstOrDefault(r => r.def.defName == data.roleDefName);
        }

        public static bool HasRole(Pawn pawn, bool isReligion)
        {
            var dict = isReligion ? religionRoles : ideologyRoles;
            return dict.ContainsKey(pawn.thingIDNumber);
        }

        public static List<RoleSaveData> GetIdeologyRoleSaveData()
        {
            var result = new List<RoleSaveData>();
            foreach (var kvp in ideologyRoles)
            {
                result.Add(new RoleSaveData
                {
                    pawnId = kvp.Key,
                    roleDefName = kvp.Value.roleDefName,
                    ideoId = kvp.Value.ideoId,
                    isLeader = kvp.Value.isLeader
                });
            }
            return result;
        }

        public static List<RoleSaveData> GetReligionRoleSaveData()
        {
            var result = new List<RoleSaveData>();
            foreach (var kvp in religionRoles)
            {
                result.Add(new RoleSaveData
                {
                    pawnId = kvp.Key,
                    roleDefName = kvp.Value.roleDefName,
                    ideoId = kvp.Value.ideoId,
                    isLeader = kvp.Value.isLeader
                });
            }
            return result;
        }

        public static void RestoreRole(Pawn pawn, RoleSaveData data, bool isReligion)
        {
            var ideo = Find.IdeoManager.IdeosListForReading.FirstOrDefault(i => i.id == data.ideoId);
            if (ideo == null) return;

            var role = ideo.RolesListForReading.FirstOrDefault(r => r.def.defName == data.roleDefName);
            if (role == null) return;

            AssignRole(pawn, role, isReligion);
        }

        public static void Clear()
        {
            ideologyRoles.Clear();
            religionRoles.Clear();
        }

        public static int ValidateAndFix()
        {
            int fixes = 0;
            var alive = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists;

            // Check A: Cross-contamination — religion ideo in ideologyRoles
            var ideKeysToRemove = new List<int>();
            foreach (var kvp in ideologyRoles)
            {
                var ideo = Find.IdeoManager.IdeosListForReading.FirstOrDefault(i => i.id == kvp.Value.ideoId);
                if (ideo != null && PresetReligions.CreatedReligionIdeos.Contains(ideo))
                {
                    ideKeysToRemove.Add(kvp.Key);
                    Log.Warning($"[IdeoRework] ValidateAndFix: Cross-contamination — pawn thingId={kvp.Key} has religion ideo '{ideo.name}' in ideologyRoles");
                }
            }
            foreach (var key in ideKeysToRemove)
            {
                ideologyRoles.Remove(key);
                var p = alive.FirstOrDefault(x => x.thingIDNumber == key);
                if (p != null) IdeoAbilityManager.ClearAbilities(p, isReligion: false);
                fixes++;
            }

            // Check B: Duplicate roles — same (roleDefName, ideoId) mapped to multiple pawns
            fixes += DeduplicateDict(ideologyRoles, alive, isReligion: false);
            fixes += DeduplicateDict(religionRoles, alive, isReligion: true);

            // Check C: Stale entries — pawn dead or not a free colonist
            fixes += RemoveStaleEntries(ideologyRoles, alive, isReligion: false);
            fixes += RemoveStaleEntries(religionRoles, alive, isReligion: true);

            // Check D: IdeoRoleManager ideologyRoles out of sync with vanilla
            foreach (var kvp in ideologyRoles.ToList())
            {
                var p = alive.FirstOrDefault(x => x.thingIDNumber == kvp.Key);
                if (p == null) continue;
                var vanillaRole = p.Ideo?.GetRole(p);
                if (vanillaRole == null)
                {
                    ideologyRoles.Remove(kvp.Key);
                    IdeoAbilityManager.ClearAbilities(p, isReligion: false);
                    if (Faction.OfPlayer.leader == p)
                        Faction.OfPlayer.leader = null;
                    fixes++;
                }
            }

            return fixes;
        }

        private static int DeduplicateDict(Dictionary<int, RoleData> dict, List<Pawn> alive, bool isReligion)
        {
            int fixes = 0;
            var groups = dict.GroupBy(kvp => (kvp.Value.roleDefName, kvp.Value.ideoId))
                             .Where(g => g.Count() > 1)
                             .ToList();

            foreach (var group in groups)
            {
                // Keep the first entry, remove the rest
                var entries = group.ToList();
                for (int i = 1; i < entries.Count; i++)
                {
                    var key = entries[i].Key;
                    dict.Remove(key);
                    var p = alive.FirstOrDefault(x => x.thingIDNumber == key);
                    if (p != null)
                    {
                        IdeoAbilityManager.ClearAbilities(p, isReligion);
                        if (isReligion && ReligionLeaderTracker.ReligionLeader == p)
                            ReligionLeaderTracker.Clear();
                        else if (!isReligion && Faction.OfPlayer.leader == p)
                            Faction.OfPlayer.leader = null;
                    }
                    fixes++;
                }
                Log.Message($"[IdeoRework] ValidateAndFix: Deduped {entries.Count - 1} duplicate(s) for {group.Key.roleDefName} (ideoId={group.Key.ideoId})");
            }
            return fixes;
        }

        private static int RemoveStaleEntries(Dictionary<int, RoleData> dict, List<Pawn> alive, bool isReligion)
        {
            int fixes = 0;
            var toRemove = dict.Keys.Where(key => !alive.Any(p => p.thingIDNumber == key)).ToList();
            foreach (var key in toRemove)
            {
                dict.Remove(key);
                fixes++;
            }
            if (toRemove.Count > 0)
                Log.Message($"[IdeoRework] ValidateAndFix: Removed {toRemove.Count} stale entry(ies) from {(isReligion ? "religionRoles" : "ideologyRoles")}");
            return fixes;
        }
    }

    public struct RoleSaveData : IExposable
    {
        public int pawnId;
        public string roleDefName;
        public int ideoId;
        public bool isLeader;

        public void ExposeData()
        {
            Scribe_Values.Look(ref pawnId, "pawnId");
            Scribe_Values.Look(ref roleDefName, "roleDefName");
            Scribe_Values.Look(ref ideoId, "ideoId");
            Scribe_Values.Look(ref isLeader, "isLeader");
        }
    }
}
