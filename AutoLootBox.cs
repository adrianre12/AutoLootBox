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
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.ObjectBuilders.Private;
using VRage.Utils;
using VRageMath;

namespace AutoLootBox
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_TerminalBlock), false, new[] { "AutoFreight1", "AutoFreight2", "AutoFreight3" })]
    internal class AutoLootBox : MyGameLogicComponent
    {
        const int DefaultShowInTerminalPeriod = 5 * 3600; //ticks
        const int DefaultRefreshPeriod = 20; //mins

        public static Random GlobalRandom = new Random();

        private IMyTerminalBlock block;
        private int refreshPeriod = DefaultRefreshPeriod; //ticks
        private int showInTerminalExpiary = int.MaxValue; //ticks
        private int refreshAfterFrame;
        private bool programming;
        private MyIni config = new MyIni();

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

            LoadConfigFromCD();

            refreshAfterFrame = (int)(MyAPIGateway.Session.GameplayFrameCounter + refreshPeriod * 1800 * (GlobalRandom.NextDouble() + 1));
            NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public override void UpdateAfterSimulation100()
        {
            var currentFrame = MyAPIGateway.Session.GameplayFrameCounter;
            //Log.Msg($"Tick {block.CubeGrid.DisplayName} frame={currentFrame}, termExpiry={showInTerminalExpiary} refresh={refreshAfterFrame}");

            if (block.ShowInTerminal)
            {
                //Log.Msg("Programming");
                if (!programming)
                {
                    programming = true;
                    LoadConfigFromCD();

                    showInTerminalExpiary = currentFrame + DefaultShowInTerminalPeriod;
                    return;
                }
                if (showInTerminalExpiary > currentFrame)
                    return;
            }

            if (programming)
            {
                //Log.Msg("End Programming");
                programming = false;
                block.ShowInTerminal = false;
                refreshAfterFrame = currentFrame + refreshPeriod * 3600;

                SaveConfigToCD();
            }

            if (currentFrame < refreshAfterFrame)
                return;

            //Log.Msg($"Refresh elapsed={(currentFrame - refreshAfterFrame)/60} frame={currentFrame}, refresh={refreshAfterFrame}");
            if (currentFrame - refreshAfterFrame > 1800)
            {
                refreshAfterFrame = (int)(currentFrame + refreshPeriod * 1800 * (GlobalRandom.NextDouble() + 1));
            }
            else
            {
                refreshAfterFrame = currentFrame + refreshPeriod * 3600;
            }

            LoadConfigFromCD();
        }

        private void LoadConfigFromCD()
        {
            if (!ParseConfigFromCD())
            {
                Log.Msg("Error in CD, creating a new config.");
                refreshPeriod = DefaultRefreshPeriod;
                SaveConfigToCD();
            }
        }

        private void SaveConfigToCD()
        {
            Log.Msg("Saving config to CD.");

            config.Clear();
            var sb = new StringBuilder();
            sb.AppendLine("Enable ShowInTerminal while loading items into the inventory");
            sb.AppendLine("Disable ShowInTerminal to save inventory");
            sb.AppendLine("ShowInTerminal is auto-disabled after 5 mins.");

            config.AddSection("LootBox");
            config.SetSectionComment("LootBox", sb.ToString());

            config.Set("LootBox", "RefreshPeriodMins", refreshPeriod);

            MyInventory myinv = (MyInventory)block.GetInventory();
            config.Set("LootBox", "Inventory", Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(myinv.GetObjectBuilder())));

            config.Invalidate();
            block.CustomData = config.ToString();
        }

        private bool ParseConfigFromCD()
        {
            //Log.Msg("ParseConfigFromCD");
            if (config.TryParse(block.CustomData))
            {
                if (!config.ContainsSection("LootBox"))
                    return false;

                if (!config.Get("LootBox", "RefreshPeriodMins").TryGetInt32(out refreshPeriod))
                    return false;

                string tmp;
                if (!config.Get("LootBox", "Inventory").TryGetString(out tmp))
                    return false;

                MyInventory myinv = (MyInventory)block.GetInventory();
                try
                {
                    var invbuilder = MyAPIGateway.Utilities.SerializeFromBinary<MyObjectBuilder_Inventory>(Convert.FromBase64String(tmp));
                    myinv.Clear();
                    myinv.Init(invbuilder);
                } catch (Exception e) {
                    Log.Msg($"Failed to parse: {tmp}\n {e}");
                    return false;
                }

                return true;
            }
            Log.Msg("Error: Failed to load config");
            return false;
        }
    }
}
