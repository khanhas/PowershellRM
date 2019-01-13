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
using System.Threading.Tasks;

namespace PowershellRM
{
    internal enum ScriptTypes
    {
        NOTVALID,
        FILENOUPDATE,
        FILE,
        LINE
    }

    internal enum State
    {
        NOTREADY,
        READY
    }

    internal class Measure
    {
        internal static string baseLine = "line";

        internal API rmAPI;
        internal string measureName;
        internal Runspace runspace;
        internal ScriptTypes scriptType = ScriptTypes.NOTVALID;
        internal string script;
        internal string output;
        internal RmAPIWrapper apiWrapper;
        internal State state = State.NOTREADY;

        internal virtual void SetRmAPI() { }

        internal virtual void Dispose() { }

        internal virtual State GetState() { return state; }

        internal void Reload()
        {
            if (scriptType != ScriptTypes.NOTVALID
             && scriptType != ScriptTypes.LINE)
            {
                return;
            }

            state = State.READY;
            script = "";
            int lineNum = 1;
            string command = rmAPI.ReadString(baseLine, "", false);

            if (string.IsNullOrEmpty(command))
            {
                scriptType = ScriptTypes.NOTVALID;
                rmAPI.Log(API.LogType.Error, "Command not found.");
                return;
            }

            while (!string.IsNullOrEmpty(command))
            {
                script += command + "\n";
                ++lineNum;
                command = rmAPI.ReadString(baseLine + lineNum, "", false);
            }

            scriptType = ScriptTypes.LINE;
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
            if (string.IsNullOrEmpty(args))
            {
                return;
            }

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

        internal void Invoke()
        {
            if (GetState() != State.READY)
            {
                return;
            }

            if (scriptType == ScriptTypes.NOTVALID
             || scriptType == ScriptTypes.FILENOUPDATE)
            {
                return;
            }

            try
            {
                using (Pipeline pipe = runspace.CreatePipeline())
                {
                    SetRmAPI();

                    switch (scriptType)
                    {
                        case ScriptTypes.FILE:
                            pipe.Commands.Add("Update");
                            break;
                        case ScriptTypes.LINE:
                            pipe.Commands.AddScript(script);
                            break;
                    }

                    Collection<PSObject> outputCollection = pipe.Invoke();

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
        internal static List<ParentMeasure> ParentMeasures = new List<ParentMeasure>();
        internal IntPtr Skin;
        internal RmPSHost rmPSHost;

        internal ParentMeasure(API api)
        {
            rmAPI = api;
            apiWrapper = new RmAPIWrapper(api);

            measureName = api.GetMeasureName().ToLowerInvariant();
            Skin = api.GetSkin();

            InitialSessionState initState = InitialSessionState.CreateDefault();
            initState.ApartmentState = System.Threading.ApartmentState.STA;
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

            ParentMeasures.Add(this);

            string filePath = rmAPI.ReadPath("ScriptFile", "");
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            if (!System.IO.File.Exists(filePath))
            {
                rmAPI.Log(API.LogType.Error, "Script file does not exist.");
                return;
            }

            scriptType = ScriptTypes.FILE;

            Task.Run(() =>
            {
                script = System.IO.File.ReadAllText(filePath);
                using (Pipeline pipe = runspace.CreatePipeline())
                {
                    pipe.Commands.AddScript(script);

                    try
                    {
                        pipe.Invoke();

                        CommandInfo updateFuncInfo = runspace.SessionStateProxy.InvokeCommand.GetCommand("Update", CommandTypes.Function);
                        if (updateFuncInfo == null)
                        {
                            rmAPI.Log(API.LogType.Debug, "Could not find Update function in script file.");
                            scriptType = ScriptTypes.FILENOUPDATE;
                        }
                    }
                    catch (Exception e)
                    {
                        rmAPI.Log(API.LogType.Error, e.ToString());
                        scriptType = ScriptTypes.NOTVALID;
                    }
                }

                state = State.READY;
            });
        }

        internal override void Dispose()
        {
            CommandInfo finalizeFuncInfo = runspace.SessionStateProxy
                .InvokeCommand.GetCommand("Finalize", CommandTypes.Function);

            if (finalizeFuncInfo != null)
            {
                using (Pipeline pipe = runspace.CreatePipeline())
                {
                    pipe.Commands.Add("Finalize");
                    pipe.Invoke();
                }
            }

            runspace.Dispose();
            ParentMeasures.Remove(this);
        }

        internal override void SetRmAPI()
        {
            runspace.SessionStateProxy.SetVariable("RMAPI", apiWrapper);
            rmPSHost.Ui.RainmeterAPI = rmAPI;
        }
    }

    internal class ChildMeasure : Measure
    {
        private ParentMeasure parent;

        internal ChildMeasure(API api)
        {
            rmAPI = api;
            apiWrapper = new RmAPIWrapper(api);

            string parentName = api.ReadString("Parent", "").ToLowerInvariant();
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
                api.Log(API.LogType.Error, "PowershellRM.dll: Parent=" + parentName + " not valid");
                return;
            }

            runspace = parent.runspace;
        }

        internal override void SetRmAPI()
        {
            runspace.SessionStateProxy.SetVariable("RMAPI", apiWrapper);
            parent.rmPSHost.Ui.RainmeterAPI = rmAPI;
        }

        internal override State GetState()
        {
            return parent.state;
        }
    }

    public static class Plugin
    {
        static IntPtr StringBuffer = IntPtr.Zero;

        [DllExport]
        public static void Initialize(ref IntPtr data, IntPtr rm)
        {
            API api = new API(rm);

            string parent = api.ReadString("Parent", "");

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

        [DllExport]
        public static IntPtr Invoke(IntPtr data, int argc, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 1)] string[] argv)
        {
            if (argc <= 0)
            {
                return IntPtr.Zero;
            }

            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            if (measure.GetState() != State.READY) {
                return Marshal.StringToHGlobalUni("");
            }

            using (Pipeline pipe = measure.runspace.CreatePipeline())
            {
                for (int i = 0; i < argc; i++)
                {
                    string command = argv[i];
                    // Trim double quotes
                    if (command.StartsWith("\"") && command.EndsWith("\""))
                    {
                        command = command.Remove(command.Length - 1, 1).Remove(0, 1);
                    }
                    pipe.Commands.AddScript(command);
                }

                try
                {
                    var outputCollection = pipe.Invoke();
                    PSObject lastObject = null;
                    foreach (PSObject output in outputCollection)
                    {
                        if (output != null)
                        {
                            lastObject = output;
                        }
                    }
                    if (lastObject != null)
                    {
                        return Marshal.StringToHGlobalUni(lastObject.BaseObject.ToString());
                    }
                }
                catch (Exception e)
                {
                    measure.rmAPI.Log(API.LogType.Error, e.Message);
                }
            }

            return IntPtr.Zero;  // Do not replace the variable
        }

    }

    internal class RmPSHost : PSHost
{
    private Guid _hostId = Guid.NewGuid();

    public RmPSHost()
    {
        Ui = new RmPSHostUserInterface();
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

internal class RmAPIWrapper
{
    private API _rm;

    public RmAPIWrapper(API rm)
    {
        _rm = rm;
    }

    public void Bang(string bangs)
    {
        _rm.Execute(bangs);
    }

    public double? Measure(string measure)
    {
        string measureName = "[" + measure + ":]";
        string value = _rm.ReplaceVariables(measureName);
        if (measureName == value)
        {
            return null;
        }

        double.TryParse(value, out double result);
        return result;
    }

    public double Measure(string measure, double defaultValue)
    {
        double? value = Measure(measure);
        if (value.HasValue)
        {
            return value.Value;
        }

        return defaultValue;
    }

    public string MeasureStr(string measure)
    {
        string measureName = "[" + measure + "]";
        string value = _rm.ReplaceVariables(measureName);
        if (measureName == value)
        {
            return null;
        }

        return value;
    }

    public string MeasureStr(string measure, string defaultValue)
    {
        string value = MeasureStr(measure);
        if (value == null)
        {
            return defaultValue;
        }

        return value;
    }

    public string VariableStr(string var)
    {
        string varName = "#" + var + "#";
        string value = _rm.ReplaceVariables(varName);

        if (varName == value)
        {
            return null;
        }

        return value;
    }

    public string VariableStr(string var, string defaultValue)
    {
        string value = VariableStr(var);
        if (value == null)
        {
            return defaultValue;
        }

        return value;
    }

    public double? Variable(string var)
    {
        string value = VariableStr(var);
        if (value == null)
        {
            return null;
        }

        double.TryParse(value, out double result);
        return result;
    }

    public double Variable(string var, double defaultValue)
    {
        double? value = Variable(var);
        if (value.HasValue)
        {
            return value.Value;
        }

        return defaultValue;
    }

    public double ReplaceVariables(string input)
    {
        string value = _rm.ReplaceVariables(input);
        double.TryParse(value, out double result);
        return result;
    }

    public string ReplaceVariablesStr(string input)
    {
        return _rm.ReplaceVariables(input);
    }

    public double Option(string key, double defaultValue = 0.0)
    {
        return _rm.ReadDouble(key, defaultValue);
    }

    public int OptionInt(string key, int defaultValue = 0)
    {
        return _rm.ReadInt(key, defaultValue);
    }

    public string OptionStr(string key, string defaultValue = "")
    {
        return _rm.ReadString(key, defaultValue);
    }

    public string OptionPath(string key, string defaultValue = "")
    {
        return _rm.ReadPath(key, defaultValue);
    }

    public void Log(string message)
    {
        _rm.Log(API.LogType.Notice, message);
    }

    public void LogError(string message)
    {
        _rm.Log(API.LogType.Error, message);
    }

    public void LogWarning(string message)
    {
        _rm.Log(API.LogType.Warning, message);
    }

    public void LogDebug(string message)
    {
        _rm.Log(API.LogType.Debug, message);
    }

    public void Log(string format, params Object[] args)
    {
        _rm.LogF(API.LogType.Notice, format, args);
    }

    public void LogError(string format, params Object[] args)
    {
        _rm.LogF(API.LogType.Error, format, args);
    }

    public void LogWarning(string format, params Object[] args)
    {
        _rm.LogF(API.LogType.Warning, format, args);
    }

    public void LogDebug(string format, params Object[] args)
    {
        _rm.LogF(API.LogType.Debug, format, args);
    }

    public string GetSkinName()
    {
        return _rm.GetSkinName();
    }

    public IntPtr GetSkinHandle()
    {
        return _rm.GetSkinWindow();
    }

    public string GetMeasureName()
    {
        return _rm.GetMeasureName();
    }
}
}
