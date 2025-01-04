using HarmonyLib;

using Qud.UI;

using XRL.World;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;

namespace KnowThePlan
{
    [HarmonyPatch(typeof(XRL.World.Parts.Brain))]
    class BondedPatch
    {
        // the goal here is to forbid each item in the list every time the player gets a new follower
        [HarmonyPostfix]
        [HarmonyPatch("SetPartyLeader")]
        static void Postfix(Brain __instance, GameObject Object, int Flags = 0, bool Transient = false)
        {
            // we only care about player followers
            if (Object is null || !Object.IsPlayer()) { return; }
            GameObject follower = __instance.ParentObject;

            // handle various options
            if (!CheckClone(Object, follower)) { return; }
            if (!CheckTelepathy(Object, follower)) { return; }
            if (!CheckEnergy(Object, follower)) { return; }

            // iterate over the list of abilities and disable for follower
            ActivatedAbilities abilities = follower.ActivatedAbilities;
            foreach (string command in Options.Forbidden.Abilities)
            {
                ActivatedAbilityEntry ability = abilities.GetAbilityByCommand(command);
                DisableAbility(ability);
            }
        }

        static void DisableAbility(ActivatedAbilityEntry Ability)
        {
            if (Ability is null) { return; }
            if (Ability.Toggleable && Ability.ToggleState)
            {
                Ability.ToggleState = false;
            }
            Ability.AIDisable = true;
        }

        // returns false if execution should stop, true otherwise
        static bool CheckClone(GameObject Leader, GameObject Follower)
        {
            if (Options.AllCompanions) { return true; }
            return Follower.GetStringProperty("CloneOf") == Leader.ID;
        }

        // returns false if execution should stop, true otherwise
        static bool CheckTelepathy(GameObject Leader, GameObject Follower)
        {
            // check if user even wants telepathy requirement
            if (!Options.RequireTelepathy) { return true; }
            // check if leader/follower qualify for telepathy if requirement enabled
            if (!Leader.HasPart<XRL.World.Parts.Mutation.Telepathy>()) { return false; }
            return Leader.CanMakeTelepathicContactWith(Follower);
        }

        // returns false if execution should stop, true otherwise
        static bool CheckEnergy(GameObject Leader, GameObject Follower)
        {
            if (!Options.UseEnergy) { return true; }
            Leader.CompanionDirectionEnergyCost(Follower, 100, "Forbidden Abilities");
            return true;
        }
    }

    // partially based on similar method from Tidy Ability Bar by Tyrir
    // https://steamcommunity.com/sharedfiles/filedetails/?id=3394062891
    // where applicable, the original software license applies
    [HarmonyPatch(typeof(Qud.UI.AbilityManagerLine), nameof(Qud.UI.AbilityManagerLine.SetupContexts))]
    public static class AbilityManagerLine_DisableForCloneCommand
    {
        public const int FLAG_AI_DISABLE = XRL.World.Parts.ActivatedAbilityEntry.FLAG_AI_DISABLE;
        public const string ALLOW_WORD = "Allow for {{Y-Y-Y-Y-Y-M|clones}}";
        public const string FORBID_WORD = "Forbid for {{Y-Y-Y-Y-Y-M|clones}}";

        public const string COMMAND_NAME = "CmdDisableForClones";


        static void Postfix(AbilityManagerLine __instance)
        {
            AbilityManagerLine.Context? context = (AbilityManagerLine.Context)(__instance?.GetNavigationContext());
            ActivatedAbilityEntry? entry = context?.data?.ability;
            if (entry is null)
            {
                UnityEngine.Debug.LogError("Could not find ActivatedAbilityEntry");
                return;
            }

            bool entryIsForbiddenForClones = Options.Forbidden.IsForbidden(entry.Command);

            var menuOption = new XRL.UI.Framework.MenuOption
            {
                InputCommand = COMMAND_NAME,
                Description = (entryIsForbiddenForClones ? ALLOW_WORD : FORBID_WORD),
                disabled = true
            };

            context.menuOptionDescriptions.Insert(0, menuOption);
            context.commandHandlers[COMMAND_NAME] = () =>
            {
                bool allowed = Options.Forbidden.Toggle(entry.Command);
                menuOption.Description = allowed ? FORBID_WORD : ALLOW_WORD;
                XRL.UI.Framework.NavigationController.currentEvent.Handle();
                __instance.screen.Refresh();
                GameManager.Instance.SetActiveLayersForNavCategory("Menu");
            };
        }
    }
}
