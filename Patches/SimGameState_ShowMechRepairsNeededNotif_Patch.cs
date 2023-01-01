using BattleTech;
using Harmony;

namespace ArmorRepair
{
    [HarmonyPatch(typeof(SimGameState), "ShowMechRepairsNeededNotif")]
    public static class SimGameState_ShowMechRepairsNeededNotif_Patch
    {
        public static bool Prefx(SimGameState __instance)
        {
            if (ArmorRepair.ModSettings.enableAutoRepairPrompt)
            {
                __instance.CompanyStats.Set("COMPANY_NotificationViewed_BattleMechRepairsNeeded", __instance.DaysPassed);
                return false; // Suppress original method
            }
            else
            {
                return true; // Do nothing if the player isn't using our Yang prompt functionality.
            }

        }
    }
}