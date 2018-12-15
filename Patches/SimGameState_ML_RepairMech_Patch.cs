using BattleTech;
using Harmony;

namespace ArmorRepair
{
    [HarmonyPatch(typeof(SimGameState), "ML_RepairMech")]
    public static class SimGameState_ML_RepairMech_Patch
    {
        public static bool Prefix(SimGameState __instance, WorkOrderEntry_RepairMechStructure order)
        {
            if (order.IsMechLabComplete)
            {
                return true;
            }
            else
            {
                MechDef mechByID = __instance.GetMechByID(order.MechLabParent.MechID);
                LocationLoadoutDef locationLoadoutDef = mechByID.GetLocationLoadoutDef(order.Location);
                locationLoadoutDef.CurrentInternalStructure = mechByID.GetChassisLocationDef(order.Location).InternalStructure;
                // Original method resets currentArmor to assignedArmor here for some reason! Removed them from this override
                Logger.LogDebug("ALERT: Intercepted armor reset from ML_RepairMech and prevented it.");
                mechByID.RefreshBattleValue();
                order.SetMechLabComplete(true);

                return false; // Prevent original method from firing
            }
        }
    }
}