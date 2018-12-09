namespace ArmorRepair
{
    public struct RepairCost
    {
        public string Tag;
        public float ArmorTPCost;
        public float ArmorCBCost;
        public float StructureTPCost;
        public float StructureCBCost;

    }

    public class Settings
    {
        #region logging
        public bool debug = false;
        public bool Debug => debug;
        #endregion logging

        #region game
        public bool enableStructureRepair = true;
        public bool EnableStructureRepair => enableStructureRepair;
        public bool scaleStructureCostByTonnage = true;
        public bool ScaleStructureCostByTonnage => scaleStructureCostByTonnage;
        public bool scaleArmorCostByTonnage = true;
        public bool ScaleArmorCostByTonnage => scaleArmorCostByTonnage;
        public bool enableAutoRepairPrompt = true;
        public bool EnableAutoRepairPrompt => enableAutoRepairPrompt;
        public bool autoRepairMechsWithDestroyedComponents = true;
        public bool AutoRepairMechsWithDestroyedComponents => autoRepairMechsWithDestroyedComponents;

        public string ArmorCategory = "Armor";
        public string StructureCategory = "Sttructure";


        public RepairCost[] RepairCostByTag;

        public void Complete()
        {
            if (RepairCostByTag != null)
                for(int i =0;i< RepairCostByTag.Length; i++)
                {
                    if (RepairCostByTag[i].ArmorCBCost <= 0f)
                        RepairCostByTag[i].ArmorCBCost = 1;
                    if (RepairCostByTag[i].ArmorTPCost <= 0f)
                        RepairCostByTag[i].ArmorTPCost = 1;
                    if (RepairCostByTag[i].StructureTPCost <= 0f)
                        RepairCostByTag[i].StructureTPCost = 1;
                    if (RepairCostByTag[i].StructureCBCost <= 0f)
                        RepairCostByTag[i].StructureCBCost = 1;

                }
        }

        #endregion game
    }
}
