using Rainmeter;
using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;

namespace PowershellRM
{
    public static class Plugin
    {
        static IntPtr StringBuffer = IntPtr.Zero;

        [DllExport]
        public static void Initialize(ref IntPtr data, IntPtr rm)
        {
            API api = new API(rm);
            Measure measure;

            int async = api.ReadInt("Async", 0);
            if (async == 1)
            {
                measure = new AsyncMeasure(api);
            }
            else
            {
                string parent = api.ReadString("Parent", null);

                if (string.IsNullOrEmpty(parent))
                {
                    measure = new ParentMeasure(api);
                }
                else
                {
                    measure = new ChildMeasure(api);
                }
            }

            data = GCHandle.ToIntPtr(GCHandle.Alloc(measure));
        }

        [DllExport]
        public static void Finalize(IntPtr data)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            measure.Dispose();

            GCHandle.FromIntPtr(data).Free();

            if (StringBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(StringBuffer);
                StringBuffer = IntPtr.Zero;
            }
        }

        [DllExport]
        public static void Reload(IntPtr data, IntPtr rm, ref double maxValue)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            measure.Reload();
        }

        [DllExport]
        public static double Update(IntPtr data)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            return measure.Update();
        }

        [DllExport]
        public static IntPtr GetString(IntPtr data)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            if (StringBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(StringBuffer);
                StringBuffer = IntPtr.Zero;
            }

            string stringValue = measure.GetString();
            if (stringValue != null)
            {
                StringBuffer = Marshal.StringToHGlobalUni(stringValue);
            }

            return StringBuffer;
        }

        [DllExport]
        public static void ExecuteBang(IntPtr data, IntPtr args)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            measure.ExecuteBang(Marshal.PtrToStringUni(args));
        }

        [DllExport]
        public static IntPtr Invoke(IntPtr data, int argc, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 1)] string[] argv)
        {
            if (argc <= 0)
            {
                return IntPtr.Zero;
            }

            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;

            string result = measure.SectionInvoke(argv);
            if (result == null)
            {
                // Do not replace the variable
                return IntPtr.Zero;
            }

            return Marshal.StringToHGlobalUni(result);
        }

        [DllExport]
        public static IntPtr Variable(IntPtr data, int argc, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 1)] string[] argv)
        {
            if (argc <= 0)
            {
                return IntPtr.Zero;
            }

            string defValue = "";
            if (argc > 1)
            {
                defValue = argv[1];
            }

            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            string result = measure.SectionGetVariable(argv[0], defValue);
            if (result == null)
            {
                // Do not replace the variable
                return IntPtr.Zero;
            }

            return Marshal.StringToHGlobalUni(result);
        }

        [DllExport]
        public static IntPtr Expand(IntPtr data, int argc, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 1)] string[] argv)
        {
            if (argc <= 0)
            {
                return IntPtr.Zero;
            }

            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            string result = measure.SectionExpand(argv[0]);

            return Marshal.StringToHGlobalUni(result);
        }
    }
}
