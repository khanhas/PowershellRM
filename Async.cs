using Microsoft.PowerShell;
using Rainmeter;
using System;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;

namespace PowershellRM
{
    internal class AsyncMeasure : Measure
    {
        PowerShell ps;

        internal AsyncMeasure(API api)
        {
            rmAPI = api;
            proxy = new APIProxy(api);

            var rmPSHost = new PSHostProxy();
            rmPSHost.Ui.RainmeterAPI = rmAPI;

            var initSS = CreateSessionState();

            runspace = RunspaceFactory.CreateRunspace(rmPSHost, initSS);

            runspace.Open();

            PrepareEnvironment();

            string filePath = rmAPI.ReadPath("ScriptFile", null);
            if (!string.IsNullOrEmpty(filePath))
            {
                if (!File.Exists(filePath))
                {
                    rmAPI.Log(API.LogType.Error, "Script file does not exist.");
                    return;
                }

                script = new Command(File.ReadAllText(filePath), true);
                return;
            }

            script = GetCommandFromLine();
        }

        internal override void Reload()
        {
            if (ps != null)
            {
                if (ps.InvocationStateInfo.State == PSInvocationState.Running)
                {
                    return;
                }

                ps.Dispose();
            }

            ps = PowerShell.Create();
            ps.Runspace = runspace;
            ps.Commands.AddCommand(script);

            ps.BeginInvoke();
        }

        internal override void Dispose()
        {
            if (ps != null)
            {
                ps.Stop();
                ps.Dispose();
            }

            runspace.Dispose();
        }

        internal override double Update()
        {
            return (double)ps.InvocationStateInfo.State;
        }

        internal override string GetString()
        {
            return ps.InvocationStateInfo.State.ToString();
        }

        internal override string SectionInvoke(string[] args)
        {
            if (ps.InvocationStateInfo.State == PSInvocationState.Running)
            {
                return null;
            }

            return base.SectionInvoke(args);
        }

        internal override string SectionGetVariable(string variableName, string defaulValue)
        {
            if (ps.InvocationStateInfo.State == PSInvocationState.Running)
            {
                return defaulValue;
            }

            return base.SectionGetVariable(variableName, defaulValue);
        }

        internal override string SectionExpand(string input)
        {
            if (ps.InvocationStateInfo.State == PSInvocationState.Running)
            {
                return input;
            }

            return base.SectionExpand(input);
        }
    }
}
