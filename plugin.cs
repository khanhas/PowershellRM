using Rainmeter;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Management.Automation;
using System.Threading;
using System.Management.Automation.Runspaces;

namespace PowershellRM
{
    internal class Measure
    {
        internal API rmAPI;
        internal string measureName;

        public string output = "";
        public PSDataCollection<PSObject> outputCollection;

        internal virtual void Dispose()
        {
        }

        internal virtual void Reload()
        {
        }

        internal virtual double Update()
        {
            double.TryParse(output, out double result);
            return result;
        }

        internal string GetString()
        {
            return output;
        }

        internal void ExecuteBang(string args)
        {
            string bang = args.ToLowerInvariant();
            if (bang.Equals("update"))
            {
                Reload();
            }
        }
    }

    internal class ParentMeasure : Measure
    {
        internal IntPtr Skin;

        internal static List<ParentMeasure> ParentMeasures = new List<ParentMeasure>();
        public PowerShell psInstance;
        public Runspace runspace;
        public PSInvocationState state = PSInvocationState.NotStarted;

        internal ParentMeasure(API api)
        {
            rmAPI = api;
            measureName = api.GetMeasureName();
            Skin = api.GetSkin();

            psInstance = PowerShell.Create();
            runspace = RunspaceFactory.CreateRunspace();
            runspace.Open();
            psInstance.Runspace = runspace;
            outputCollection = new PSDataCollection<PSObject>();

            ParentMeasures.Add(this);
        }

        internal override void Dispose()
        {
            psInstance.Dispose();
            runspace.Dispose();
            ParentMeasures.Remove(this);
        }

        internal override void Reload()
        {
            output = "";
            outputCollection.Clear();

            int lineNum = 1;
            string baseLine = "Line";
            string command = rmAPI.ReadString(baseLine, "");

            if (string.IsNullOrEmpty(command))
            {
                rmAPI.Log(API.LogType.Error, "Command not found.");
                return;
            }

            while (!string.IsNullOrEmpty(command))
            {
                psInstance.AddScript(command);
                ++lineNum;
                command = rmAPI.ReadString(baseLine + lineNum, "");
            }

            void callMeBack(IAsyncResult ar)
            {
                rmAPI.LogF(API.LogType.Debug, "{0}: Done. State: {1}", measureName, psInstance.InvocationStateInfo.State);
                state = psInstance.InvocationStateInfo.State;

                foreach (PSObject outputItem in outputCollection)
                {
                    output = outputItem.BaseObject.ToString();
                }
            }

            psInstance.BeginInvoke<PSObject, PSObject>(
                null,
                outputCollection,
                null,
                callMeBack,
                null);
        }
    }

    internal class ChildMeasure : Measure
    {
        private ParentMeasure parent;

        internal ChildMeasure(API api)
        {
            rmAPI = api;
            string parentName = api.ReadString("ParentName", "");
            IntPtr skin = api.GetSkin();
            measureName = api.GetMeasureName();

            // Find parent using name AND the skin handle to be sure that it's the right one.
            parent = null;
            foreach (ParentMeasure parentMeasure in ParentMeasure.ParentMeasures)
            {
                if (parentMeasure.Skin.Equals(skin) && parentMeasure.measureName.Equals(parentName))
                {
                    parent = parentMeasure;
                }
            }

            if (parent == null)
            {
                api.Log(API.LogType.Error, "PowershellRM.dll: ParentName=" + parentName + " not valid");
                return;
            }

            outputCollection = new PSDataCollection<PSObject>();
        }

        internal override void Reload()
        {
            PSInvocationState state = parent.psInstance.InvocationStateInfo.State;
            if (parent == null ||
                state == PSInvocationState.Failed ||
                state == PSInvocationState.Disconnected ||
                state == PSInvocationState.Stopped)
            {
                return;
            }

            if (state != PSInvocationState.Completed)
            {
                rmAPI.LogF(API.LogType.Debug, "{0}: Reloading...", rmAPI.GetMeasureName());
                Thread.Sleep(50);
                Reload();
                return;
            }

            output = "";
            int lineNum = 1;
            string baseLine = "Line";
            string command = rmAPI.ReadString(baseLine, "");

            if (string.IsNullOrEmpty(command))
            {
                rmAPI.Log(API.LogType.Error, "Command not found.");
                return;
            }

            PowerShell psInstance = parent.psInstance;

            while (!string.IsNullOrEmpty(command))
            {
                psInstance.AddScript(command);
                ++lineNum;
                command = rmAPI.ReadString(baseLine + lineNum, "");
            }

            void callMeBack(IAsyncResult ar)
            {
                rmAPI.LogF(API.LogType.Debug, "{0}: Done. State: {1}", measureName, psInstance.InvocationStateInfo.State);

                foreach (PSObject outputItem in outputCollection)
                {
                    output = outputItem.BaseObject.ToString();
                }
            }

            psInstance.BeginInvoke<PSObject, PSObject>(
                null,
                outputCollection,
                null,
                callMeBack,
                null);
        }
    }

    public static class Plugin
    {
        static IntPtr StringBuffer = IntPtr.Zero;

        [DllExport]
        public static void Initialize(ref IntPtr data, IntPtr rm)
        {
            API api = new API(rm);

            string parent = api.ReadString("ParentName", "");

            Measure measure;
            if (string.IsNullOrEmpty(parent))
            {
                measure = new ParentMeasure(api);
            }
            else
            {
                measure = new ChildMeasure(api);
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
    }
}
