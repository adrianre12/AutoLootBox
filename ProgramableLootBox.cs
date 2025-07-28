using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.ObjectBuilders.Private;
using VRage.Utils;

namespace ProgramableLootBox
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_TerminalBlock), false, new[] { "AutoFreight1" })]
    internal class ProgramableLootBox : MyGameLogicComponent
    {
        private IMyCubeBlock block;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            MyLog.Default.WriteLine("BoxInit...");

            if (!MyAPIGateway.Session.IsServer)
                return;

            block = Entity as IMyCubeBlock;

            NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public override void UpdateAfterSimulation100()
        {
            Log.Msg($"Tick {block.CubeGrid.DisplayName}");

            IMyInventory inv = block.GetInventory();
            MyDefinitionId id = new MyDefinitionId(typeof(MyObjectBuilder_Datapad), "Datapad");
            var item = inv.FindItem(id);
            if (item != null)
            {
                var datapad = item.Content as MyObjectBuilder_Datapad;
                Log.Msg($"Datapad found {datapad.Name} {datapad.Data} {datapad.GetId().ToString()}");
                
            }
            else
            {

                MyObjectBuilder_Datapad datapadObj = MyObjectBuilderSerializer.CreateNewObject(id) as MyObjectBuilder_Datapad;
                datapadObj.Name = "A Name";
                datapadObj.Data = "Some Data";

                inv.AddItems(1, datapadObj);
            }
        }
    }
}
