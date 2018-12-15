using BattleTech;
using Harmony;
using UnityEngine;

namespace ArmorRepair
{
    [HarmonyPatch(typeof(SimGameState), "CreateComponentInstallWorkOrder")]
    public static class SimGameState_CreateComponentInstallWorkOrder
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Low)]
        public static void ChangeCost(string mechSimGameUID,
            MechComponentRef mechComponent, ChassisLocations newLocation, 
            ChassisLocations previousLocation, ref WorkOrderEntry_InstallComponent __result)
        {
            if (newLocation == ChassisLocations.None)
                return;
            if (MechLabPanel_LoadMech.CurrentMech == null || MechLabPanel_LoadMech.CurrentMech.Chassis == null)
                return;
            if (mechComponent.Def == null)
                return;

            if(__result == null)
                return;
            

            float tpmod = 1;
            float cbmod = 1;

            Logger.LogDebug($"Module Install Cost for {mechComponent.ComponentDefID}: ");
            Logger.LogDebug("***************************************");


            foreach (var tag in ArmorRepair.ModSettings.RepairCostByTag)
            {
                if (MechLabPanel_LoadMech.CurrentMech.Chassis.ChassisTags.Contains(tag.Tag))
                {
                    Logger.LogDebug($" Chassis {tag.Tag} mods tp:{tag.InstallTPCost:0.00} cb:{tag.InstallCBCost:0.00}");

                    tpmod *= tag.InstallTPCost;
                    cbmod *= tag.InstallCBCost;
                }

                if (mechComponent.Def.ComponentTags.Contains(tag.Tag))
                {
                    Logger.LogDebug($" {mechComponent.ComponentDefID} {tag.Tag} mods tp:{tag.InstallTPCost:0.00} cb:{tag.InstallCBCost:0.00}");
                    tpmod *= tag.InstallTPCost;
                    cbmod *= tag.InstallCBCost;
                }

            }

            if (tpmod != 1 || cbmod != 1)
            {
                var trav = new Traverse(__result);
                if (tpmod != 1)
                {
                    var cost = trav.Field<int>("Cost");
                    int new_cost = Mathf.CeilToInt(cost.Value * tpmod);
                    Logger.LogDebug($" TP cost: {cost.Value} * {tpmod:0.000} = {new_cost}");
                    cost.Value = new_cost;
                }

                if (cbmod != 1)
                {
                    var cost = trav.Field<int>("CBillCost");
                    int new_cost = Mathf.CeilToInt(cost.Value * cbmod);
                    Logger.LogDebug($" CBIll cost: {cost.Value} * {cbmod:0.000} = {new_cost}");
                    cost.Value = new_cost;
                }

            }
            else
                Logger.LogDebug(" no need to adjust, return");

            Logger.LogDebug("***************************************");
        }
    }
}