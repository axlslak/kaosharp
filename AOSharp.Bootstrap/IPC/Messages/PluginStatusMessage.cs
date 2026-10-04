using System;
using System.IO;

namespace AOSharp.Bootstrap.IPC
{
    public enum PluginLoadState { Loading, Initialized, Failed }

    [Serializable]
    public class PluginLoadResult
    {
        public string Path;
        public PluginLoadState State;
        public string Detail;
    }

    public class PluginStatusMessage : IPCMessage
    {
        public string Path;
        public PluginLoadState State;
        public string Detail;
        public PluginStatusMessage() : base((byte)HookOpCode.PluginStatus) { }
        protected override void OnSerialize(BinaryWriter writer)
        {
            writer.Write(Path ?? "");
            writer.Write((byte)State);
            // Keep each pipe message comfortably below the existing 64 KB buffer.
            string detail = Detail ?? "";
            writer.Write(detail.Length > 4000 ? detail.Substring(0, 4000) : detail);
        }
        protected override void OnDeserialize(BinaryReader reader)
        {
            Path = reader.ReadString();
            State = (PluginLoadState)reader.ReadByte();
            Detail = reader.ReadString();
        }
    }
}
