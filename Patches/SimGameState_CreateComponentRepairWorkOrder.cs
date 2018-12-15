using BattleTech;
using Harmony;
using UnityEngine;

namespace ArmorRepair
{
    [HarmonyPatch(typeof(SimGameState), "CreateComponentRepairWorkOrder")]
    public static class SimGameState_CreateComponentRepairWorkOrder
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Low)]
        private static void Postfix(MechComponentRef mechComponent, bool isOnMech, WorkOrderEntry_RepairComponent __result)
        {
            var mech = MechLabPanel_LoadMech.CurrentMech;
            if (mechComponent == null)
                return;

            float tpmod = 1;
            float cbmod = 1;

            Logger.LogDebug($"Module Repair Cost for {mechComponent.ComponentDefID}: ");
            Logger.LogDebug("***************************************");

            foreach (var tag in ArmorRepair.ModSettings.RepairCostByTag)
            {
                if (mech != null && mech.Chassis.ChassisTags.Contains(tag.Tag))
                {
                    Logger.LogDebug($" Chassis {tag.Tag} mods tp:{tag.RepairTPCost:0.00} cb:{tag.RepairCBCost:0.00}");

                    tpmod *= tag.RepairTPCost;
                    cbmod *= tag.RepairCBCost;
                }

                if (mechComponent.Def.ComponentTags.Contains(tag.Tag))
                {
                    Logger.LogDebug($" {mechComponent.ComponentDefID} {tag.Tag} mods tp:{tag.RepairTPCost:0.00} cb:{tag.RepairCBCost:0.00}");
                    tpmod *= tag.RepairTPCost;
                    cbmod *= tag.RepairCBCost;
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