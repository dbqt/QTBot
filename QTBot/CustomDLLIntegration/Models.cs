using System;
using System.Collections.Generic;
using System.IO;

namespace QTBot.CustomDLLIntegration
{
    public class DLLIntegrationModel
    {
        public DLLIntegrationModel() { }

        public DLLIntegrationModel(DLLIntegrationInterface dllI, DLLStartup startup)
        {
            DllIntegration = dllI;
            DllProperties = startup;
        }

        public DLLIntegrationInterface DllIntegration { get; }
        public DLLStartup DllProperties { get; }
    }

    public class IntegrationStartup
    {
        public IntegrationStartup() { }

        public IntegrationStartup(List<string> dllPaths)
        {
            DllsToStart = new List<DLLStartup>();
            foreach (string dllPath in dllPaths)
            {
                if (Path.GetExtension(dllPath) == ".dll")
                {
                    DllsToStart.Add(new DLLStartup(dllPath));
                }
            }
        }

        public List<DLLStartup> DllsToStart { get; set; }
    }

    public class DLLStartup
    {
        public DLLStartup() { }

        public DLLStartup(string filePath) : this(filePath, false, Guid.NewGuid())
        {
        }

        public DLLStartup(string filePath, bool enabled, Guid guidID)
        {
            DllName = Path.GetFileNameWithoutExtension(filePath);
            IsEnabled = enabled;
            DllPath = filePath;
            DllGuidID = guidID;
        }

        public string DllName { get; set; } = "";
        public string DllPath { get; set; } = "";
        public bool IsEnabled { get; set; } = false;
        public Guid DllGuidID { get; set; }
    }
}
