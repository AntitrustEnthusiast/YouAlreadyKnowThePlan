using System;

using XRL;
using XRL.Core;
using XRL.World;
using XRL.World.Parts;

namespace KnowThePlan
{
    [PlayerMutator]
    [HasCallAfterGameLoaded]
    public class KnowThePlanPlayerMutator : IPlayerMutator
    {
        public void mutate(GameObject player)
        {
            // add our listener to the player when a New Game begins
            _ = player.AddPart<KnowThePlan_PlayerListener>();
        }

        [CallAfterGameLoaded]
        public static void GameLoadedCallback()
        {
            // Called whenever loading a save game
            try
            {

                var player = XRLCore.Core.Game.Player.Body;
                _ = (player.RequirePart<KnowThePlan_PlayerListener>());
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("Error mutating player on load:");
                UnityEngine.Debug.LogError(e.Message);
            }
        }
    }

    // [Serializable]
    // we don't need serialization for this class, it stores no data
    // leaving as comment in case I merge with the ForbiddenAbilities class later
    // HandleEvent functions are partially based on functions from Clever Girl by Kizby:
    // https://github.com/Kizby/Clever-Girl
    class KnowThePlan_PlayerListener : IPart
    {
        private string _actionName = "Already Know The Plan - Toggle Thrown";
        private string _command = "KnowThePlan_ToggleThrown";

        public override bool WantEvent(int ID, int cascade) =>
            base.WantEvent(ID, cascade) ||
            ID == OwnerGetInventoryActionsEvent.ID ||
            ID == InventoryActionEvent.ID;

        public override bool HandleEvent(OwnerGetInventoryActionsEvent E)
        {
            if (!Options.ShowCompanionThrownToggle) { return true; }
            if ((E.Object is null) || E.Object.IsPlayer() || !E.Actor.IsPlayer()) { return true; }
            if (E.Object.IsPlayerLed() != true) { return true; }

            if (E.Object.HasPart(typeof(CannotBeInfluenced)))
            {
                // don't manage someone who can't be managed
                return true;
            }
            bool hasThrownWeaponSlot = ThrownWeaponSlot.HasThrownWeaponSlot(E.Object);
            string displayName = $"{(hasThrownWeaponSlot ? "forbid" : "allow")} throwing weapons";
            _ = E.AddAction(_actionName, displayName, _command, Key: 'W', FireOnActor: true, WorksAtDistance: true);
            return true;
        }

        public override bool HandleEvent(InventoryActionEvent E)
        {
            if (E.Command == _command && ParentObject.CheckCompanionDirection(E.Item))
            {
                if (ThrownWeaponSlot.HasThrownWeaponSlot(E.Item))
                {
                    ThrownWeaponSlot.RemoveThrownWeaponSlot(E.Item);
                }
                else
                {
                    ThrownWeaponSlot.AddThrownWeaponSlot(E.Item);
                }
                ParentObject.CompanionDirectionEnergyCost(E.Item, 100, "Manage Thrown Weapon Plan");
                E.RequestInterfaceExit();
            }
            return true;
        }
    }
}