using Rainmeter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

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

        internal virtual void Dispose()
        {
        }

        internal virtual void SetRmAPI()
        {
        }

        internal void Reload()
        {
            if (!isScriptFromFile)
            {
                script = "";
                int lineNum = 1;
                string command = rmAPI.ReadString(baseLine, "", false);

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

        internal double Update()
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
                    SetRmAPI();

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

        public RmPSHost rmPSHost;

        internal ParentMeasure(API api)
        {
            rmAPI = api;
            measureName = api.GetMeasureName().ToLowerInvariant();
            Skin = api.GetSkin();

            InitialSessionState initState = InitialSessionState.CreateDefault();
            initState.ApartmentState = System.Threading.ApartmentState.MTA;
            initState.ThreadOptions = PSThreadOptions.ReuseThread;

            switch (rmAPI.ReadString("ExecutionPolicy", "").ToLowerInvariant())
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

            rmPSHost = new RmPSHost();

            runspace = RunspaceFactory.CreateRunspace(rmPSHost, initState);

            runspace.Open();

            SetRmAPI();
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

        internal override void SetRmAPI()
        {
            runspace.SessionStateProxy.SetVariable("RMAPI", rmAPI);
            rmPSHost.RainmeterAPI = rmAPI;
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

        internal override void SetRmAPI()
        {
            runspace.SessionStateProxy.SetVariable("RMAPI", rmAPI);
            parent.rmPSHost.RainmeterAPI = rmAPI;
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

    internal class RmPSHost : PSHost
    {
        private Guid _hostId = Guid.NewGuid();
        private API _rmAPI;

        public RmPSHost()
        {
            Ui = new RmPSHostUserInterface();
        }

        public API RainmeterAPI
        {
            get { return _rmAPI; }
            set
            {
                _rmAPI = value;
                Ui.RainmeterAPI = value;
            }
        }

        public override Guid InstanceId
        {
            get { return _hostId; }
        }

        public override string Name
        {

            get { return "RainmeterPSHost"; }
        }

        public override Version Version
        {
            get { return new Version(1, 0); }
        }

        public override PSHostUserInterface UI
        {
            get { return Ui; }
        }

        public override CultureInfo CurrentCulture
        {
            get { return Thread.CurrentThread.CurrentCulture; }
        }

        public override CultureInfo CurrentUICulture
        {
            get { return Thread.CurrentThread.CurrentUICulture; }
        }

        internal RmPSHostUserInterface Ui { get; set; }

        public override void EnterNestedPrompt()
        {
            throw new NotImplementedException();
        }

        public override void ExitNestedPrompt()
        {
            throw new NotImplementedException();
        }

        public override void NotifyBeginApplication()
        {
            return;
        }

        public override void NotifyEndApplication()
        {
            return;
        }

        public override void SetShouldExit(int exitCode)
        {
            return;
        }
    }

    internal class RmPSHostUserInterface : PSHostUserInterface
    {
        public API RainmeterAPI { get; set; }

        public override void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
        {
            RainmeterAPI.Log(API.LogType.Notice, value);
        }

        public override void Write(string value)
        {
            RainmeterAPI.Log(API.LogType.Notice, value);
        }

        public override void WriteDebugLine(string message)
        {
            RainmeterAPI.Log(API.LogType.Debug, message);
        }

        public override void WriteErrorLine(string value)
        {
            RainmeterAPI.Log(API.LogType.Error, value);
        }

        public override void WriteLine(string value)
        {
            RainmeterAPI.Log(API.LogType.Notice, value);
        }

        public override void WriteVerboseLine(string message)
        {
            RainmeterAPI.Log(API.LogType.Notice, message);
        }

        public override void WriteWarningLine(string message)
        {
            RainmeterAPI.Log(API.LogType.Warning, message);
        }

        public override void WriteProgress(long sourceId, ProgressRecord record)
        {
            return;
        }

        public string Output
        {
            get { return null; }
        }

        public override Dictionary<string, PSObject> Prompt(string caption, string message, Collection<FieldDescription> descriptions)
        {
            throw new NotImplementedException();
        }

        public override int PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices, int defaultChoice)
        {
            throw new NotImplementedException();
        }

        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName, PSCredentialTypes allowedCredentialTypes, PSCredentialUIOptions options)
        {
            throw new NotImplementedException();
        }

        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName)
        {
            throw new NotImplementedException();
        }

        public override PSHostRawUserInterface RawUI
        {
            get { return null; }
        }

        public override string ReadLine()
        {
            throw new NotImplementedException();
        }

        public override System.Security.SecureString ReadLineAsSecureString()
        {
            throw new NotImplementedException();
        }
    }
}
