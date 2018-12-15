using System;
using System.Linq;
using BattleTech;
using BattleTech.UI;
using Harmony;
using UnityEngine;

namespace ArmorRepair
{
    [HarmonyPatch(typeof(SimGameState), "ResolveCompleteContract")]
    public static class SimGameState_ResolveCompleteContract_Patch
    {

        // Just for safety, ensure the temp queue in this mod is completely clear before we run any processing
        public static bool Prefix(SimGameState __instance)
        {
            try
            {
                Globals.tempMechLabQueue.Clear();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                return true;
            }

            return true; // Allow original method to fire
        }

        // Run after completion of contracts and queue up any orders in the temp queue into the game's Mech Lab queue 
        public static void Postfix(SimGameState __instance)
        {
            try
            {
                // If there are any work orders in the temporary queue, prompt the player
                if (Globals.tempMechLabQueue.Count > 0)
                {
                    Logger.LogDebug("Processing temp Mech Lab queue orders.");

                    int cbills = 0;
                    int techCost = 0;
                    int mechRepairCount = 0;
                    int skipMechCount = 0;
                    string mechRepairCountDisplayed = String.Empty;
                    string skipMechCountDisplayed = String.Empty;
                    string skipMechMessage = String.Empty;
                    string finalMessage = String.Empty;

                    // If player has disabled auto repairing mechs with destroyed components, check for them and remove them from the temp queue before continuing
                    if (!ArmorRepair.ModSettings.AutoRepairMechsWithDestroyedComponents)
                    {
                        for (int index = 0; index < Globals.tempMechLabQueue.Count; index++)
                        {
                            WorkOrderEntry_MechLab order = Globals.tempMechLabQueue[index];

                            Logger.LogDebug("Checking for destroyed components.");
                            bool destroyedComponents = false;
                            MechDef mech = __instance.GetMechByID(order.MechID);
                            destroyedComponents = Helpers.CheckDestroyedComponents(mech);

                            if (destroyedComponents)
                            {
                                // Remove this work order from the temp mech lab queue if the mech has destroyed components and move to next iteration
                                Logger.LogDebug("Removing " + mech.Name + " order from temp queue due to destroyed components and mod settings.");
                                Globals.tempMechLabQueue.Remove(order);
                                destroyedComponents = false;
                                skipMechCount++;
                                index++;

                            }
                        }
                    }

                    Logger.LogDebug("Temp Queue has " + Globals.tempMechLabQueue.Count + " entries.");

                    // Calculate summary of total repair costs from the temp work order queue
                    for (int index = 0; index < Globals.tempMechLabQueue.Count; index++)
                    {
                        WorkOrderEntry_MechLab order = Globals.tempMechLabQueue[index];
                        MechDef mech = __instance.GetMechByID(order.MechID);
                        Logger.LogDebug("Adding " + mech.Name + " to RepairCount.");
                        cbills += order.GetCBillCost();
                        techCost += order.GetCost();
                        mechRepairCount++;
                    }

                    mechRepairCount = Mathf.Clamp(mechRepairCount, 0, 4);
                    Logger.LogDebug("Temp Queue has " + Globals.tempMechLabQueue.Count + " work order entries.");

                    // If Yang's Auto Repair prompt is enabled, build a message prompt dialog for the player
                    if (ArmorRepair.ModSettings.EnableAutoRepairPrompt)
                    {

                        // Calculate a friendly techCost of the work order in days, based on number of current mechtechs in the player's game.
                        if (techCost != 0 && __instance.MechTechSkill != 0)
                        {
                            techCost = Mathf.CeilToInt((float)techCost / (float)__instance.MechTechSkill);
                        }
                        else
                        {
                            techCost = 1; // Safety in case of weird div/0
                        }

                        // Generate a quick friendly description of how many mechs were damaged in battle
                        switch (mechRepairCount)
                        {
                            case 0: { Logger.LogDebug("mechRepairCount was 0."); break; }
                            case 1: { mechRepairCountDisplayed = "one of our 'Mechs was"; break; }
                            case 2: { mechRepairCountDisplayed = "a couple of the 'Mechs were"; break; }
                            case 3: { mechRepairCountDisplayed = "three of our 'Mechs were"; break; }
                            case 4: { mechRepairCountDisplayed = "our whole lance was"; break; }
                        }
                        // Generate a friendly description of how many mechs were damaged but had components destroyed
                        switch (skipMechCount)
                        {
                            case 0: { Logger.LogDebug("skipMechCount was 0."); break; }
                            case 1: { skipMechCountDisplayed = "one of the 'Mechs is damaged but has"; break; }
                            case 2: { skipMechCountDisplayed = "two of the 'Mechs are damaged but have"; break; }
                            case 3: { skipMechCountDisplayed = "three of the 'Mechs are damaged but have "; break; }
                            case 4: { skipMechCountDisplayed = "the whole lance is damaged but has"; break; }
                        }

                        // Check if there are any mechs to process
                        if (mechRepairCount > 0 || skipMechCount > 0)
                        {
                            Logger.LogDebug("mechRepairCount is " + mechRepairCount + " skipMechCount is " + skipMechCount);

                            // Setup the notification for mechs with damaged components that we might want to skip
                            if (skipMechCount > 0 && mechRepairCount == 0)
                            {
                                skipMechMessage = String.Format("{0} destroyed components. I'll leave the repairs for you to review.", skipMechCountDisplayed);
                            }
                            else
                            {
                                skipMechMessage = String.Format("{0} destroyed components, so I'll leave those repairs to you.", skipMechCountDisplayed);
                            }

                            Logger.LogDebug("Firing Yang's UI notification.");
                            SimGameInterruptManager notificationQueue = __instance.GetInterruptQueue();

                            // If all of the mechs needing repairs have damaged components and should be skipped from auto-repair, change the message notification structure to make more sense (e.g. just have an OK button)
                            if (skipMechCount > 0 && mechRepairCount == 0)
                            {
                                finalMessage = String.Format(
                                    "Boss, {0} \n\n",
                                    skipMechMessage
                                );

                                // Queue Notification
                                notificationQueue.QueuePauseNotification(
                                    "'Mech Repairs Needed",
                                    finalMessage,
                                    __instance.GetCrewPortrait(SimGameCrew.Crew_Yang),
                                    string.Empty,
                                    delegate
                                    {
                                        Logger.LogDebug("[PROMPT] All damaged mechs had destroyed components and won't be queued for repair.");
                                    },
                                    "OK"
                                );
                            }
                            else
                            {
                                if (skipMechCount > 0)
                                {
                                    finalMessage = String.Format(
                                        "Boss, {0} damaged. It'll cost <color=#DE6729>{1}{2:n0}</color> and {3} days for these repairs. Want my crew to get started?\n\nAlso, {4}\n\n",
                                        mechRepairCountDisplayed,
                                        '¢', cbills.ToString(),
                                        techCost.ToString(),
                                        skipMechMessage
                                    );
                                }
                                else
                                {
                                    finalMessage = String.Format(
                                        "Boss, {0} damaged on the last engagement. It'll cost <color=#DE6729>{1}{2:n0}</color> and {3} days for the repairs.\n\nWant my crew to get started?",
                                        mechRepairCountDisplayed,
                                        '¢', cbills.ToString(),
                                        techCost.ToString()
                                    );
                                }


                                // Queue up Yang's notification
                                notificationQueue.QueuePauseNotification(
                                    "'Mech Repairs Needed",
                                    finalMessage,
                                    __instance.GetCrewPortrait(SimGameCrew.Crew_Yang),
                                    string.Empty,
                                    delegate
                                    {
                                        Logger.LogDebug("[PROMPT] Moving work orders from temp queue to Mech Lab queue: " + Globals.tempMechLabQueue.Count + " work orders");
                                        foreach (WorkOrderEntry_MechLab workOrder in Globals.tempMechLabQueue.ToList())
                                        {
                                            Logger.LogInfo("[PROMPT] Moving work order from temp queue to Mech Lab queue: " + workOrder.Description + " - " + workOrder.GetCBillCost());
                                            Helpers.SubmitWorkOrder(__instance, workOrder);
                                            Globals.tempMechLabQueue.Remove(workOrder);
                                        }
                                    },
                                    "Yes",
                                    delegate
                                    {
                                        Logger.LogInfo("[PROMPT] Discarding work orders from temp queue: " + Globals.tempMechLabQueue.Count + " work orders");
                                        foreach (WorkOrderEntry_MechLab workOrder in Globals.tempMechLabQueue.ToList())
                                        {
                                            Logger.LogInfo("[PROMPT] Discarding work order from temp queue: " + workOrder.Description + " - " + workOrder.GetCBillCost());
                                            Globals.tempMechLabQueue.Remove(workOrder);
                                        }
                                    },
                                    "No"
                                );
                            }
                        }
                    }
                    else // If Auto Repair prompt is not enabled, just proceed with queuing the remaining temp queue work orders and don't notify the player
                    {
                        foreach (WorkOrderEntry_MechLab workOrder in Globals.tempMechLabQueue.ToList())
                        {
                            Logger.LogInfo("[AUTO] Moving work order from temp queue to Mech Lab queue: " + workOrder.Description + " - " + workOrder.GetCBillCost());
                            Helpers.SubmitWorkOrder(__instance, workOrder);
                            Globals.tempMechLabQueue.Remove(workOrder);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Globals.tempMechLabQueue.Clear();
                Logger.LogError(ex);
            }
        }
    }
}