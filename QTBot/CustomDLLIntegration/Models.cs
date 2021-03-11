using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace QTBot.CustomDLLIntegration
{

    public class DLLIntegrationModel
    {
        public DLLIntegrationModel() { }

        public DLLIntegrationModel(DLLIntegratrionInterface dllI, DLLStartup startup)
        {
            dllIntegration = dllI;
            dllProperties = startup;
        }

        public DLLIntegratrionInterface dllIntegration { get; }
        public DLLStartup dllProperties { get; }
    }

    public class IntegrationStartup
    {
        public IntegrationStartup() { }

        public IntegrationStartup(List<string> dllPaths)
        {
            dllsToStart = new List<DLLStartup>();
            foreach(string dllPath in dllPaths)
            {
                if(Path.GetExtension(dllPath) == ".dll")
                {
                    dllsToStart.Add(new DLLStartup(dllPath));
                }
            }
        }

        public List<DLLStartup> dllsToStart { get; } = new List<DLLStartup>();
    }

    public class DLLStartup
    {
        public DLLStartup() { }

        public DLLStartup(string filePath) : this(filePath, false, Guid.NewGuid())
        {
        }

        public DLLStartup(string filePath, bool enabled, Guid guidID)
        {
            dllName = Path.GetFileNameWithoutExtension(filePath);
            isEnabled = enabled;
            dllPath = filePath;
            dllGuidID = guidID;
        }

        public string dllName { get; } = "";
        public string dllPath { get; } = "";
        public bool isEnabled { get; set; } = false;
        public Guid dllGuidID { get; }
    }
}
