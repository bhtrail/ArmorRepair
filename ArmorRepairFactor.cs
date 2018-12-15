using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CustomComponents;

namespace ArmorRepair
{
    [CustomComponent("ArmorRepair")]
    public class ArmorRepairFactor : SimpleCustomComponent, IAfterLoad
    {
        public float ArmorTPCost { get; set; }
        public float ArmorCBCost { get; set; }


        public void OnLoaded(Dictionary<string, object> values)
        {
            if (ArmorCBCost <= 0)
                ArmorCBCost = 1;
            if (ArmorTPCost <= 0)
                ArmorTPCost = 1;
        }
    }

    [CustomComponent("StructureRepair")]
    public class StructureRepairFactor : SimpleCustomComponent, IAfterLoad
    {
        public float StructureTPCost { get; set; }
        public float StructureCBCost { get; set; }


        public void OnLoaded(Dictionary<string, object> values)
        {
            if (StructureTPCost <= 0)
                StructureTPCost = 1;
            if (StructureCBCost <= 0)
                StructureCBCost = 1;
        }
    }
}
