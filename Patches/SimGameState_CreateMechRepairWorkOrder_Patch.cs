using System;
using BattleTech;
using CustomComponents;
using Harmony;
using UnityEngine;

namespace ArmorRepair
{
    [HarmonyPatch(typeof(SimGameState), "CreateMechRepairWorkOrder")]
    public static class SimGameState_CreateMechRepairWorkOrder_Patch
    {

        public static void Postfix(ref SimGameState __instance, ref string mechSimGameUID, ref ChassisLocations location, ref int structureCount, ref WorkOrderEntry_RepairMechStructure __result)
        {
            try
            {

                float mechTonnageModifier = 1f;
                // Original method code, this is still needed to work out zero structure modifiers 
                string id = string.Format("MechLab - RepairMech - {0}", __instance.GenerateSimGameUID());
                bool is_repaired = false;
                float cbmod = 1f;
                float tpmod = 1f;

                foreach (MechDef mechDef in __instance.ActiveMechs.Values)
                {
                    if (mechDef.GUID == mechSimGameUID)
                    {
                        if (mechDef.GetChassisLocationDef(location).InternalStructure == (float)structureCount)
                        {
                            is_repaired = true;
                            break;
                        }
                        Logger.LogDebug("Structure WO Subentry Costing:");
                        Logger.LogDebug("***************************************");
                        Logger.LogDebug(" location: " + location.ToString());
                        Logger.LogDebug(" structureCount: " + structureCount);
                        Logger.LogDebug(" mechTonnageModifier: " + mechTonnageModifier);

                        // If ScaleStructureCostByTonnage is enabled, make the mech tonnage work as a percentage tech cost reduction (95 tons = 0.95 or "95%" of the cost, 50 tons = 0.05 or "50%" of the cost etc)
                        if (ArmorRepair.ModSettings.ScaleStructureCostByTonnage)
                        {
                            mechTonnageModifier = mechDef.Chassis.Tonnage * 0.01f;
                        }

                        StructureRepairFactor str = null;
                        MechComponentRef structitem = null;
                        foreach (var item in mechDef.Inventory)
                        {
                            if (item.IsCategory(ArmorRepair.ModSettings.StructureCategory))
                            {
                                str = item.GetComponent<StructureRepairFactor>();
                                structitem = item;
                                break;
                            }
                        }

                        if (str != null)
                        {
                            Logger.LogDebug($" StructRepair mods tp:{str.StructureTPCost:0.00} cb:{str.StructureCBCost:0.00}");
                        }

                        tpmod *= str?.StructureTPCost ?? 1;
                        cbmod *= str?.StructureCBCost ?? 1;


                        if (ArmorRepair.ModSettings.RepairCostByTag != null && ArmorRepair.ModSettings.RepairCostByTag.Length > 0)
                            foreach (var cost in ArmorRepair.ModSettings.RepairCostByTag)
                            {
                                if (mechDef.Chassis.ChassisTags.Contains(cost.Tag))
                                {
                                    Logger.LogDebug($" Chassis {cost.Tag} mods tp:{cost.StructureTPCost:0.00} cb:{cost.StructureCBCost:0.00}");
                                    tpmod *= cost.StructureTPCost;
                                    cbmod *= cost.StructureCBCost;
                                }

                                if (structitem != null && structitem.Def.ComponentTags.Contains(cost.Tag))
                                {
                                    Logger.LogDebug($" {structitem.ComponentDefID} {cost.Tag} mods tp:{cost.StructureTPCost:0.00} cb:{cost.StructureCBCost:0.00}");

                                    tpmod *= cost.StructureTPCost;
                                    cbmod *= cost.StructureCBCost;
                                }

                            }
                        break;
                    }
                }
                if (is_repaired)
                {
                    cbmod = __instance.Constants.MechLab.ZeroStructureCBillModifier;
                    tpmod = __instance.Constants.MechLab.ZeroStructureTechPointModifier;
                }



                int techCost = Mathf.CeilToInt((__instance.Constants.MechLab.StructureRepairTechPoints * (float)structureCount * tpmod) * mechTonnageModifier);
                int cbillCost = Mathf.CeilToInt((float)((__instance.Constants.MechLab.StructureRepairCost * structureCount) * cbmod) * mechTonnageModifier);

                Logger.LogDebug($" tpmod: {tpmod:0.000}");
                Logger.LogDebug($" cbmod: {cbmod:0.000}");
                Logger.LogDebug(" techCost: " + techCost);
                Logger.LogDebug(" cBill cost: " + cbillCost);
                Logger.LogDebug("***************************************");

                __result = new WorkOrderEntry_RepairMechStructure(id, string.Format("Repair 'Mech - {0}", location.ToString()), mechSimGameUID, techCost, location, structureCount, cbillCost, string.Empty);

            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }
    }
}