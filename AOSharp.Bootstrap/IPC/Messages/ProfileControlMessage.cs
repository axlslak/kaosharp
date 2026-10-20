using System;
using System.IO;
using Newtonsoft.Json;

namespace AOSharp.Bootstrap.IPC
{
    public enum ProfileControlKind : byte { Hello, Ready, Command, Reply, Reload, Progress, PluginCommand }
    public enum ProfileReloadStage : byte { Unloading, Loading, Completed, Failed, CleanupFailed, Rejected }

    public class ProfileControlMessage : IPCMessage
    {
        public const int ProtocolVersion = 2;
        public ProfileControlKind Kind;
        public long OperationId;
        public ProfileReloadStage Stage;
        public string Text;
        public string[] Assemblies = new string[0];

        public ProfileControlMessage() : base((byte)HookOpCode.ProfileControl) { }

        protected override void OnSerialize(BinaryWriter writer)
        {
            if ((Text?.Length ?? 0) > 3000) throw new InvalidDataException("Control text is too long.");
            writer.Write((byte)Kind);
            writer.Write(OperationId);
            writer.Write((byte)Stage);
            writer.Write(Text ?? "");
            writer.Write(JsonConvert.SerializeObject(Assemblies ?? new string[0]));
        }

        protected override void OnDeserialize(BinaryReader reader)
        {
            Kind = (ProfileControlKind)reader.ReadByte();
            OperationId = reader.ReadInt64();
            Stage = (ProfileReloadStage)reader.ReadByte();
            Text = reader.ReadString();
            Assemblies = JsonConvert.DeserializeObject<string[]>(reader.ReadString()) ?? new string[0];
            if (Text.Length > 3000 || Assemblies.Length > 1024)
                throw new InvalidDataException("Invalid profile control message.");
        }
    }
}
