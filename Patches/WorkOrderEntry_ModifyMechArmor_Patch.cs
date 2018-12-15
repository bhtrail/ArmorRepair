using System;
using BattleTech;
using Harmony;
using UnityEngine;

namespace ArmorRepair
{
    [HarmonyPatch(typeof(WorkOrderEntry_ModifyMechArmor))]
    [HarmonyPatch(new Type[]
    {
        typeof(string),
        typeof(string),
        typeof(string),
        typeof(int),
        typeof(ChassisLocations),
        typeof(int),
        typeof(int),
        typeof(int),
        typeof(string)
    })]
    [HarmonyPatch(MethodType.Constructor)]
    public static class WorkOrderEntry_ModifyMechArmor_Patch
    {
        private static void Prefix(ref int cbillCost, ref int techCost, int desiredFrontArmor, int desiredRearArmor)
        {
            try
            {

                float techCostModifier = 0.01f; // Modify int based armor techCosts to a pseudo float
                float num = techCost * techCostModifier;
                techCost = Mathf.CeilToInt(num);
                cbillCost = Mathf.CeilToInt(cbillCost);

                Logger.LogDebug("Armor WO Costing: ");
                Logger.LogDebug("*********************");
                Logger.LogDebug("techCost: " + techCost);
                Logger.LogDebug("cBillCost: " + cbillCost);
                Logger.LogDebug("*********************");

            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }
    }
}