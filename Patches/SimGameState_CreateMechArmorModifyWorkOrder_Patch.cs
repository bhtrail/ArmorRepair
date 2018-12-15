using System;
using BattleTech;
using CustomComponents;
using Harmony;
using UnityEngine;

namespace ArmorRepair
{
    [HarmonyPatch(typeof(SimGameState), "CreateMechArmorModifyWorkOrder")]
    public static class SimGameState_CreateMechArmorModifyWorkOrder_Patch
    {
        private static void Postfix(ref SimGameState __instance, 
            ref string mechSimGameUID,
            ref ChassisLocations location,
            ref int armorDiff, ref int frontArmor, ref int rearArmor, ref WorkOrderEntry_ModifyMechArmor __result)
        {
            string id = string.Format("MechLab - ModifyArmor - {0}", __instance.GenerateSimGameUID());

            try
            {
                float mechTonnageModifier = 1f;
                int techCost = 0;
                int cbillCost = 0;


                foreach (MechDef mechDef in __instance.ActiveMechs.Values)
                {
                    if (mechDef.GUID == mechSimGameUID)
                    {
                        ArmorRepairFactor armor = null;
                        MechComponentRef armoritem = null;
                        foreach (var item in mechDef.Inventory)
                        {
                            if (item.IsCategory(ArmorRepair.ModSettings.ArmorCategory))
                            {
                                armor = item.GetComponent<ArmorRepairFactor>();
                                armoritem = item;
                                break;
                            }
                        }

                        Logger.LogDebug("Armor WO SubEntry Costing: ");
                        Logger.LogDebug("***************************************");
                        Logger.LogDebug(" location: " + location.ToString());
                        Logger.LogDebug(" armorDifference: " + armorDiff);
                        Logger.LogDebug(" mechTonnage: " + mechDef.Chassis.Tonnage);
                        Logger.LogDebug(" mechTonnageModifier: " + mechTonnageModifier);
                        if (armor != null)
                        {
                            Logger.LogDebug($" ArmorRepair mods tp:{armor.ArmorTPCost:0.00} cb:{armor.ArmorCBCost:0.00}");
                        }

                        float atpcost = armor?.ArmorTPCost ?? 1;
                        float acbcost = armor?.ArmorCBCost ?? 1;


                        if(ArmorRepair.ModSettings.RepairCostByTag != null && ArmorRepair.ModSettings.RepairCostByTag.Length > 0)
                            foreach (var cost in ArmorRepair.ModSettings.RepairCostByTag)
                            {
                                if (mechDef.Chassis.ChassisTags.Contains(cost.Tag))
                                {
                                    Logger.LogDebug($" Chassis {cost.Tag} mods tp:{cost.ArmorTPCost:0.00} cb:{cost.ArmorCBCost:0.00}");
                                    atpcost *= cost.ArmorTPCost;
                                    acbcost *= cost.ArmorCBCost;
                                }

                                if(armoritem != null && armoritem.Def.ComponentTags.Contains(cost.Tag))
                                {
                                    Logger.LogDebug($" {armoritem.ComponentDefID} {cost.Tag} mods tp:{cost.ArmorTPCost:0.00} cb:{cost.ArmorCBCost:0.00}");
                                    atpcost *= cost.ArmorTPCost;
                                    acbcost *= cost.ArmorCBCost;
                                }

                            }


                        // If ScaleArmorCostByTonnage is enabled, make the mech tonnage work as a percentage tech cost reduction (95 tons = 0.95 or "95%" of the cost, 50 tons = 0.05 or "50%" of the cost etc)
                        if (ArmorRepair.ModSettings.ScaleArmorCostByTonnage)
                        {
                            mechTonnageModifier = mechDef.Chassis.Tonnage * 0.01f;
                        }

                        float locationTechCost = ((armorDiff * mechTonnageModifier) * __instance.Constants.MechLab.ArmorInstallTechPoints) * atpcost;
                        float locationCbillCost = ((armorDiff * mechTonnageModifier) * __instance.Constants.MechLab.ArmorInstallCost) * acbcost;
                        techCost = Mathf.CeilToInt(locationTechCost);
                        cbillCost = Mathf.CeilToInt(locationCbillCost);

                        Logger.LogDebug($" tpmod: {atpcost:0.000}");
                        Logger.LogDebug($" cbmod: {acbcost:0.000}");

                        Logger.LogDebug(" techCost: " + techCost);
                        Logger.LogDebug(" cbillCost: " + cbillCost);
                        Logger.LogDebug("***************************************");
                    }
                }

                __result = new WorkOrderEntry_ModifyMechArmor(id, string.Format("Modify Armor - {0}", location.ToString()), mechSimGameUID, techCost, location, frontArmor, rearArmor, cbillCost, string.Empty);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }

        }
    }
}