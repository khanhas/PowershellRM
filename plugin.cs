using Rainmeter;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Threading;
using System.Management.Automation.Runspaces;

namespace PowershellRM
{
    internal class Measure
    {
        internal API rmAPI;
        internal string measureName;

        internal static string baseLine = "line";

        internal Runspace runspace = null;

        public string script = "";
        public bool isScriptFromFile = false;
        public string output = "";
        public PipelineState lastState = PipelineState.NotStarted;

        internal virtual void Reload()
        {
            if (!isScriptFromFile)
            {
                script = "";
                int lineNum = 1;
                string command = rmAPI.ReadString(baseLine, "");

                if (string.IsNullOrEmpty(command))
                {
                    rmAPI.Log(API.LogType.Error, "Command not found.");
                    return;
                }

                while (!string.IsNullOrEmpty(command))
                {
                    script += command + "\n";
                    ++lineNum;
                    command = rmAPI.ReadString(baseLine + lineNum, "");
                }
            }
        }

        internal virtual void Dispose()
        {
        }

        internal virtual double Update()
        {
            Invoke();
            double.TryParse(output, out double result);
            return result;
        }

        internal string GetString()
        {
            return output;
        }

        internal void ExecuteBang(string args)
        {
            if (args.Length > 0)
            {
                try
                {
                    using (Pipeline pipe = runspace.CreatePipeline())
                    {
                        pipe.Commands.AddScript(args);
                        pipe.Invoke();
                    }
                }
                catch (Exception e)
                {
                    rmAPI.Log(API.LogType.Error, e.ToString());
                }
            }
        }

        internal void Invoke()
        {
            try
            {
                if (runspace == null)
                {
                    throw new Exception("Could not found Runspace");
                }

                using (Pipeline pipe = runspace.CreatePipeline())
                {
                    if (isScriptFromFile)
                    {
                        pipe.Commands.Add("Update");
                    }
                    else
                    {
                        pipe.Commands.AddScript(script);
                    }
                    Collection<PSObject> outputCollection = pipe.Invoke();

                    rmAPI.LogF(API.LogType.Debug, "Done. State: {0}", pipe.PipelineStateInfo.State);
                    lastState = pipe.PipelineStateInfo.State;

                    PSObject lastObject = null;
                    foreach (PSObject outputItem in outputCollection)
                    {
                        if (outputItem != null)
                        {
                            lastObject = outputItem;
                        }
                    }

                    if (lastObject != null)
                    {
                        output = lastObject.BaseObject.ToString();
                    }
                }
            }
            catch (Exception e)
            {
                rmAPI.Log(API.LogType.Error, e.ToString());
            }
        }
    }

    internal class ParentMeasure : Measure
    {
        internal IntPtr Skin;

        internal static List<ParentMeasure> ParentMeasures = new List<ParentMeasure>();

        internal ParentMeasure(API api)
        {
            rmAPI = api;
            measureName = api.GetMeasureName().ToLowerInvariant();
            Skin = api.GetSkin();

            InitialSessionState initState = InitialSessionState.CreateDefault();
            initState.ApartmentState = System.Threading.ApartmentState.MTA;
            initState.ThreadOptions = PSThreadOptions.UseNewThread;
            switch(rmAPI.ReadString("ExecutionPolicy", "").ToLowerInvariant())
            {
                case "unrestricted":
                    initState.ExecutionPolicy = Microsoft.PowerShell.ExecutionPolicy.Unrestricted;
                    break;
                case "remotesigned":
                    initState.ExecutionPolicy = Microsoft.PowerShell.ExecutionPolicy.RemoteSigned;
                    break;
                case "allsigned":
                    initState.ExecutionPolicy = Microsoft.PowerShell.ExecutionPolicy.AllSigned;
                    break;
                case "restricted":
                    initState.ExecutionPolicy = Microsoft.PowerShell.ExecutionPolicy.Restricted;
                    break;
                case "bypass":
                    initState.ExecutionPolicy = Microsoft.PowerShell.ExecutionPolicy.Bypass;
                    break;
                case "undefined":
                    initState.ExecutionPolicy = Microsoft.PowerShell.ExecutionPolicy.Undefined;
                    break;
                default:
                    initState.ExecutionPolicy = Microsoft.PowerShell.ExecutionPolicy.Default;
                    break;
            }

            runspace = RunspaceFactory.CreateRunspace(initState);
            runspace.Open();

            runspace.SessionStateProxy.Path.SetLocation(rmAPI.ReplaceVariables("#CURRENTPATH#"));

            string filePath = rmAPI.ReadPath("ScriptFile", "");
            if (!string.IsNullOrEmpty(filePath))
            {
                
                if (!System.IO.File.Exists(filePath))
                {
                    rmAPI.Log(API.LogType.Error, "Script file does not exist.");
                }
                else
                {
                    isScriptFromFile = true;
                    script = System.IO.File.ReadAllText(filePath);

                    try
                    {
                        using (Pipeline pipe = runspace.CreatePipeline())
                        {
                            pipe.Commands.AddScript(script);
                            pipe.Invoke();
                        }
                    }
                    catch (Exception e)
                    {
                        rmAPI.Log(API.LogType.Error, e.ToString());
                    }
                }
            }

            ParentMeasures.Add(this);
        }

        internal override void Dispose()
        {

            runspace.Dispose();
            ParentMeasures.Remove(this);
        }
    }

    internal class ChildMeasure : Measure
    {
        private ParentMeasure parent;

        internal ChildMeasure(API api)
        {
            rmAPI = api;
            string parentName = api.ReadString("ParentName", "").ToLowerInvariant();
            IntPtr skin = api.GetSkin();
            measureName = api.GetMeasureName();

            // Find parent using name AND the skin handle to be sure that it's the right one.
            parent = null;
            foreach (ParentMeasure parentMeasure in ParentMeasure.ParentMeasures)
            {
                if (parentMeasure.Skin.Equals(skin) && parentMeasure.measureName.Equals(parentName))
                {
                    parent = parentMeasure;
                    break;
                }
            }

            if (parent == null)
            {
                api.Log(API.LogType.Error, "PowershellRM.dll: ParentName=" + parentName + " not valid");
                return;
            }

            runspace = parent.runspace;
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
