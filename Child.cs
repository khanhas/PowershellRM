using Rainmeter;
using System;
using System.Management.Automation.Runspaces;

namespace PowershellRM
{
    internal class ChildMeasure : Measure
    {
        private ParentMeasure parent;

        internal ChildMeasure(API api)
        {
            rmAPI = api;
            proxy = new APIProxy(api);

            string parentName = api.ReadString("Parent", "").ToLowerInvariant();
            // Supports parent/children across skins.
            string parentSkin = api.ReadString("ParentSkin", api.GetSkinName()).ToLowerInvariant();
            string skin = api.GetSkinName().ToLowerInvariant();

            // Find parent using measure name AND the skin name to be sure that it's the right one.
            parent = null;
            foreach (ParentMeasure parentMeasure in ParentMeasure.ParentMeasures)
            {
                if (parentMeasure.skin.Equals(skin) &&
                    parentMeasure.measureName.Equals(parentName))
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

        internal override void Reload()
        {
            if (parent == null)
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
                }
                catch (Exception e)
                {
                    rmAPI.Log(API.LogType.Error, e.ToString());
                }
            }
        }

        internal override double Update()
        {
            if (parent == null) return 0;

            if (parent.state == State.NotReady) return 0;

            Invoke();
            return outputNumber;
        }

        internal override string GetString()
        {
            return outputString;
        }

        internal override void SetRmAPI()
        {
            runspace.SessionStateProxy.SetVariable("RMAPI", proxy);
            parent.rmPSHost.Ui.RainmeterAPI = rmAPI;
        }

        internal override string SectionInvoke(string[] args)
        {
            if (runspace == null)
            {
                return null;
            }

            return base.SectionInvoke(args);
        }

        internal override string SectionGetVariable(string variableName, string defaulValue)
        {
            if (runspace == null)
            {
                return defaulValue;
            }

            return base.SectionGetVariable(variableName, defaulValue);
        }

        internal override string SectionExpand(string input)
        {
            if (runspace == null)
            {
                return null;
            }

            return base.SectionExpand(input);
        }
    }
}
