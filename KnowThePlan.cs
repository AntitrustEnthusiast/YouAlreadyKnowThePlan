using System;
using System.Linq;
using System.Collections.Generic;

using SerializeField = UnityEngine.SerializeField;

using XRL;
using XRL.UI;
using XRL.World;

namespace KnowThePlan
{
    [Serializable]
    // use IGameSystem to persist between games
    public class ForbiddenAbilities : IGameSystem
    {
        // i'd use a hash set for more performant insert/contains, but serializing them is annoying
        [SerializeField]
        public List<string> Abilities = new();

        public void Forbid(string Ability)
        {
            if (Abilities.Contains(Ability)) { return; }
            Abilities.Add(Ability);
        }

        public bool Allow(string Ability)
        {
            return Abilities.Remove(Ability);
        }

        public bool IsForbidden(string Ability)
        {
            return Abilities.Contains(Ability);
        }

        public void Clear()
        {
            Abilities.Clear();
        }

        public bool IsAllowed(string Ability) => !IsForbidden(Ability);

        // returns the allowed-state after toggling
        // i.e. true if allowed, false if forbidden
        public bool Toggle(string Ability)
        {
            if (Allow(Ability)) { return true; }
            Forbid(Ability);
            return false;
        }
    }


    class Options
    {
        private static string GetOption(string ID, string Default = "")
        {
            return XRL.UI.Options.GetOption(ID, Default: Default);
        }

        // used by Button-type option Option_KnowThePlan_ClearList
        public static void Clear()
        {
            UnityEngine.Debug.Log("User cleared the list of forbidden abilities");
            try
            {
                // throws an error if i use class variable, so just calling from The.Game here
                // only runs on user click anyway, no performance concerns
                ForbiddenAbilities list = The.Game.RequireSystem(() => new ForbiddenAbilities());
                list.Clear();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("Error clearing list:");
                UnityEngine.Debug.LogError(e.Message);
            }
        }

        public static bool AllCompanions = GetOption("Option_KnowThePlan_AllCompanions").EqualsNoCase("Yes");
        public static bool RequireTelepathy = GetOption("Option_KnowThePlan_RequireTelepathy").EqualsNoCase("Yes");
        public static bool UseEnergy = GetOption("Option_KnowThePlan_UseEnergy").EqualsNoCase("Yes");

        public static ForbiddenAbilities Forbidden => The.Game.RequireSystem(() => new ForbiddenAbilities());
    }
}
