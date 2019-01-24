using Microsoft.PowerShell;
using Rainmeter;
using System;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;

namespace PowershellRM
{
    internal enum ScriptType
    {
        NotValid,
        FileNoUpdate,
        File,
        Line
    }

    internal enum State
    {
        NotReady,
        Ready
    }

    internal class Measure
    {
        internal API rmAPI;
        internal APIProxy proxy;
        internal Runspace runspace;
        internal Command script;

        internal double outputNumber;
        internal string outputString;

        internal ScriptType type = ScriptType.NotValid;

        internal virtual void SetRmAPI() { }

        internal virtual void Dispose() { }

        internal virtual double Update() { return 0; }

        internal virtual string GetString() { return null; }

        internal virtual void Reload() { }

        internal InitialSessionState CreateSessionState()
        {
            InitialSessionState initState = InitialSessionState.CreateDefault();
            initState.ApartmentState = ApartmentState.MTA;
            initState.ThreadOptions = PSThreadOptions.ReuseThread;

            string executionPolicy = rmAPI.ReadString("ExecutionPolicy", null);
            if (!string.IsNullOrEmpty(executionPolicy))
            {
                try
                {
                    initState.ExecutionPolicy = (ExecutionPolicy)Enum.Parse(
                        typeof(ExecutionPolicy),
                        executionPolicy,
                        true
                    );
                }
                catch
                {
                    rmAPI.LogF(
                        API.LogType.Warning,
                        "Unknown ExecutionPolicy: \"{0}\". Using \"Default\".",
                        executionPolicy
                    );
                    initState.ExecutionPolicy = ExecutionPolicy.Default;
                }
            }

            return initState;
        }

        internal void PrepareEnvironment()
        {
            SetRmAPI();
            runspace.SessionStateProxy
                .Path.SetLocation(rmAPI.ReplaceVariables("#CURRENTPATH#"));
        }

        internal void Invoke()
        {
            if (type == ScriptType.NotValid ||
                type == ScriptType.FileNoUpdate)
            {
                return;
            }

            using (Pipeline pipe = runspace.CreatePipeline())
            {
                SetRmAPI();

                pipe.Commands.Add(script);

                Collection<PSObject> outputCollection;

                try
                {
                    outputCollection = pipe.Invoke();
                }
                catch (Exception e)
                {
                    rmAPI.Log(API.LogType.Error, e.ToString());
                    return;
                }

                object lastObject = null;

                for (int i = outputCollection.Count - 1; i >= 0; i--)
                {
                    if (outputCollection[i] != null)
                    {
                        lastObject = outputCollection[i].BaseObject;
                        break;
                    }
                }

                if (lastObject != null)
                {
                    outputString = lastObject.ToString();
                    try
                    {
                        outputNumber = Convert.ToDouble(lastObject);
                    }
                    catch
                    {
                        outputNumber = 0;
                    }
                }
            }
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

        internal Command GetCommandFromLine()
        {
            const string baseLine = "line";

            string rawScript = "";
            string command = rmAPI.ReadString(baseLine, null, false);

            if (string.IsNullOrEmpty(command))
            {
                type = ScriptType.NotValid;
                rmAPI.Log(API.LogType.Error, "Command not found.");
            }
            else
            {
                int lineNum = 1;
                while (!string.IsNullOrEmpty(command))
                {
                    rawScript += command + "\n";
                    ++lineNum;
                    command = rmAPI.ReadString(baseLine + lineNum, null, false);
                }

                type = ScriptType.Line;
            }

            return new Command(rawScript, true);
        }

        internal virtual string SectionInvoke(string command)
        {
            if (runspace.RunspaceAvailability != RunspaceAvailability.Available)
            {
                return "";
            }

            using (Pipeline pipe = runspace.CreatePipeline())
            {
                pipe.Commands.AddScript(command);
                try
                {
                    var outputCollection = pipe.Invoke();
                    object lastObject = null;

                    for (int i = outputCollection.Count - 1; i >= 0; i--)
                    {
                        if (outputCollection[i] != null)
                        {
                            lastObject = outputCollection[i].BaseObject;
                            break;
                        }
                    }

                    if (lastObject != null)
                    {
                        return lastObject.ToString();
                    }
                }
                catch (Exception e)
                {
                    rmAPI.Log(API.LogType.Error, e.Message);
                }
            }

            return null;
        }

        internal virtual string SectionGetVariable(string variableName, string defaulValue)
        {
            if (runspace.RunspaceAvailability != RunspaceAvailability.Available)
            {
                return defaulValue;
            }

            try
            {
                object value = runspace.SessionStateProxy.GetVariable(variableName);
                return value.ToString();
            }
            catch (Exception e)
            {
                rmAPI.Log(API.LogType.Error, e.Message);
            }

            return defaulValue;
        }

        internal virtual string SectionExpand(string input)
        {
            if (runspace.RunspaceAvailability != RunspaceAvailability.Available)
            {
                return input;
            }

            try
            {
                string value = runspace
                    .SessionStateProxy
                    .InvokeCommand
                    .ExpandString(input);
                return value;
            }
            catch (Exception e)
            {
                rmAPI.Log(API.LogType.Error, e.Message);
            }

            return input;
        }
    }
}
