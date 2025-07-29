using EmptyKeys.UserInterface.Generated.AtmBlockView_Bindings;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game;
using Sandbox.Game.Gui;
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
        const int DefaultShowInTerminalPeriod = 3600; //5 * 3600; //ticks
        const int DefaultRefreshPeriod = 2; //20; //mins

        public static Random GlobalRandom = new Random();

        private IMyTerminalBlock block;
        private bool once;
        private int refreshPeriod = DefaultRefreshPeriod * 3600; //ticks
        private int showInTerminalExpiary = int.MaxValue; //ticks
        private int refreshAfterFrame;
        private bool programming;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            Log.Msg("Init...");

            if (!MyAPIGateway.Session.IsServer)
                return;

            block = Entity as IMyTerminalBlock;

            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();

            block.ShowInTerminal = false;
            block.ShowInInventory = false;
            block.ShowInToolbarConfig = false;
            block.ShowOnHUD = false;

            LoadInventory();

            refreshAfterFrame = (int)(MyAPIGateway.Session.GameplayFrameCounter + refreshPeriod * (GlobalRandom.NextDouble()) + 1);
            NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public override void UpdateAfterSimulation100()
        {
            var currentFrame = MyAPIGateway.Session.GameplayFrameCounter;
            Log.Msg($"Tick {block.CubeGrid.DisplayName} frame={currentFrame}, termExpiry={showInTerminalExpiary} refresh={refreshAfterFrame}");

            if (block.ShowInTerminal)
            {
                Log.Msg("Programming");
                if (!programming)
                {
                    programming = true;
                    LoadInventory();

                    showInTerminalExpiary = currentFrame + DefaultShowInTerminalPeriod;
                    return;
                }
                if (showInTerminalExpiary > currentFrame)
                    return;
            }

            if (programming)
            {
                Log.Msg("End Programming");

                programming = false;
                block.ShowInTerminal = false;
                refreshAfterFrame = currentFrame + refreshPeriod;

                StoreInventory();
            }

            if (currentFrame < refreshAfterFrame)
                return;

            refreshAfterFrame = currentFrame + refreshPeriod;
            Log.Msg("Refresh");

            LoadInventory();
        }
        private void LoadInventory()
        {
            Log.Msg("Loading Inventory");
            MyInventory myinv = (MyInventory)block.GetInventory();

            var invbuilder = MyAPIGateway.Utilities.SerializeFromXML<MyObjectBuilder_Inventory>(block.CustomData);
            myinv.Clear();
            myinv.Init(invbuilder);
        }
        private void StoreInventory()
        {
            Log.Msg("Storing Inventory");
            MyInventory myinv = (MyInventory)block.GetInventory();
            block.CustomData = MyAPIGateway.Utilities.SerializeToXML(myinv.GetObjectBuilder());
        }

    }
}
