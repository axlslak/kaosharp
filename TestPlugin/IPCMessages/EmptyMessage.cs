using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Common.GameData;
using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;
using TestPlugin.IPCMessages;

namespace TestPlugin
{
    [AoContract((int)IPCOpcode.Empty)]
    public class EmptyMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.Empty;
    }
}
