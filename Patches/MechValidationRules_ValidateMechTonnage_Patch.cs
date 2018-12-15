using System;
using System.Collections.Generic;
using BattleTech;
using Harmony;

namespace ArmorRepair
{
    [HarmonyPatch(typeof(MechValidationRules), "ValidateMechTonnage")]
    public static class MechValidationRules_ValidateMechTonnage_Patch
    {
        public static void Postfix(MechDef mechDef, ref Dictionary<MechValidationType, List<string>> errorMessages)
        {
            try
            {
                for (int i = 0; i < mechDef.Inventory.Length; i++)
                {
                    MechComponentRef mechComponentRef = mechDef.Inventory[i];
                    if (mechComponentRef.DamageLevel == ComponentDamageLevel.Destroyed)
                    {
                        Logger.LogDebug("Flagging destroyed component warning: " + mechDef.Name);
                        errorMessages[MechValidationType.Underweight].Add(string.Format("DESTROYED COMPONENT: 'Mech has destroyed components", new object[0]));
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }
    }
}