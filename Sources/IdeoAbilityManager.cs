using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace IdeoRework
{
    public static class IdeoAbilityManager
    {
        private static Dictionary<int, List<Ability>> ideologyAbilities = new Dictionary<int, List<Ability>>();
        private static Dictionary<int, List<Ability>> religionAbilities = new Dictionary<int, List<Ability>>();

        public static void InitializeAbilities(Pawn pawn, Precept_Role role, bool isReligion)
        {
            var abilities = new List<Ability>();

            // Add abilities from role's granted abilities
            if (role.def.grantedAbilities != null)
            {
                foreach (var abilityDef in role.def.grantedAbilities)
                {
                    var ability = AbilityUtility.MakeAbility(abilityDef, pawn, role);
                    if (ability != null)
                        abilities.Add(ability);
                }
            }

            // Add certainty-affecting abilities for leaders
            if (role.def.leaderRole)
            {
                if (isReligion)
                {
                    // Religion leader: ConvertReligion + ReassureReligion
                    var convertReligion = DefDatabase<AbilityDef>.GetNamedSilentFail("ConvertReligion");
                    var reassureReligion = DefDatabase<AbilityDef>.GetNamedSilentFail("ReassureReligion");
                    if (convertReligion != null)
                        abilities.Add(AbilityUtility.MakeAbility(convertReligion, pawn, role));
                    if (reassureReligion != null)
                        abilities.Add(AbilityUtility.MakeAbility(reassureReligion, pawn, role));
                }
                else
                {
                    // Ideology leader: Convert + Reassure
                    var convert = DefDatabase<AbilityDef>.GetNamedSilentFail("Convert");
                    var reassure = DefDatabase<AbilityDef>.GetNamedSilentFail("Reassure");
                    if (convert != null)
                        abilities.Add(AbilityUtility.MakeAbility(convert, pawn, role));
                    if (reassure != null)
                        abilities.Add(AbilityUtility.MakeAbility(reassure, pawn, role));
                }
            }

            if (isReligion)
                religionAbilities[pawn.thingIDNumber] = abilities;
            else
                ideologyAbilities[pawn.thingIDNumber] = abilities;
        }

        public static void ClearAbilities(Pawn pawn, bool isReligion)
        {
            if (isReligion)
                religionAbilities.Remove(pawn.thingIDNumber);
            else
                ideologyAbilities.Remove(pawn.thingIDNumber);
        }

        public static List<Ability> GetAllAbilities(Pawn pawn)
        {
            var result = new List<Ability>();

            if (ideologyAbilities.TryGetValue(pawn.thingIDNumber, out var ideologyAbs))
                result.AddRange(ideologyAbs);

            if (religionAbilities.TryGetValue(pawn.thingIDNumber, out var religionAbs))
            {
                foreach (var ability in religionAbs)
                {
                    if (!result.Any(a => a.def == ability.def))
                        result.Add(ability);
                }
            }

            return result;
        }

        public static void Clear()
        {
            ideologyAbilities.Clear();
            religionAbilities.Clear();
        }

        public static List<AbilitySaveData> GetIdeologyAbilitySaveData()
        {
            var result = new List<AbilitySaveData>();
            foreach (var kvp in ideologyAbilities)
            {
                result.Add(new AbilitySaveData
                {
                    pawnId = kvp.Key,
                    abilityDefNames = kvp.Value.Select(a => a.def.defName).ToList()
                });
            }
            return result;
        }

        public static List<AbilitySaveData> GetReligionAbilitySaveData()
        {
            var result = new List<AbilitySaveData>();
            foreach (var kvp in religionAbilities)
            {
                result.Add(new AbilitySaveData
                {
                    pawnId = kvp.Key,
                    abilityDefNames = kvp.Value.Select(a => a.def.defName).ToList()
                });
            }
            return result;
        }
    }

    public struct AbilitySaveData : IExposable
    {
        public int pawnId;
        public List<string> abilityDefNames;

        public void ExposeData()
        {
            Scribe_Values.Look(ref pawnId, "pawnId");
            Scribe_Collections.Look(ref abilityDefNames, "abilityDefNames", LookMode.Value);
        }
    }
}
