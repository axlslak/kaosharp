using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Bootstrap;
using AOSharp.Core;
using AOSharp.Core.UI;
using AOSharp.Core.Inventory;
using AOSharp.Core.Movement;
using AOSharp.Common.GameData;
using AOSharp.Common.GameData.UI;
using AOSharp.Core.GameData;
using AOSharp.Core.UI.Options;
using AOSharp.Core.IPC;
using AOSharp.Common.Unmanaged.Imports;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using TestPlugin.IPCMessages;
using System.Threading;
using AOSharp.Common.Unmanaged.DataTypes;
using Zoltu.IO;
using SmokeLounge.AOtomation.Messaging.Messages;
using SmokeLounge.AOtomation.Messaging.Messages.ChatMessages;
using System.Runtime.InteropServices;
using System.Drawing;
using AOSharp.Common.SharedEventArgs;
using SmokeLounge.AOtomation.Messaging.GameData;
using AOSharp.Common.Unmanaged.Interfaces;
using AOSharp.Common.Helpers;
using AOSharp.Core.GMI;
using System.IdentityModel.Metadata;

namespace TestPlugin
{
    public class Main : AOPluginEntry
    {
        public Settings Settings;
        private IPCChannel _ipcChannel;
        private MovementController mc;
        Window testWindow;
        private Menu _menu;
        private int i = 0;

        [DllImport("DisplaySystem.dll", EntryPoint = "?ClearAnimations@VisualCATMesh_t@@QAEXXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern void ClearAnimations(IntPtr pVisualCatmesh);

        public override void Run(string pluginDir)
        {
            try
            {
                Chat.WriteLine("TestPlugin loaded");

                Chat.WriteLine($"LocalPlayer: {DynelManager.LocalPlayer.Identity}");
                Chat.WriteLine($"   Name: {DynelManager.LocalPlayer.Name}");
                Chat.WriteLine($"   Pos: {DynelManager.LocalPlayer.Position}");
                Chat.WriteLine($"   MoveState: {DynelManager.LocalPlayer.MovementState}");
                Chat.WriteLine($"   Health: {DynelManager.LocalPlayer.GetStat(Stat.Health)}");

                /*
                Chat.WriteLine("Playfield");
                Chat.WriteLine($"   Identity: {Playfield.Identity}");
                Chat.WriteLine($"   Name: {Playfield.Name}");
                Chat.WriteLine($"   AllowsVehicles: {Playfield.AllowsVehicles}");
                Chat.WriteLine($"   IsDungeon: {Playfield.IsDungeon}");
                Chat.WriteLine($"   IsShadowlands: {Playfield.IsShadowlands}");
                Chat.WriteLine($"   NumDynels: {DynelManager.AllDynels.Count}");
                */

                Chat.WriteLine("Team:");
                Chat.WriteLine($"\tIsInTeam: {Team.IsInTeam}");
                Chat.WriteLine($"\tIsLeader: {Team.IsLeader}");
                Chat.WriteLine($"\tIsRaid: {Team.IsRaid}");

                Chat.WriteLine($"Camera:");
                Chat.WriteLine($"IsFirstPerson: {Camera.IsFirstPerson}");
                Chat.WriteLine($"Position: {Camera.Position}");
                Chat.WriteLine($"Rot: {Camera.Rotation}");

                mc = new MovementController(drawPath: true);


                foreach (TeamMember teamMember in Team.Members)
                {
                    Chat.WriteLine($"\t{teamMember.Name} - {teamMember.Identity} - {teamMember.Level} - {teamMember.Profession} - IsLeader:{teamMember.IsLeader} @ Team {teamMember.TeamIndex + 1}");
                }

                Chat.WriteLine("Tests:");

                /*
                Chat.WriteLine($"Base stat: {DynelManager.LocalPlayer.GetStat(Stat.RunSpeed, 1)}");
                Chat.WriteLine($"Modified stat: {DynelManager.LocalPlayer.GetStat(Stat.RunSpeed, 2)}");
                Chat.WriteLine($"No Trickle stat: {DynelManager.LocalPlayer.GetStat(Stat.RunSpeed, 3)}");

                Team.Members.ForEach(x => Chat.WriteLine($"{x.Name} IsLeader: {x.IsLeader}"));

                foreach (Room room in Playfield.Rooms)
                {
                    Chat.WriteLine($"Ptr: {room.Pointer.ToString("X4")}\tName: {room.Name}\tIdx: {room.Instance}\tRot: {room.Rotation}\tPos: {room.Position}\tCenter: {room.Center}\tTemplatePos: {room.TemplatePos}\tYOffset: {room.YOffset}\tNumDoors: {room.NumDoors}\tFloor: {room.Floor}");
                }

                //AO3D export
                foreach (Room room in Playfield.Rooms)
                {
                    Chat.WriteLine($"new RoomInstance(\"{room.Name}\", {room.Floor}, new Vector3{room.Position}, {(int)room.Rotation / 90}, {room.LocalRect.MinX}, {room.LocalRect.MinY}, {room.LocalRect.MaxX}, {room.LocalRect.MaxY}, new Vector3{room.Center}, new Vector3{room.TemplatePos}),");
                }
                */


                /*
                foreach(Spell spell in Spell.List)
                {
                    Chat.WriteLine($"\t{spell.Identity}\t{spell.Name}\t{spell.MeetsUseReqs()}\t{spell.IsReady}");
                }
                */

                Chat.WriteLine("Dynels:");
                foreach (SimpleChar c in DynelManager.Characters)
                {
                    Chat.WriteLine($"\t{c.Name}\t{((int)c.Flags).ToString("X4")}\t{c.Profession}");
                }


                foreach (PerkAction perkAction in PerkAction.List)
                {
                    //Chat.WriteLine($"\t{perk.Identity}\t{perk.Hash}\t{perk.Name}\t{perk.MeetsSelfUseReqs()}\t{perk.GetStat(Stat.AttackDelay)}");
                    Chat.WriteLine($"{perkAction.Name} = 0x{((uint)perkAction.Hash).ToString("X4")},");
                }


                /*
                Chat.WriteLine("Buffs:");
                foreach(Buff buff in DynelManager.LocalPlayer.Buffs)
                {
                    Chat.WriteLine($"\tBuff: {buff.Name}\t{buff.RemainingTime}/{buff.TotalTime}");
                }
                */

                /*
                Perk perk;
                if(Perk.Find(PerkHash.Gore, out perk))
                {
                    if(DynelManager.LocalPlayer.FightingTarget != null)
                        Chat.WriteLine($"Can use perk? {perk.MeetsUseReqs(DynelManager.LocalPlayer.FightingTarget)}");
                }
                */

                /*
                Chat.WriteLine("Pet Identities:");
                foreach(Identity identity in DynelManager.LocalPlayer.Pets)
                {
                    Chat.WriteLine($"\t{identity}");
                }

                Chat.WriteLine("Pet Dynels:");
                foreach(SimpleChar pet in DynelManager.LocalPlayer.GetPetDynels())
                {
                    Chat.WriteLine($"\t{pet.Name}");
                }
                */

                /*
                Item item;
                if(Inventory.Find(244216, out item))
                {
                    DummyItem dummyItem = DummyItem.GetFromTemplate(item.Slot);
                    dummyItem.MeetsUseReqs();
                }
                */

                //DevExtras.LoadAllSurfaces();

                /*
                MovementController movementController = new MovementController(true);

                List<Vector3> testPath = new List<Vector3> {
                    new Vector3(438.6, 8.0f, 524.4f),
                    new Vector3(446.8f, 8.0f, 503.7f),
                    new Vector3(460.8, 15.1f, 414.0f) 
                };

                movementController.RunPath(testPath);
                */

                Chat.WriteLine($"Missions ({Mission.List.Count})");
                foreach (Mission mission in Mission.List)
                {
                    Chat.WriteLine($"   {mission.Identity}");
                    Chat.WriteLine($"       Ptr: {mission.Pointer.ToString("X4")}");
                    Chat.WriteLine($"       Source: {mission.Source}");
                    Chat.WriteLine($"       Playfield: {(mission.Location != null ? mission.Location.Playfield.ToString() : "NULL")}");
                    Chat.WriteLine($"       WorldPos: {(mission.Location != null ? mission.Location.Pos.ToString() : "NULL")}");
                    Chat.WriteLine($"       DisplayName: {mission.DisplayName}");
                }

                //Inventory.GetContainerItems(new Identity(IdentityType.Bank, Game.ClientInst));

                //DynelManager.LocalPlayer.SetStat(Stat.AggDef, 100);

                /*
                List<Item> characterItems = Inventory.Items;
                //List<Item> characterItems = Inventory.Items;

                foreach (Item item in characterItems)
                {
                    Chat.WriteLine($"{item.Slot} - {item.LowId} - {item.Name} - {item.QualityLevel} - {item.UniqueIdentity}");
                }
                */

                /*
                Chat.WriteLine("Backpacks:");

                List<Backpack> backpacks = Inventory.Backpacks;
                foreach(Backpack backpack in backpacks)
                {
                    Chat.WriteLine($"{backpack.Identity} ({backpack.Name}) - IsOpen:{backpack.IsOpen}{((backpack.IsOpen) ? $" - Items:{backpack.Items.Count}" : "")}");
                }        

                if (Inventory.FindContainer("loli", out Backpack bp))
                {
                    Chat.WriteLine("Found BP by handle!");
                }
                */
                /*
                Item noviRing;
                if (Inventory.Find(226307, out noviRing))
                {
                    //noviRing.Equip(EquipSlot.Cloth_RightFinger);

                    Container openBag = Inventory.Backpacks.FirstOrDefault(x => x.IsOpen);
                    if(openBag != null)
                    {
                        noviRing.MoveToContainer(openBag);
                    }
                }
                */

                //DynelManager.LocalPlayer.CastNano(new Identity(IdentityType.NanoProgram, 223372), DynelManager.LocalPlayer);

                _menu = new Menu("TestPlugin", "TestPlugin");
                _menu.AddItem(new MenuBool("DrawingTest", "Drawing Test", false));
                //_menu.AddItem(new MenuTest("CrashTime", "Inb4 Crash"));
                OptionPanel.AddMenu(_menu);

                Chat.RegisterCommand("modlist", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    if (DummyItem.CreateDummyItemID(302724, 302724, 300, out Identity item))
                    {
                        N3EngineClientAnarchy.DebugSpellListToChat(item, 1, 0xE);
                        Chat.WriteLine(Utils.FindPattern("Gamecode.dll", "55 8B EC 8B 41 24 8B 4D 08 8B 04 88 5D C2 04 00").ToString("X"));
                    }
                });

                Chat.RegisterCommand("kdtree", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    VisualEnvFX_t.ToggleRandyDebuggerKDTreeDisplay(VisualEnvFX_t.GetInstance());
                });

                Chat.RegisterCommand("modlistspell", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    if (Spell.Find("Mongo Bash!", out Spell item))
                    {

                        Chat.WriteLine("SpellList:");
                        foreach (SpellData spell in item.UseModifiers)
                        {
                            Chat.WriteLine($"\t{spell.Function} - {spell.GetType()}");

                            foreach (var prop in spell.Properties)
                                Chat.WriteLine($"\t\t{prop.Key}: {prop.Value}");
                        }
                    }
                });

                Chat.RegisterCommand("healing", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    if (Inventory.Find("Health and Nano Stim", out Item item))
                    {
                        int targetHealing = item.UseModifiers.Where(x => x is SpellData.Healing hx && hx.ApplyOn == SpellModifierTarget.Target).Cast<SpellData.Healing>().Sum(x => x.Average);
                        Chat.WriteLine($"{item.Name} @ {item.QualityLevel} heals targets for {targetHealing}");
                    }
                });

                Chat.RegisterCommand("dumpops", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    for (int i = 0; i < 300; i++)
                    {
                        string str = DevExtras.SpellOperatorToString(i);

                        if (str == string.Empty || str.Contains("Missing SpellData"))
                            continue;

                        Chat.WriteLine($"{str} = {i},");
                    }
                });


                Chat.RegisterCommand("dueltarget", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    if (Targeting.TargetChar == null)
                        return;

                    Duel.Challenge(Targeting.Target.Identity);
                });

                Chat.RegisterCommand("split", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    if (param.Length < 2)
                        return;

                    if (Inventory.Find(int.Parse(param[0]), out Item item))
                        item.Split(int.Parse(param[1]));
                });

                Chat.RegisterCommand("testui", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    //Chat.WriteLine(Window.GetActiveWindow().Name);
                    Chat.WriteLine($"{ItemListViewBase_c.Create(new Rect(999999, 999999, -999999, -999999), 0, 0, 0, Identity.None).ToString("X4")}");

                });

                Chat.RegisterCommand("testmail", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    SendShortMailMsg(MailMsgType.TakeAll, 0xEC36AD);

                });

                Chat.RegisterCommand("dumppf", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    for (int i = 100; i < ushort.MaxValue; i++)
                    {
                        IntPtr pName = N3InterfaceModule_t.GetPFName(i);

                        if (pName == IntPtr.Zero)
                            continue;

                        Chat.WriteLine($"{Utils.UnsafePointerToString(pName).Replace(" ", "").Replace("'", "")} = {i},");
                        //break;
                    }
                });

                Chat.RegisterCommand("testbsjoin", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    Battlestation.JoinQueue(Battlestation.Side.Blue);
                });

                Chat.RegisterCommand("testbsleave", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    Battlestation.LeaveQueue();
                });

                Chat.RegisterCommand("openwindow", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    testWindow = Window.CreateFromXml("Test", $"{pluginDir}\\TestWindow.xml");
                    testWindow.Show(true);
                    chatWindow.WriteLine($"Window.Pointer: {testWindow.Pointer.ToString("X4")}");
                    chatWindow.WriteLine($"Window.Name: {testWindow.Name}");
                    if (testWindow.IsValid)
                    {
                        if (testWindow.FindView("textOptionCheckbox", out View textOptionCheckboxView))
                        {
                            Chat.WriteLine($"textOptionCheckboxView.Pointer: {textOptionCheckboxView.Pointer.ToString("X4")}");
                        }

                        if (testWindow.FindView("testTextView", out TextView testView))
                        {
                            Chat.WriteLine($"testTextView.Pointer: {testView.Pointer.ToString("X4")}");
                            Chat.WriteLine($"testTextView.Text: {testView.Text}");
                            testView.SetDefaultColor(0xFF00E8);

                            testView.Text = "1337";
                            Chat.WriteLine($"testTextView.Text(New): {testView.Text}");
                        }

                        if (testWindow.FindView("testPowerBar", out PowerBarView testPowerBar))
                        {
                            Chat.WriteLine($"testPowerBar.Pointer: {testPowerBar.Pointer.ToString("X4")}");
                            Chat.WriteLine($"testPowerBar.Value: {testPowerBar.Value}");
                            testPowerBar.Value = 0.1f;
                        }

                        if (testWindow.FindView("testCheck", out Checkbox testCheckbox))
                        {
                            Chat.WriteLine($"testCheckbox");
                        }

                        if (testWindow.FindView("testButton", out Button testButton))
                        {
                            testButton.Clicked += OnTestButtonClicked;
                            testButton.SetLabelColor(0xFF0000);
                            testButton.SetGfx(ButtonState.Pressed, "GFX_GUI_BS_REDSTAR");
                        }

                        if (testWindow.FindView("testButton2", out ButtonBase testButton2))
                        {
                            testButton2.Clicked += OnTestButtonClicked;
                        }

                        if (testWindow.FindView("testComboBox", out ComboBox testComboBox))
                        {
                            Chat.WriteLine($"ComboBox.Pointer: {testComboBox.Pointer.ToString("X4")}");

                            for (int i = 0; i < 10; i++)
                                testComboBox.AppendItem(i, $"loli {i}");
                        }
                        if (testWindow.FindView("testTextInput", out TextInputView testTextInput))
                        {
                            testTextInput.SetAlpha((float)0.7);
                            testTextInput.Text = "head pats";
                        }
                        if (testWindow.FindView("testBitmapView", out BitmapView testBitmapView))
                        {
                            testBitmapView.SetBitmap("GFX_GUI_SHORT_HOR_BAR_EMPTY");
                        }
                    }
                });

                Chat.RegisterCommand("savesettings", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    Settings.Save();
                });

                Chat.RegisterCommand("test", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    //foreach(Item item in Inventory.Bank.Items)
                    //{
                    //    Chat.WriteLine(item.Name);
                    //}

                    //DBIdentity identity = new DBIdentity((DBIdentityType)1000009, 4365);
                    //DBIdentity identity = new DBIdentity((DBIdentityType)1000047, 5927);
                    //IntPtr animPtr = ResourceDatabase.GetDbObject(identity);
                    //Chat.WriteLine(animPtr.ToString("X"));

                    //unsafe
                    //{
                    //    IntPtr pCatMesh = *(IntPtr*)(DynelManager.LocalPlayer.Pointer + 0xC0);
                    //    ClearAnimations(pCatMesh);
                    //}

                    //Chat.WriteLine(DynelManager.LocalPlayer.Pets.Length);
                    //Chat.WriteLine(Targeting.Target.Identity.Instance);

                    //foreach(Item aggroTool in Inventory.Items.Where(x => x.Name == "Aggression Enhancer"))
                    //    Chat.WriteLine($"QL {aggroTool.QualityLevel} - Low: {aggroTool.LowId} / High: {aggroTool.HighId}");

                    //if (DummyItem.TryGet(new Identity(IdentityType.NanoProgram, 55751), out NanoItem buff))
                    //    Chat.WriteLine($"Loaded buff: {buff.Name} -> {buff.Id} -> NCU: {buff.NCU} -> StackingOrder: {buff.StackingOrder} -> Nanoline{buff.Nanoline}");

                    //if(DynelManager.Find("Collected Essence", out SimpleChar boss))
                    //{
                    //    Chat.WriteLine($"Essence: {boss.Identity}");
                    //    foreach (Buff bbuff in boss.Buffs)
                    //        Chat.WriteLine(bbuff.Name);
                    //}

                    //GMI.GetInventory().ContinueWith(marketInventory =>
                    //{
                    //    Chat.WriteLine($"!!GMI!!");
                    //    Chat.WriteLine($"Credits: {marketInventory.Result.Credits}");
                    //    Chat.WriteLine($"Items:");
                    //    foreach (MarketInventoryItem item in marketInventory.Result.Items)
                    //        Chat.WriteLine($"\t{item.Name}\tQL: {item.TemplateQL}\tCount: {item.Count}");
                    //});

                    //GMI.GetMarketOrders().ContinueWith(marketOrders =>
                    //{
                    //    Chat.WriteLine($"MySellOrders ({marketOrders.Result.SellOrders.Count}):");
                    //    foreach (MyMarketSellOrder order in marketOrders.Result.SellOrders)
                    //    {
                    //        Chat.WriteLine($"\t{order.Id} - {order.Name} - Count: {order.Count}\tPrice: {order.Price}");

                    //        if (order.Name == "Tear of Oedipus")
                    //            order.Cancel();
                    //    }

                    //    Chat.WriteLine($"MyBuyOrders ({marketOrders.Result.BuyOrders.Count}):"); 
                    //    foreach (MyMarketBuyOrder order in marketOrders.Result.BuyOrders)
                    //        Chat.WriteLine($"\t{order.Id} - {order.Name} - Count: {order.Count}\tMinQL: {order.MinQl}\tMaxQL: {order.MaxQl}\tPrice: {order.Price}");
                    //});

                    //WITHDRAW CASH
                    /*
                    GMI.GetInventory().ContinueWith(marketInventory =>
                    {
                        if (marketInventory.Result == null)
                            return;

                        GMI.WithdrawCash(1000);
                    });
                    */

                    //FIND ITEM AND WITHDRAW
                    //GMI.GetInventory().ContinueWith(marketInventory =>
                    //{
                    //    if (marketInventory.Result == null)
                    //        return;

                    //    if(marketInventory.Result.Find("Health and Nano Stim", out MyMarketInventoryItem item))
                    //    {
                    //        Chat.WriteLine($"Withdrawing {item.Name}");

                    //        item.Withdraw(1).ContinueWith(withdrawResult =>
                    //        {
                    //            if(withdrawResult.Result.Succeeded)
                    //                Chat.WriteLine($"Successfully withdrew {item.Name}");
                    //            else
                    //                Chat.WriteLine($"Failed to withdraw {item.Name}. The error is {withdrawResult.Result.Message}");
                    //        });
                    //    }
                    //});


                    //GMI.GetItemVisualsBatch(new List<MarketClusterItem>() { new MarketClusterItem() { ClusterId = 53656, Ql = 1 }, new MarketClusterItem() { ClusterId = 181670, Ql = 299 }, new MarketClusterItem() { ClusterId = 181670, Ql = 100 } });


                    //Settings["DrawStuff"] = true;

                    //DynelManager.LocalPlayer.Position += Vector3.Rotate(Vector3.Zero, DynelManager.LocalPlayer.Rotation.Forward, 90);

                    /*
                    if (Targeting.Target == null)
                        Chat.WriteLine("No target");
                    else
                        Chat.WriteLine(Targeting.Target.Identity);


                    if (Targeting.TargetChar == null)
                        Chat.WriteLine("No target or target isn't a char");
                    else
                        Chat.WriteLine(Targeting.TargetChar.Identity);
                    */

                    /*
                    if (Spell.Find(85872, out Spell spell))
                    {
                        Chat.WriteLine($"Drain Self No Target: {spell.MeetsUseReqs()}");
                        Chat.WriteLine($"Drain Self Self: {spell.MeetsSelfUseReqs()}");
                        Chat.WriteLine($"Drain Self No Target: {spell.MeetsUseReqs(Targeting.TargetChar)}");
                    }
                    */

                    /*
                    Chat.WriteLine($"Number Of Free Inventory Slots: {Inventory.NumFreeSlots}");

                    if (Inventory.Find(281267, out Item item1))
                    {
                        Chat.WriteLine("Equip Slots:");
                        foreach(EquipSlot slot in item1.EquipSlots)
                            Chat.WriteLine($"\t{slot}");
                    }

                    Backpack backpack = Inventory.Backpacks.FirstOrDefault();

                    if(backpack != null)
                        backpack.SetName("Randoseru");

                    if(PerkAction.Find("Double Shot", out PerkAction perk))
                    {
                        Chat.WriteLine(perk.MeetsUseReqs());
                    }
                    */

                    /*
                    if (Mission.Find("Mission Assignment 7429-323-...", out Mission mission))
                        mission.UploadToMap(); 
                    */
                    /*
                    foreach(Pet pet in DynelManager.LocalPlayer.Pets)
                    {
                        Chat.WriteLine(pet.Character != null ? pet.Character.Name : "No pet character found");
                        Chat.WriteLine(pet.Type);
                    }
                    */


                    //DevExtras.Test();

                    //if (DynelManager.LocalPlayer.Buffs.Find(215264, out Buff testBuff))
                    //    testBuff.Remove();

                    /*
                    if (DynelManager.LocalPlayer.FightingTarget != null)
                    {
                        chatWindow.WriteLine(DynelManager.LocalPlayer.GetLogicalRangeToTarget(DynelManager.LocalPlayer.FightingTarget).ToString());


                        if (Perk.Find("Capture Vigor", out Perk perk))
                        {
                            chatWindow.WriteLine(perk.GetStat(Stat.AttackRange).ToString());
                            chatWindow.WriteLine(perk.IsInRange(DynelManager.LocalPlayer.FightingTarget).ToString());
                        }
                    }*/
                });

                try
                {
                    IPCChannel testChannel1 = new IPCChannel(225);
                    IPCChannel testChannel2 = new IPCChannel(226);
                    IPCChannel testChannel3 = new IPCChannel(227);
                    IPCChannel testChannel4 = new IPCChannel(228);
                    IPCChannel testChannel5 = new IPCChannel(229);

                    _ipcChannel = new IPCChannel(1);

                    _ipcChannel.RegisterCallback((int)IPCOpcode.Test, (sender, msg) =>
                    {
                        TestMessage testMsg = (TestMessage)msg;

                        Chat.WriteLine($"TestMessage: {testMsg.Leet} - {testMsg.Position}");
                    });

                    _ipcChannel.RegisterCallback((int)IPCOpcode.Empty, (sender, msg) =>
                    {
                        Chat.WriteLine($"EmptyMessage");
                    });

                }
                catch (Exception e)
                {
                    Chat.WriteLine(e.Message);
                }

                Settings = new Settings("TestPlugin");
                Settings.AddVariable("DrawStuff", false);
                Settings.AddVariable("AnotherVariable", 1911);

                Game.OnUpdate += OnUpdate;
                Game.TeleportStarted += Game_OnTeleportStarted;
                Game.TeleportEnded += Game_OnTeleportEnded;
                Game.TeleportFailed += Game_OnTeleportFailed;
                Game.PlayfieldInit += Game_PlayfieldInit;
                //MiscClientEvents.AttemptingSpellCast += AttemptingSpellCast;
                Network.N3MessageReceived += Network_N3MessageReceived;
                //Network.N3MessageSent += Network_N3MessageSent;
                Network.PacketReceived += Network_PacketReceived;
                // Network.PacketSent += Network_PacketSent;
                Network.ChatMessageReceived += Network_ChatMessageReceived;
                Team.TeamRequest += Team_TeamRequest;
                Team.MemberLeft += Team_MemberLeft;
                Item.ItemUsed += Item_ItemUsed;
                NpcDialog.AnswerListChanged += NpcDialog_AnswerListChanged;
                Inventory.ContainerOpened += OnContainerOpened;
                Duel.Challenged = DuelChallenged;
                Duel.StatusChanged = DuelStatusChanged;
                CharacterAction.Inspect += OnInspect;
                //DynelManager.DynelSpawned += DynelSpawned;
                //DynelManager.CharInPlay += CharInPlay;
            }
            catch (Exception e)
            {
                Chat.WriteLine(e.Message);
            }
        }

        private void OnInspect(object sender, InspectEventArgs inpectArgs)
        {
            foreach (var page in inpectArgs.Pages)
            {
                Chat.WriteLine(page.Key);

                foreach (var slot in page.Value)
                    Chat.WriteLine($"{slot.EquipSlot} {slot.LowId} {slot.HighId} {slot.Ql}");
            }
        }

        private void DuelChallenged(object sender, DuelRequestEventArgs e)
        {
            Chat.WriteLine($"{e.Challenger} challenged us to a duel!");

            e.Accept();
        }

        private void DuelStatusChanged(object sender, DuelStatusChangedEventArgs e)
        {
            Chat.WriteLine($"Duel Status changed to {e.Status} ({e.Opponent})");
        }

        private void OnTestButtonClicked(object s, ButtonBase button)
        {
            Chat.WriteLine($"TestButton Clicked! {button.Pointer.ToString("X4")}");
        }

        private void OnContainerOpened(object sender, Container container)
        {
            Chat.WriteLine($"Container {container.Identity} opened.");
            foreach (Item item in container.Items)
                Chat.WriteLine($"\t{item.Name} @ {item.Slot}");
        }

        private void AttemptingSpellCast(object sender, AttemptingSpellCastEventArgs e)
        {
            Chat.WriteLine($"{e.Nano}, {e.Target}");
            e.Block();
        }

        private void Network_PacketSent(object s, byte[] packet)
        {
            N3MessageType msgType = (N3MessageType)((packet[16] << 24) + (packet[17] << 16) + (packet[18] << 8) + packet[19]);

            if (msgType == N3MessageType.QuestAlternative)
                Chat.WriteLine(BitConverter.ToString(packet).Replace("-", ""));
        }

        private void Network_ChatMessageReceived(object s, SmokeLounge.AOtomation.Messaging.Messages.ChatMessageBody chatMessage)
        {
            if (chatMessage.PacketType == ChatMessageType.PrivateMessage)
            {
                Chat.WriteLine($"Received {((PrivateMsgMessage)chatMessage).Text}");
            }
        }

        private void Network_N3MessageSent(object s, SmokeLounge.AOtomation.Messaging.Messages.N3Message n3Msg)
        {
            //Chat.WriteLine($"{n3Msg.N3MessageType}");
        }

        private void Network_N3MessageReceived(object s, SmokeLounge.AOtomation.Messaging.Messages.N3Message n3Msg)
        {
            // Chat.WriteLine($"{n3Msg.N3MessageType} {n3Msg.Identity}");

            /*
            if(n3Msg.N3MessageType == SmokeLounge.AOtomation.Messaging.Messages.N3MessageType.PlayfieldAnarchyF)
            {
                PlayfieldAnarchyFMessage ayy = (PlayfieldAnarchyFMessage)n3Msg;
                //Chat.WriteLine($"GenericCmd: {ayy.Action.ToString()}\t{ayy.Count.ToString()}\t{ayy.Target.ToString()}\t{ayy.Temp1.ToString()}\t{ayy.Temp4.ToString()}\t{ayy.User.ToString()}\t{ayy.Identity.ToString()}");
            }*/

            //if (n3Msg.N3MessageType == N3MessageType.CharDCMove)
            //    Chat.WriteLine($"MoveType: {((CharDCMoveMessage)n3Msg).MoveType}");

            /*
            if (n3Msg.N3MessageType == SmokeLounge.AOtomation.Messaging.Messages.N3MessageType.TemplateAction)
            {
                TemplateActionMessage ayy = (TemplateActionMessage)n3Msg;
                Chat.WriteLine($"TemplateAction: {ayy.Unknown1.ToString()}\t{ayy.Unknown2.ToString()}\t{ayy.Unknown3.ToString()}\t{ayy.Unknown4.ToString()}\t{ayy.ItemLowId.ToString()}\t{ayy.Placement.ToString()}\t{ayy.Identity.ToString()}");
            }
            */


            //if (n3Msg.N3MessageType == N3MessageType.Stat)
            //{
            //    StatMessage ayy = (StatMessage)n3Msg;
            //    Chat.WriteLine($"Stat Update for {ayy.Identity}");

            //    foreach (var stat in ayy.Stats)
            //        Chat.WriteLine($"\t{stat.Value1}: {stat.Value2}");
            //}

            if (n3Msg.N3MessageType == N3MessageType.Trade)
            {
                TradeMessage ayy = (TradeMessage)n3Msg;
                Chat.WriteLine($"Trade: TradeAction: {ayy.Action}\tParam1: {ayy.Param1}\tParam2: {ayy.Param2}\tParam3: {ayy.Param3}\tParam4: {ayy.Param4}\t{ayy.Unknown1}");
            }

            if (n3Msg.N3MessageType == N3MessageType.SimpleCharFullUpdate)
            {
                SimpleCharFullUpdateMessage scfu = (SimpleCharFullUpdateMessage)n3Msg;

                if (scfu.Flags2 == ScfuFlags2.HasOwner)
                    Chat.WriteLine($"{scfu.Name} has flag 4. Owner: {scfu.Owner}");
            }

            /*
            if (n3Msg.N3MessageType == SmokeLounge.AOtomation.Messaging.Messages.N3MessageType.Feedback)
            {
                FeedbackMessage ayy = (FeedbackMessage)n3Msg;
                Chat.WriteLine($"Feedback: {ayy.MessageId.ToString()}\t{ayy.CategoryId.ToString()}\t{ayy.Unknown1.ToString()}");
            }*/
            /*
            if(n3Msg.N3MessageType == SmokeLounge.AOtomation.Messaging.Messages.N3MessageType.CharacterAction)
            {
                CharacterActionMessage charActionMessage = (CharacterActionMessage)n3Msg;
                Chat.WriteLine($"CharacterAction {charActionMessage.Action}\t{charActionMessage.Identity}\t{charActionMessage.Target}\t{charActionMessage.Parameter1}\t{charActionMessage.Parameter2}\t{charActionMessage.Unknown1}\t{charActionMessage.Unknown2}");
            }*/
        }

        private void Team_TeamRequest(object s, TeamRequestEventArgs e)
        {
            e.Accept();
        }

        private void Team_MemberLeft(object s, Identity leaver)
        {
            Chat.WriteLine($"Player {leaver} left the team.");
        }

        private void NpcDialog_AnswerListChanged(object s, Dictionary<int, string> options)
        {
            /*
            Identity target = (Identity)s;

            foreach(KeyValuePair<int, string> option in options)
            {
                if (option.Value == "Is there anything I can help you with?" ||
                    option.Value == "I will defend against the creatures of the brink!" ||
                    option.Value == "I will deal with only the weakest aversaries")
                    NpcDialog.SelectAnswer(target, option.Key);
            }
            */
        }

        private void Game_OnTeleportStarted(object s, EventArgs e)
        {
            Chat.WriteLine("Teleport Started!");
        }

        private void Game_PlayfieldInit(object s, uint id)
        {
            Chat.WriteLine($"PlayfieldInit: {id}");
        }

        private void Game_OnTeleportFailed(object s, EventArgs e)
        {
            Chat.WriteLine("Teleport Failed!");
        }

        private void Game_OnTeleportEnded(object s, EventArgs e)
        {
            Chat.WriteLine($"Teleport Ended!");
        }

        private void Item_ItemUsed(object s, ItemUsedEventArgs e)
        {
            Chat.WriteLine($"Item {e.Item.Name} used by {e.OwnerIdentity}");
            //mc.SetDestination(new Vector3(620, 66.8f, 687));
        }

        double lastTrigger = Time.NormalTime;

        private void OnUpdate(object s, float deltaTime)
        {
            if (testWindow != null && testWindow.IsValid)
            {
                if (testWindow.FindView("testTextView", out TextView testView))
                    testView.Text = Targeting.TargetChar == null ? "<No Target>" : Targeting.TargetChar.Position.ToString();

                if (testWindow.FindView("testPowerBar", out PowerBarView testPowerBar))
                {
                    if (Targeting.TargetChar == null)
                    {
                        testPowerBar.Value = 0;
                    }
                    else
                    {
                        float dist = DynelManager.LocalPlayer.GetLogicalRangeToTarget(Targeting.TargetChar);
                        testPowerBar.Value = Math.Min(1f, dist / 20f);
                    }
                }
            }

            if (Settings["DrawStuff"].AsBool())
            {
                foreach (Dynel player in DynelManager.Players)
                {
                    Debug.DrawSphere(player.Position, 1, DebuggingColor.LightBlue);
                    Debug.DrawLine(DynelManager.LocalPlayer.Position, player.Position, DebuggingColor.LightBlue);
                }
            }

            if (DynelManager.LocalPlayer.FightingTarget != null)
            {
                if (SpecialAttack.FastAttack.IsInRange(DynelManager.LocalPlayer.FightingTarget) && SpecialAttack.FastAttack.IsAvailable())
                    SpecialAttack.FastAttack.UseOn(DynelManager.LocalPlayer.FightingTarget);
            }

            foreach (SimpleChar character in DynelManager.Characters)
            {
                if (character.IsPathing)
                {
                    Debug.DrawLine(character.Position, character.PathingDestination, DebuggingColor.LightBlue);
                    Debug.DrawSphere(character.PathingDestination, 0.2f, DebuggingColor.LightBlue);
                }
            }

            Vector3 rayOrigin = DynelManager.LocalPlayer.Position;
            Vector3 rayTarget = DynelManager.LocalPlayer.Position;
            rayTarget.Y = 0;

            if (Playfield.Raycast(rayOrigin, rayTarget, out Vector3 hitPos, out Vector3 hitNormal))
            {
                Debug.DrawLine(rayOrigin, rayTarget, DebuggingColor.White);
                Debug.DrawLine(hitPos, hitPos + hitNormal, DebuggingColor.Yellow);
                Debug.DrawSphere(hitPos, 0.2f, DebuggingColor.White);
                Debug.DrawSphere(hitPos + hitNormal, 0.2f, DebuggingColor.Yellow);
            }

            /*
            if (!Item.HasPendingUse && Inventory.Find(285509, out Item derp))
            {
                derp.Use();
            }*/

            if (Time.NormalTime > lastTrigger + 2)
            {
                //Chat.WriteLine($"IsChecked: {((Checkbox)window.Views[0]).IsChecked}");
                //IntPtr tooltip = AOSharp.Common.Unmanaged.Imports.ToolTip_c.Create("LOLITA", "COMPLEX");

                /*
                Spell testSpell;
                if(Spell.Find("Matrix of Ka", out testSpell))
                {
                    if (testSpell.IsReady && testSpell.MeetsUseReqs())
                        testSpell.Cast();
                }
                */

                /*
                Perk testPerk;
                if(Perk.Find("Bot Confinement", out testPerk))
                {
                    Chat.WriteLine($"Bot Confinement IsAvailable: {testPerk.IsAvailable}");
                    //if (testPerk.IsAvailable)
                    //   testPerk.Use();
                }*/

                //SimpleChar randomTarget = DynelManager.Characters.OrderBy(x => Guid.NewGuid()).FirstOrDefault();
                //Targeting.SetTarget(randomTarget);

                /*
                _ipcChannel.Broadcast(new TestMessage()
                {
                    Position = DynelManager.LocalPlayer.Position,
                    Leet = 1337
                });
                */

                lastTrigger = Time.NormalTime;
            }
        }

        private void DynelSpawned(object s, Dynel dynel)
        {
            if (dynel.Identity.Type == IdentityType.SimpleChar)
            {
                SimpleChar c = dynel.Cast<SimpleChar>();

                Chat.WriteLine($"SimpleChar Spawned(TestPlugin): {c.Identity} -- {c.Name} -- {c.Position} -- {c.Health} -- IsInPlay: {c.IsInPlay}");
            }
        }

        private void CharInPlay(object s, SimpleChar character)
        {
            Chat.WriteLine($"{character.Name} is now in play.");
        }

        public enum MailMsgType
        {
            Read = 1,
            Delete = 2,
            TakeAll = 3,
        }

        private void SendShortMailMsg(MailMsgType msgType, int msgId)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BigEndianBinaryWriter writer = new BigEndianBinaryWriter(stream))
                {
                    //Header
                    writer.Write((short)0);
                    writer.Write((short)PacketType.N3Message);
                    writer.Write((short)1);
                    writer.Write((short)0);
                    writer.Write(Game.ClientInst);
                    writer.Write((int)2);
                    writer.Write((int)N3MessageType.Mail);
                    writer.Write((int)IdentityType.SimpleChar);
                    writer.Write(Game.ClientInst);
                    writer.Write((byte)0);

                    //Body
                    writer.Write((short)msgType);
                    writer.Write((int)0);
                    writer.Write(msgId);


                    //Fix packet length
                    short length = (short)writer.BaseStream.Position;
                    writer.BaseStream.Position = 6;
                    writer.Write(length);

                    Network.Send(stream.ToArray());
                }
            }
        }

        public void DeleteMail(int msgId)
        {
            SendShortMailMsg(MailMsgType.Delete, msgId);
        }

        private void Network_PacketReceived(object s, byte[] packet)
        {
            N3MessageType msgType = (N3MessageType)((packet[16] << 24) + (packet[17] << 16) + (packet[18] << 8) + packet[19]);

            if (msgType == N3MessageType.SimpleCharFullUpdate)
            {
                SimpleCharFullUpdateFlags flags = (SimpleCharFullUpdateFlags)((packet[30] << 24) + (packet[31] << 16) + (packet[32] << 8) + packet[33]);
                //Chat.WriteLine($"{flags} - IsPet: {flags.HasFlag(SimpleCharFullUpdateFlags.IsPet)}");
                File.WriteAllBytes($"SCFU/{(int)((packet[24] << 24) + (packet[25] << 16) + (packet[26] << 8) + packet[27])}", packet);
            }

            //Chat.WriteLine($"{msgType} - {BitConverter.ToString(packet).Replace("-", "")}");

            return;
            //Chat.WriteLine($"{msgType}");

            if (msgType == N3MessageType.QuestAlternative)
                Chat.WriteLine(BitConverter.ToString(packet).Replace("-", ""));

            if (msgType == N3MessageType.SpellList)
                Chat.WriteLine($"SpellList: {BitConverter.ToString(packet).Replace("-", "")}");

            if (msgType == N3MessageType.QuestFullUpdate)
                Chat.WriteLine($"QuestFullUpdate: {BitConverter.ToString(packet).Replace("-", "")}");

            return;

            if (((N3MessageType)((packet[16] << 24) + (packet[17] << 16) + (packet[18] << 8) + packet[19])) == N3MessageType.PlayfieldAnarchyF)
            {
                Identity Identity;
                int Version;
                Vector3 CharacterCoordinates;
                byte Unknown2;
                Identity PlayfieldId1;
                int Unknown3;
                uint Unknown4;
                Identity PlayfieldId2;
                Identity PlayfieldId3;

                PlayfieldACGInfo PlayfieldAcgInfo = null;

                int PlayfieldX;
                int PlayfieldZ;

                using (MemoryStream stream = new MemoryStream(packet))
                {
                    using (BigEndianBinaryReader reader = new BigEndianBinaryReader(stream))
                    {
                        reader.BaseStream.Position = 20;
                        Identity = new Identity((IdentityType)reader.ReadInt32(), reader.ReadInt32());
                        reader.ReadByte();

                        Version = reader.ReadInt32();
                        CharacterCoordinates = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                        Chat.WriteLine(CharacterCoordinates);
                        Unknown2 = reader.ReadByte();
                        PlayfieldId1 = new Identity((IdentityType)reader.ReadInt32(), reader.ReadInt32());
                        Chat.WriteLine(PlayfieldId1);

                        Unknown3 = reader.ReadInt32();
                        Unknown4 = reader.ReadUInt32();
                        PlayfieldId2 = new Identity((IdentityType)reader.ReadInt32(), reader.ReadInt32());
                        PlayfieldId3 = new Identity((IdentityType)reader.ReadInt32(), reader.ReadInt32());
                        Chat.WriteLine(PlayfieldId2);
                        Chat.WriteLine(PlayfieldId3);

                        switch (PlayfieldId3.Type)
                        {
                            case IdentityType.ACGBuildingGeneratorData:
                                {
                                    PlayfieldAcgInfo = new PlayfieldACGInfo()
                                    {
                                        Revision = reader.ReadInt32(),
                                        Version = reader.ReadInt16(),
                                        Width = reader.ReadInt16(),
                                        Height = reader.ReadInt16(),
                                        Unknown3 = reader.ReadInt16(),
                                        TemplateId = (TemplatePlayfields)reader.ReadInt32(),
                                        AmbientR = reader.ReadByte(),
                                        AmbientG = reader.ReadByte(),
                                        AmbientB = reader.ReadByte(),
                                        Rooms = new List<RoomInstance>()
                                    };

                                    var roomCnt = reader.ReadInt32();
                                    for (int i = 0; i < roomCnt; i++)
                                    {
                                        var rI = new RoomInstance
                                        {
                                            RoomId = reader.ReadInt16(),
                                            Floor = reader.ReadByte(),
                                            X = reader.ReadByte(),
                                            Y = reader.ReadByte(),
                                            Rotation = reader.ReadByte()
                                        };
                                        PlayfieldAcgInfo.Rooms.Add(rI);
                                    }
                                    break;
                                }
                            case IdentityType.ProxyInstance:
                                reader.ReadInt32();
                                reader.ReadInt32();

                                var roomCount = reader.ReadInt32();
                                for (var i = 0; i < roomCount; i++)
                                {

                                    reader.ReadInt32();
                                    reader.ReadInt32();
                                    reader.ReadInt32();
                                    reader.ReadInt32();
                                    reader.ReadInt32();
                                }
                                break;
                            default:
                                break;
                        }

                        PlayfieldX = reader.ReadInt32();
                        PlayfieldZ = reader.ReadInt32();
                    }
                }

                Chat.WriteLine($"Template: {PlayfieldAcgInfo.TemplateId}");
                foreach (RoomInstance room in PlayfieldAcgInfo.Rooms)
                    Chat.WriteLine($"{room.RoomId}\t{room.Floor}\t{room.X}\t{room.Y}\t{room.Rotation}");
            }
        }

        public override void Teardown()
        {
            Chat.WriteLine("Teardown time!");
        }
    }
}
