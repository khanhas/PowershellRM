using Rainmeter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Threading;

namespace PowershellRM
{
    internal class PSHostProxy : PSHost
    {
        private Guid _hostId = Guid.NewGuid();

        public PSHostProxy()
        {
            Ui = new PSHostUIProxy();
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

        internal PSHostUIProxy Ui { get; set; }

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

    internal class PSHostUIProxy : PSHostUserInterface
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

    internal class APIProxy
    {
        private API _rm;

        public APIProxy(API rm)
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
