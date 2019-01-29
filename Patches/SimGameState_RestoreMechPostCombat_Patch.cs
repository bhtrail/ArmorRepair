using System;
using Harmony;
using BattleTech;
using UnityEngine;
using System.Linq;

namespace ArmorRepair
{

    // Ensure our temp Mech Lab queue is always cleared before processing another mission/contract completion


    /* Prefix on RestoreMechPostCombat to create a new modify armor work order from the armor loss difference of each mech at the end of combat
     * 
     *  If successful we prevent firing the original method as this is required to stop mech armor being blindly reset at the end of a contract.
     */
    [HarmonyPatch(typeof(SimGameState), "RestoreMechPostCombat")]
    public static class SimGameState_RestoreMechPostCombat_Patch
    {

        public static bool Prefix(SimGameState __instance, MechDef mech)
        {
            try
            {
                // Start of analysing a mech for armor repair
                Logger.LogInfo("Analysing Mech: " + mech.Name);
                Logger.LogInfo("============================================");

                // Base generic MechLab WO for a mech that requires armor or structure repair - each individual locational subentry WO has to be added to this base WO later
                WorkOrderEntry_MechLab newMechLabWorkOrder = null;

                /* STRUCTURE DAMAGE CHECKS
                 * ------------------------
                 * Check if the given mech needs any structure repaired and that EnableStructureRepair is true in the mod settings
                 * 
                 */
                if (ArmorRepair.ModSettings.EnableStructureRepair)
                {
                    if (Helpers.CheckStructureDamage(mech))
                    {
                        Logger.LogDebug("SimGameConstant: StructureRepairTechPoints: " + __instance.Constants.MechLab.StructureRepairTechPoints);
                        Logger.LogDebug("SimGameConstant: StructureRepairCost: " + __instance.Constants.MechLab.StructureRepairCost);

                        // Loop over the ChassisLocations for repair in their highest -> lowest priority order from the dictionary defined in Helpers
                        for (int index = 0; index < Globals.repairPriorities.Count; index++)
                        {
                            // Set current looped ChassisLocation
                            ChassisLocations thisLoc = Globals.repairPriorities.ElementAt(index).Value;
                            // Get current mech's loadout definition from the looped chassis location
                            LocationLoadoutDef thisLocLoadout = mech.GetLocationLoadoutDef(thisLoc);
                            // Friendly name for this location
                            string thisLocName = thisLoc.ToString();

                            Logger.LogDebug("Analysing location: " + thisLocName);

                            // Check if a new base MechLab order needs to be created or not
                            if (newMechLabWorkOrder == null)
                            {
                                // Create new base work order of the generic MechLab type if it doesn't already exist
                                newMechLabWorkOrder = Helpers.CreateBaseMechLabOrder(__instance, mech);
                            }

                            float currentStructure = thisLocLoadout.CurrentInternalStructure;
                            float definedStructure = mech.GetChassisLocationDef(thisLoc).InternalStructure;

                            // Only create work orders for repairing structure if this location has taken damage in combat
                            if (currentStructure != definedStructure)
                            {
                                // Work out difference of structure lost for each location - default to 0
                                int structureDifference = 0;
                                structureDifference = (int)Mathf.Abs(currentStructure - definedStructure);

                                Logger.LogInfo("Total structure difference for " + thisLocName + " is " + structureDifference);

                                Logger.LogInfo("Creating MechRepair work order entry for " + thisLocName);
                                Logger.LogDebug("Calling CreateMechRepairWorkOrder with params - GUID: " +
                                    mech.GUID.ToString() +
                                    " | Location: " + thisLocName +
                                    " | structureDifference: " + structureDifference
                                );

                                WorkOrderEntry_RepairMechStructure newRepairWorkOrder = __instance.CreateMechRepairWorkOrder(
                                    mech.GUID,
                                    thisLocLoadout.Location,
                                    structureDifference
                                );

                                Logger.LogDebug("Adding WO subentry to repair missing " + thisLocName + " structure.");
                                newMechLabWorkOrder.AddSubEntry(newRepairWorkOrder);

                            }
                            else
                            {
                                Logger.LogDebug("Structure repair not required for: " + thisLocName);
                            }
                        }
                    }
                }

                /* COMPONENT DAMAGE CHECKS
                 * -----------------------
                 * Check if the given mech needs any critted components repaired
                 * 
                 * NOTE: Not yet working. Repair components are added to work order but not actually repaired after WO completes. Noticed there is another queue involved on SGS.WorkOrderComponents we need to debug.
                 * Currently throws "SimGameState [ERROR] ML_RepairComponent MechBay - RepairComponent - SGRef_490 had an invalid mechComponentID Ammo_AmmunitionBox_Generic_AC5, skipping" in SimGame logger on WO completion.
                if (Helpers.CheckDamagedComponents(mech))
                {
                    for (int index = 0; index < mech.Inventory.Length; index++)
                    {
                        MechComponentRef mechComponentRef = mech.Inventory[index];

                        // Penalized = Critted Component
                        if (mechComponentRef.DamageLevel == ComponentDamageLevel.Penalized)
                        {
                            // Check if a new base MechLab order needs to be created or not
                            if (newMechLabWorkOrder == null)
                            {
                                // Create new base work order of the generic MechLab type if it doesn't already exist
                                newMechLabWorkOrder = Helpers.CreateBaseMechLabOrder(__instance, mech);
                            }

                            // Create a new component repair work order for this component
                            Logger.LogInfo("Creating Component Repair work order entry for " + mechComponentRef.ComponentDefID);
                            WorkOrderEntry_RepairComponent newComponentRepairOrder = __instance.CreateComponentRepairWorkOrder(mechComponentRef, false);

                            // Attach as a child to the base Mech Lab order.
                            Logger.LogDebug("Adding WO subentry to repair component " + mechComponentRef.ComponentDefID);
                            newMechLabWorkOrder.AddSubEntry(newComponentRepairOrder);
                        }
                    }
                }
                */


                /* ARMOR DAMAGE CHECKS
                 * -------------------
                 * Check if the given mech needs any structure repaired
                 * 
                 */
                if (Helpers.CheckArmorDamage(mech))
                {

                    Logger.LogDebug("SimGameConstant: ArmorInstallTechPoints: " + __instance.Constants.MechLab.ArmorInstallTechPoints);
                    Logger.LogDebug("SimGameConstant: ArmorInstallCost: " + __instance.Constants.MechLab.ArmorInstallCost);

                    // Loop over the ChassisLocations for repair in their highest -> lowest priority order from the dictionary defined in Helpers
                    for (int index = 0; index < Globals.repairPriorities.Count; index++)
                    {
                        // Set current ChassisLocation
                        ChassisLocations thisLoc = Globals.repairPriorities.ElementAt(index).Value;
                        // Get current mech's loadout from the looped chassis location
                        LocationLoadoutDef thisLocLoadout = mech.GetLocationLoadoutDef(thisLoc);
                        // Friendly name for this location
                        string thisLocName = thisLoc.ToString();

                        Logger.LogDebug("Analysing location: " + thisLocName);

                        // Check if a new base MechLab order needs to be created
                        if (newMechLabWorkOrder == null)
                        {
                            // Create new base work order of the generic MechLab type if it doesn't already exist
                            newMechLabWorkOrder = Helpers.CreateBaseMechLabOrder(__instance, mech);
                        }

                        // Work out difference of armor lost for each location - default to 0
                        int armorDifference = 0;

                        // Consider rear armour in difference calculation if this is a RT, CT or LT
                        if (thisLocLoadout == mech.CenterTorso || thisLocLoadout == mech.RightTorso || thisLocLoadout == mech.LeftTorso)
                        {
                            Logger.LogDebug("Location also has rear armor.");
                            armorDifference = (int)Mathf.Abs(thisLocLoadout.CurrentArmor - thisLocLoadout.AssignedArmor) + (int)Mathf.Abs(thisLocLoadout.CurrentRearArmor - thisLocLoadout.AssignedRearArmor);
                        }
                        else
                        {
                            armorDifference = (int)Mathf.Abs(thisLocLoadout.CurrentArmor - thisLocLoadout.AssignedArmor);
                        }
                        // Only create work orders for repairing armor if this location has taken armor damage in combat
                        if (armorDifference != 0)
                        {
                            Logger.LogInfo("Total armor difference for " + thisLocName + " is " + armorDifference);
                            Logger.LogInfo("Creating ModifyMechArmor work order entry for " + thisLocName);
                            Logger.LogDebug("Calling ModifyMechArmor WO with params - GUID: " +
                                mech.GUID.ToString() +
                                " | Location: " + thisLocName +
                                " | armorDifference: " + armorDifference +
                                " | AssignedArmor: " + thisLocLoadout.AssignedArmor +
                                " | AssignedRearArmor: " + thisLocLoadout.AssignedRearArmor
                            );
                            WorkOrderEntry_ModifyMechArmor newArmorWorkOrder = __instance.CreateMechArmorModifyWorkOrder(
                                mech.GUID,
                                thisLocLoadout.Location,
                                armorDifference,
                                (int)(thisLocLoadout.AssignedArmor),
                                (int)(thisLocLoadout.AssignedRearArmor)
                            );

                            /* IMPORTANT!
                                * This has turned out to be required as CurrentArmor appears to be reset to AssignedArmor from somewhere unknown in the game after battle
                                * So if we don't reset AssignedArmor now, player can cancel the work order to get a free armor reset anyway!
                                * 
                                * NOTE: CeilToInt (or similar rounding) is vital to prevent fractions of armor from causing Mech tonnage / validation issues for the player
                                */
                            Logger.LogDebug("Forcing assignment of Assigned Armor: " + thisLocLoadout.AssignedArmor + " To Current Armor (CeilToInt): " + Mathf.CeilToInt(thisLocLoadout.CurrentArmor));
                            thisLocLoadout.AssignedArmor = Mathf.CeilToInt(thisLocLoadout.CurrentArmor);
                            thisLocLoadout.AssignedRearArmor = Mathf.CeilToInt(thisLocLoadout.CurrentRearArmor);

                            Logger.LogInfo("Adding WO subentry to install missing " + thisLocName + " armor.");
                            newMechLabWorkOrder.AddSubEntry(newArmorWorkOrder);

                        }
                        else
                        {
                            Logger.LogDebug("Armor repair not required for: " + thisLocName);
                        }

                    }
                }


                /* WORK ORDER SUBMISSION
                 * ---------------------
                 * Submit the complete work order for the mech, which will include any repair armor / structure subentries for each location
                 * 
                 */
                if (newMechLabWorkOrder != null)
                {
                    if (newMechLabWorkOrder.SubEntryCount > 0)
                    {
                        // Submit work order to our temporary queue for internal processing
                        Helpers.SubmitTempWorkOrder(
                            __instance,
                            newMechLabWorkOrder,
                            mech
                        );
                    }
                    else
                    {
                        Logger.LogInfo(mech.Name + " did not require repairs.");
                    }
                }

                // Lifted from original RestoreMechPostCombat method - resets any non-functional mech components back to functional
                foreach (MechComponentRef mechComponentRef in mech.Inventory)
                {
                    if (mechComponentRef.DamageLevel == ComponentDamageLevel.NonFunctional)
                    {
                        Logger.LogDebug("Resetting non-functional mech component: " + mechComponentRef.ToString());
                        mechComponentRef.DamageLevel = ComponentDamageLevel.Functional;
                    }
                }

                return false; // Finally, prevent firing the original method
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                return true; // Allow original method to fire if there is an exception
            }
        }
    }



    /* Patch CreateMechArmorModifyWorkOrder so we can apply tonnage modifiers to armor work orders in the game
     *  The intent of this is to make light mechs cheaper to repair armor on compared with heavy/assault mechs. It can be disabled in the mod.json settings.
     */

    /* Patch CreateMechRepairWorkOrder so we can apply tonnage modifiers to structure repair work orders in the game
     *  The intent of this is to make light mechs cheaper to repair structure on compared with heavy/assault mechs. It can be disabled in the mod.json settings.
     *  
     */


    /* [FIX] Patch WorkOrderEntry_ModifyMechArmor so we can apply our armor tech cost modifier 
     *  
     *  techCostModifier is used to reduce the overall tech cost for armor work orders
     *      PROBLEM: 
     *      HBS exposed ArmorInstallTechCost in SimGameConstants but it's an int, rather than a float.
     *      
     *      By default this means when we set it to even the lowest possible int value (1), armor install tech costs are still calculated at unitsLost * ArmorInstallTechCost = techCost. 
     *      This results in even the lowest integer (1) setting causing armor units to be over 3x more expensive than the default cost of structure units! It's too much even late game with lots of mechtechs.
     *      
     *      WORKAROUND:
     *      The workaround for this is to modify the game's calculated techCost for armor Work Oroders by * 0.01f, reducing it by a factor of 100 as a base.
     *      This effectively turns the SimGameConstants integer into a usable float without having to modify shit tons of references or doing anything too messy. 
     *      
     *      The player can then modify the ultimate tech cost for armor by setting the SimGameConstants.ArmorInstallTechCost integer as normal. 
     *      An ArmorInstallTechCost setting of 10 in SimGameConstants will now be equivalent to a StructureRepairTechCost of 0.1 (vanilla default for structure units is 0.3 for balancing illustration)
    */


    /* [FIX] Patch into ML_RepairMech to prevent structure repair work orders from resetting armor
     *  HBS hardcoded structure repairs to reset armor because reasons
     *  
     *  This must prevent ML_RepairMech from firing as it's the only way we can stop blind armor resets when mech structure is repaired
     */

    /* [FIX] UI WARNING ON DESTROYED COMPONENTS
     * Attempting to flag up warning in Mech Bay / Mech Lab when a mech has destroyed components.
     * 
     * This isn't a problem in vanilla, but now we are auto repairing armour and structure and can't auto repair components easily (e.g. they might not be in stock)
     * we now need to flag up the player that there is a problem with the mech when it has destroyed components.
     */


    /* [FIX] SUPPRESS YANG REPAIRS WARNING
     * If the player has enabled Yang's notification about mech repairs in this mod, suppress the default in-game warning from spamming the player about repairs twice
     */


    /* TESTING / DEBUGGING
     * Testing and debugging patches
     * 
     */

    // Just to debug Structure WO final costs


    /* ML_ModifyArmor executes when a work order item for modifying armor is completed, and physically sets the desired amor on the mech. 
     *  It's not needed at this time 
    [HarmonyPatch(typeof(SimGameState), "ML_ModifyArmor")]
    public static class SimGameState_ML_ModifyArmor_Patch
    {
        private static bool Prefix(SimGameState __instance, WorkOrderEntry_ModifyMechArmor order)
        {
            MechDef mechByID = __instance.GetMechByID(order.MechLabParent.MechID);
            LocationLoadoutDef locationLoadoutDef = mechByID.GetLocationLoadoutDef(order.Location);

            Logger.LogDebug("ML_ModifyArmor was called with params: ");
            Logger.LogDebug("************************************** ");
            Logger.LogDebug("mechByID: " + mechByID.Description.Name);
            Logger.LogDebug("CurrentArmor: " + locationLoadoutDef.CurrentArmor + " = Desired: " + (float)order.DesiredFrontArmor);
            Logger.LogDebug("CurrentRearArmor: " + locationLoadoutDef.CurrentRearArmor + " = Desired: " + (float)order.DesiredRearArmor);
            Logger.LogDebug("AssignedArmor: " + locationLoadoutDef.AssignedArmor + " = Desired: " + (float)order.DesiredFrontArmor);
            Logger.LogDebug("AssignedRearArmor: " + locationLoadoutDef.AssignedRearArmor + " = Desired: " + (float)order.DesiredRearArmor);
            Logger.LogDebug("************************************** ");

            return true;
        }
    }*/

}
