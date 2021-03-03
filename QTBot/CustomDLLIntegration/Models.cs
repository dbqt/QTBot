﻿
using System;
using System.Collections.Generic;
using System.IO;

namespace QTBot.CustomDLLIntegration
{
    public class SettingsUI
    {
    }

    public class TwitchData
    {
    }

    public class StartupData
    {

        public int waitPeriodInSeconds { get; }
    }

    public class DLLIntegrationModel
    {
        public DLLIntegrationModel(DLLIntegratrionInterface dllI, DLLStartup startup)
        {
            dllIntegratrion = dllI;
            dllProperties = startup;
        }

        public DLLIntegratrionInterface dllIntegratrion { get; }
        public DLLStartup dllProperties { get; }
    }

    public class IntegrationStartup
    {
        public IntegrationStartup(List<string> dllPaths)
        {
            dllsToStart = new List<DLLStartup>();
            foreach(string dllPath in dllPaths)
            {
                if(Path.GetExtension(dllPath) == "dll")
                {
                    dllsToStart.Add(new DLLStartup(dllPath));
                }                
            }
        }

        public List<DLLStartup> dllsToStart {get;}
    }

    public class DLLStartup
    {
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

        public string dllName { get; }
        public string dllPath { get; }
        public bool isEnabled { get; set; }
        public Guid dllGuidID { get; }
    }
}
