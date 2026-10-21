using System;
using System.Runtime.InteropServices;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class Variant_c
    {
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate int AsInt32Delegate(IntPtr pThis);

        private static readonly Lazy<AsInt32Delegate> _asInt32 =
            new Lazy<AsInt32Delegate>(ResolveAsInt32);

        public static int AsInt32(IntPtr pThis) => _asInt32.Value(pThis);

        private static AsInt32Delegate ResolveAsInt32()
        {
            IntPtr module = Kernel32.GetModuleHandle("Utils.dll");
            if (module == IntPtr.Zero)
                throw new DllNotFoundException("Utils.dll is not loaded.");

            // Both clients return a 32-bit integer, with different MSVC return-type names.
            IntPtr address = Kernel32.GetProcAddress(module, "?AsInt32@Variant@@QBEJXZ");
            if (address == IntPtr.Zero)
                address = Kernel32.GetProcAddress(module, "?AsInt32@Variant@@QBEHXZ");
            if (address == IntPtr.Zero)
                throw new EntryPointNotFoundException("Utils.dll exports neither supported Variant.AsInt32 entry point.");

            return Marshal.GetDelegateForFunctionPointer<AsInt32Delegate>(address);
        }

        [DllImport("Utils.dll", EntryPoint = "?AsFloat@Variant@@QBEMXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern float AsFloat(IntPtr pThis);

        [DllImport("Utils.dll", EntryPoint = "?AsDouble@Variant@@QBENXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern double AsDouble(IntPtr pThis);

        [return: MarshalAs(UnmanagedType.U1)]
        [DllImport("Utils.dll", EntryPoint = "?AsBool@Variant@@QBE_NXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool AsBool(IntPtr pThis);

        [DllImport("Utils.dll", EntryPoint = "?AsString@Variant@@QBE?AV?$basic_string@DU?$char_traits@D@std@@V?$allocator@D@2@@std@@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr AsString(IntPtr pThis, IntPtr pStr);

        [DllImport("Utils.dll", EntryPoint = "?SetBool@Variant@@QAEX_N@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void SetBool(IntPtr pThis, bool value);

        [return: MarshalAs(UnmanagedType.U1)]
        [DllImport("Utils.dll", EntryPoint = "?SaveToString@Variant@@QAE_NAAV?$basic_string@DU?$char_traits@D@std@@V?$allocator@D@2@@std@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool SaveToString(IntPtr pThis, IntPtr pStr);

        [return: MarshalAs(UnmanagedType.U1)]
        [DllImport("Utils.dll", EntryPoint = "?LoadFromString@Variant@@QAE_NPBD@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool LoadFromString(IntPtr pThis, [MarshalAs(UnmanagedType.LPStr)] string value);

        [DllImport("Utils.dll", EntryPoint = "??0Variant@@QAE@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr Constructor(IntPtr pThis);

        [DllImport("Utils.dll", EntryPoint = "??0Variant@@QAE@H@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr Constructor(IntPtr pThis, int value);

        [DllImport("Utils.dll", EntryPoint = "??0Variant@@QAE@M@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr Constructor(IntPtr pThis, float value);

        [DllImport("Utils.dll", EntryPoint = "??0Variant@@QAE@N@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr Constructor(IntPtr pThis, double value);

        [DllImport("Utils.dll", EntryPoint = "??0Variant@@QAE@_N@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr Constructor(IntPtr pThis, bool value);

        [DllImport("Utils.dll", EntryPoint = "??1Variant@@QAE@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern int Deconstructor(IntPtr pThis);
    }
}
