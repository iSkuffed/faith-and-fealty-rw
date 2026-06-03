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
