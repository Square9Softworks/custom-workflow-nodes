using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Square9.CustomNode;

namespace ErrorImport
{
    public class CaptureImport : CaptureImporter
    {
        public override List<string> Import()
        {
            ErrorImporter.Import(Database, Workflow, true, Settings.GetStringSetting("TargetNode"));
            return new List<string>();
        }
    }
    public class ActionImport : ActionImporter
    {
        public override List<GlobalSearchDocument> Import()
        {
            ErrorImporter.Import(Database, Workflow, false, Settings.GetStringSetting("TargetNode"));
            
            return new List<GlobalSearchDocument>();
        }
    }

    public class ErrorImporter
    {
        public static void Import(Database db, dynamic workflow, bool capture, string targetNode)
        {
            var query = new List<ProcessFilter>();

            query.Add(new StatusFilter(ProcessStatus.Errored));
            query.Add(new ProcessTypeFilter(capture ? ProcessType.GlobalCapture : ProcessType.GlobalAction));
            query.Add(new WorkflowNameFilter(workflow.Name.Value));

            var processes = db.GetProcessesByQuery(query,QueryOperator.And);
        
            if (!String.IsNullOrEmpty(targetNode))
            {
                var targetedProcesses = new List<Process>();
                foreach (var process in processes)
                {
                    var history = (JArray)process.GetProcessDynamic().History;

                    if (history.Last["Node"].ToString() == targetNode)
                        targetedProcesses.Add(process);
                }
                processes = targetedProcesses;
            }
            
            if(processes.Count > 0)
            {
                string nodeKey = "";
                var nodes = (JObject)workflow["Nodes"];
                foreach(var node in nodes)
                {
                    if(node.Value["Category"].ToString() == "1aac6ca6-4d37-4d68-85ba-90243e390308")
                    {
                        if (node.Value["Settings"]["TargetNode"]["StringValue"].ToString() == targetNode)
                        {
                            nodeKey = node.Key;
                            break;
                        }
                    }
                }

                if(!String.IsNullOrEmpty(nodeKey))
                {
                    foreach(var process in processes)
                    {
                        process.AddHistory("Error Document Recovered");
                        process.SetStatus(ProcessStatus.Ready);
                        var dynamicProcess = process.GetProcessDynamic();
                        dynamicProcess.CurrentNode.Value = nodeKey;
                        process.SaveProcessDynamic(dynamicProcess);
                        db.UpdateProcess(process);
                    }
                }
            }
        }
    }
}
