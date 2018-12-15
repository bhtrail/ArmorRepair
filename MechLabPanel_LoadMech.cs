using BattleTech;
using BattleTech.UI;
using Harmony;

namespace ArmorRepair
{
    [HarmonyPatch(typeof(MechLabPanel), "LoadMech")]
    public static class MechLabPanel_LoadMech
    {
        public static MechDef CurrentMech = null;

        [HarmonyPrefix]
        public static void SetMech(MechDef newMechDef)
        {
            CurrentMech = newMechDef;
        }
    }
}