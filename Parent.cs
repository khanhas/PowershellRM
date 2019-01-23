using Rainmeter;
using Microsoft.PowerShell;
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
using System.Threading.Tasks;

namespace PowershellRM
{
    internal class ParentMeasure : Measure
    {
        internal static List<ParentMeasure> ParentMeasures = new List<ParentMeasure>();
        internal string skin;
        internal string measureName;
        internal PSHostProxy rmPSHost;
        internal State state = State.NotReady;

        internal ParentMeasure(API api)
        {
            rmAPI = api;
            proxy = new APIProxy(api);

            measureName = api.GetMeasureName().ToLowerInvariant();
            skin = api.GetSkinName().ToLowerInvariant();

            var initState = CreateSessionState();

            rmPSHost = new PSHostProxy();

            runspace = RunspaceFactory.CreateRunspace(rmPSHost, initState);
            runspace.Open();

            PrepareEnvironment();

            ParentMeasures.Add(this);

            string filePath = rmAPI.ReadPath("ScriptFile", null);
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            if (!File.Exists(filePath))
            {
                rmAPI.Log(API.LogType.Error, "Script file does not exist.");
                return;
            }

            type = ScriptType.FileNoUpdate;

            Task.Run(() =>
            {
                string rawScript = File.ReadAllText(filePath);
                using (Pipeline pipe = runspace.CreatePipeline())
                {
                    pipe.Commands.AddScript(rawScript);

                    try
                    {
                        pipe.Invoke();

                        CommandInfo updateFuncInfo = runspace.SessionStateProxy
                            .InvokeCommand.GetCommand("Update", CommandTypes.Function);

                        if (updateFuncInfo == null)
                        {
                            rmAPI.Log(API.LogType.Debug, "Could not find Update function in script file.");
                        }
                        else
                        {
                            type = ScriptType.File;
                            script = new Command("Update");
                        }
                    }
                    catch (Exception e)
                    {
                        rmAPI.Log(API.LogType.Error, e.ToString());
                        type = ScriptType.NotValid;
                    }
                }

                state = State.Ready;
            });
        }

        internal override void Reload()
        {
            if (type == ScriptType.File ||
                type == ScriptType.FileNoUpdate)
            {
                return;
            }

            script = GetCommandFromLine();
            using (Pipeline pipe = runspace.CreatePipeline())
            {
                pipe.Commands.Add(script);
                try
                {
                    pipe.Invoke();
                    state = State.Ready;
                }
                catch (Exception e)
                {
                    rmAPI.Log(API.LogType.Error, e.ToString());
                    state = State.NotReady;
                }
            }
        }

        internal override double Update()
        {
            if (state == State.NotReady)
            {
                return 0;
            }

            Invoke();
            return outputNumber;
        }

        internal override string GetString()
        {
            return outputString;
        }

        internal override void Dispose()
        {
            if (type == ScriptType.File ||
                type == ScriptType.FileNoUpdate)
            {
                CommandInfo finalizeFuncInfo = runspace.SessionStateProxy
                    .InvokeCommand.GetCommand("Finalize", CommandTypes.Function);

                if (finalizeFuncInfo != null)
                {
                    try
                    {
                        using (Pipeline pipe = runspace.CreatePipeline("Finalize"))
                        {
                            pipe.Invoke();
                        }
                    }
                    catch(Exception e)
                    {
                        rmAPI.Log(API.LogType.Error, e.ToString());
                    }
                }
            }

            runspace.Dispose();
            ParentMeasures.Remove(this);
        }

        internal override void SetRmAPI()
        {
            runspace.SessionStateProxy.SetVariable("RMAPI", proxy);
            rmPSHost.Ui.RainmeterAPI = rmAPI;
        }

        internal override string SectionInvoke(string[] args)
        {
            if (state != State.Ready)
            {
                return "";
            }

            return base.SectionInvoke(args);
        }

        internal override string SectionGetVariable(string variableName, string defaulValue)
        {
            if (state != State.Ready)
            {
                return defaulValue;
            }

            return base.SectionGetVariable(variableName, defaulValue);
        }

        internal override string SectionExpand(string input)
        {
            if (state != State.Ready)
            {
                return input;
            }

            return base.SectionExpand(input);
        }
    }
}
