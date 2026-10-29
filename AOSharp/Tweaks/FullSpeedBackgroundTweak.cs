using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;

namespace AOSharp.Tweaks
{
    // Native new-client patch. No injection, foreground spoofing or periodic work.
    internal sealed class FullSpeedBackgroundTweak
    {
        internal enum PatchState { Missing, Unsupported, Disabled, Enabled }

        private const int SupportedLength = 4926976;
        private const int BranchOffset = 0x15D72B;
        private const string OriginalHash = "f2fd2bb9019f75a1f1a521678f8ed55b24fe0efb47f7a196625e0504cfa8bb90";
        private const string PatchedHash = "17d2ff52861529721ddff8a556042cfdd2dd169cd099aad55d9ab55ec63f8d3a";
        private readonly string _path;
        private readonly string _backupPath;

        internal FullSpeedBackgroundTweak(string installDirectory)
        {
            _path = Path.Combine(Path.GetFullPath(installDirectory), "Cheetah.dll");
            _backupPath = _path + ".aosharp-background-f2fd2bb9019f.bak";
        }

        internal PatchState Inspect()
        {
            if (!File.Exists(_path)) return PatchState.Missing;
            using (var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (stream.Length != SupportedLength) return PatchState.Unsupported;
                return Identify(ReadImage(stream));
            }
        }

        internal void SetEnabled(bool enabled)
        {
            RequireClientsClosed();
            // Hold an exclusive write handle throughout verification, backup and edit.
            // This also prevents a new client from loading this DLL during the edit.
            using (var stream = new FileStream(_path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                byte[] before = ReadImage(stream);
                PatchState state = Identify(before);
                if (state != PatchState.Disabled && state != PatchState.Enabled)
                    throw new InvalidOperationException("This Cheetah.dll build is not supported. No game file was changed.");

                if ((state == PatchState.Enabled) == enabled)
                {
                    if (enabled) ReadBackup();
                    return;
                }
                byte[] original;
                if (enabled)
                {
                    EnsureBackup(before);
                    original = before;
                }
                else
                {
                    original = ReadBackup();
                }

                byte[] after = (byte[])before.Clone();
                // Original: JNE skips Sleep(50) only when active. JMP skips it
                // regardless of focus, AFTER the separate lost/paused guard jumps.
                // The active flag and every other byte remain unchanged.
                after[BranchOffset] = enabled ? (byte)0xEB : original[BranchOffset];
                string expectedHash = enabled ? PatchedHash : OriginalHash;
                if (Hash(after) != expectedHash)
                    throw new InvalidOperationException("The patch verification failed. No game file was changed.");

                RequireClientsClosed();
                try
                {
                    stream.Position = BranchOffset;
                    stream.WriteByte(after[BranchOffset]);
                    stream.Flush(true);
                    if (Hash(ReadImage(stream)) != expectedHash)
                        throw new IOException("The updated DLL did not pass verification.");
                }
                catch (Exception writeError)
                {
                    // Only the single verified byte is ever written; never truncate
                    // the DLL or restore an old whole file over a later game update.
                    try
                    {
                        stream.Position = BranchOffset;
                        stream.WriteByte(before[BranchOffset]);
                        stream.Flush(true);
                        if (Hash(ReadImage(stream)) != Hash(before))
                            throw new IOException("The rollback did not pass verification.");
                    }
                    catch (Exception rollbackError)
                    {
                        throw new IOException("The change and rollback both failed. Do not start AO. Original backup: "
                            + _backupPath + ". " + rollbackError.Message, writeError);
                    }
                    throw new IOException("The change failed; the previous DLL contents were restored. "
                        + writeError.Message, writeError);
                }
            }
        }

        private static byte[] ReadImage(FileStream stream)
        {
            if (stream.Length != SupportedLength)
                throw new InvalidOperationException("This Cheetah.dll size is not supported. No game file was changed.");
            byte[] bytes = new byte[SupportedLength];
            stream.Position = 0;
            int offset = 0;
            while (offset < bytes.Length)
            {
                int read = stream.Read(bytes, offset, bytes.Length - offset);
                if (read == 0) throw new EndOfStreamException("Could not read the complete Cheetah.dll.");
                offset += read;
            }
            return bytes;
        }

        private static PatchState Identify(byte[] bytes)
        {
            if (bytes.Length != SupportedLength) return PatchState.Unsupported;
            string hash = Hash(bytes);
            if (hash == OriginalHash) return PatchState.Disabled;
            if (hash == PatchedHash) return PatchState.Enabled;
            return PatchState.Unsupported;
        }

        private static string Hash(byte[] bytes)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }

        private void EnsureBackup(byte[] original)
        {
            if (File.Exists(_backupPath))
            {
                ReadBackup();
                return;
            }

            bool created = false;
            try
            {
                using (var backup = new FileStream(_backupPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    created = true;
                    backup.Write(original, 0, original.Length);
                    backup.Flush(true);
                }
                ReadBackup();
            }
            catch
            {
                // Remove only an incomplete backup created by this operation.
                if (created)
                {
                    try { File.Delete(_backupPath); }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
                throw;
            }
        }

        private byte[] ReadBackup()
        {
            if (!File.Exists(_backupPath))
                throw new IOException("The original backup is missing: " + _backupPath);
            using (var backup = new FileStream(_backupPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                byte[] bytes = ReadImage(backup);
                if (Hash(bytes) != OriginalHash)
                    throw new IOException("The original backup did not pass verification: " + _backupPath);
                return bytes;
            }
        }

        private static void RequireClientsClosed()
        {
            bool running = false;
            foreach (string name in new[] { "AnarchyOnline", "Anarchy" })
            {
                Process[] processes = Process.GetProcessesByName(name);
                try { running |= processes.Length != 0; }
                finally { foreach (var process in processes) process.Dispose(); }
            }
            if (running)
                throw new InvalidOperationException("Close all AO game clients and the AO game launcher before changing this option.");
        }
    }
}
