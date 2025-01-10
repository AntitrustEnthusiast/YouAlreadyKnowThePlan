using System;
using System.Collections.Generic;

using SerializeField = UnityEngine.SerializeField;

using XRL;
using XRL.World;
using XRL.World.Parts;
using XRL.World.Anatomy;

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

    public static class ThrownWeaponSlot
    {
        public static string RemovedSlotKey = "KnowThePlan_RemovedSlotCount";

        // does not check if slot already exists, just adds 1-2 based on cybernetics
        public static void AddThrownWeaponSlot(GameObject GO)
        {
            Body body = GO.Body;
            if (body is null)
            {
                // UnityEngine.Debug.LogError("tried to add slot to companion with no body");
                return;
            }

            // default to 1 to account for the thrown weapon slot every(?) creature gets
            int slotCount = GO.GetIntPropertyIfSet(RemovedSlotKey) ?? 1;
            BodyPart bodyPart = body.GetBody();
            for (int i = 0; i < slotCount; i++)
            {
                // UnityEngine.Debug.Log($"Adding thrown weapon slot {i + 1}");
                bodyPart.AddPartAt("Thrown Weapon", 0, (string)null, (string)null, (string)null, (string)null, "KnowThePlan::ThrownWeaponSlot", (int?)null, (int?)null, (int?)null, (bool?)null, (bool?)null, (bool?)null, (bool?)null, (bool?)null, (bool?)null, (bool?)null, (bool?)null, (bool?)null, (bool?)null, "Thrown Weapon", (string)null, DoUpdate: true);
            }
        }

        // returns the number of slots removed for later re-adding
        public static int RemoveThrownWeaponSlot(GameObject GO)
        {
            Body body = GO.Body;
            if (body is null)
            {
                UnityEngine.Debug.LogError("tried to remove thrown weapon slot from companion with no body");
                return 0;
            }
            BodyPart thrownSlot = body.GetFirstPart("Thrown Weapon");
            int counter = 0;
            while (thrownSlot is not null)
            {
                counter++;
                // UnityEngine.Debug.Log($"Attempting to remove thrown weapon slot #{counter}");
                body.RemovePart(thrownSlot);
                if (counter >= 30) { return -1; }
                thrownSlot = body.GetFirstPart("Thrown Weapon");
            }
            GO.SetIntProperty(RemovedSlotKey, counter);
            return counter;
        }

        public static BodyPart GetThrownWeaponSlot(Body body)
        {
            BodyPart thrownWeaponSlot = body.GetFirstPart("Thrown Weapon");
            return thrownWeaponSlot;
        }

        public static bool HasThrownWeaponSlot(GameObject GO )
        {
            Body body = GO.Body;
            if (body is null) { return false; }
            return (GetThrownWeaponSlot(body) is not null);
        }

        public static void HandleThrownWeapons(GameObject Follower)
        {
            if (!Options.RemoveThrownWeaponSlot) { return; }
            RemoveThrownWeaponSlot(Follower);
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
                UnityEngine.Debug.LogError("Know The Plan: Error clearing list of forbidden abilities:");
                UnityEngine.Debug.LogError(e.Message);
            }
        }

        public static bool AllCompanions = GetOption("Option_KnowThePlan_AllCompanions").EqualsNoCase("Yes");
        public static bool RequireTelepathy = GetOption("Option_KnowThePlan_RequireTelepathy").EqualsNoCase("Yes");
        public static bool UseEnergy = GetOption("Option_KnowThePlan_UseEnergy").EqualsNoCase("Yes");
        public static bool RemoveThrownWeaponSlot = GetOption("Option_KnowThePlan_RemoveThrownSlot").EqualsNoCase("Yes");
        public static bool ShowCompanionThrownToggle = GetOption("Option_KnowThePlan_ShowCompanionThrownToggle").EqualsNoCase("Yes");

        public static ForbiddenAbilities Forbidden => The.Game.RequireSystem(() => new ForbiddenAbilities());
    }
}
