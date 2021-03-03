using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using QTBot.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace QTBot.CustomDLLIntegration
{
    public static class IntegrationHelper
    {
        //dll integration variables
        private static List<DLLIntegrationModel> _DLLIntegratrions = new List<DLLIntegrationModel>();
        private static IntegrationStartup _IntegrationStartup;

        /// <summary>
        /// This method is responsible for starting an thread that handles all DLL integration 
        /// </summary>
        public static void SetupDLLIntegration()
        {
            string dllDirectoryPath = "";
            string dllSartupJSONPath = "";
            try
            {
                if (Directory.Exists(dllDirectoryPath) == false)
                {
                    Directory.CreateDirectory(dllDirectoryPath);
                }

                List<string> filePaths = Directory.GetFiles(dllDirectoryPath).ToList<string>();
                if (filePaths.Count > 0)
                {
                    if (File.Exists(dllSartupJSONPath) == false)
                    {
                        //DLLs detected but no startup file was found. Add new file with all DLLs disabled.  
                        _IntegrationStartup = new IntegrationStartup(filePaths);
                        File.WriteAllText(dllSartupJSONPath, JsonConvert.SerializeObject(_IntegrationStartup));
                    }
                    else
                    {
                        //Startup file found continue with setup

                        //*warning* this way of reading the file will fail if there is a large amount of DLLs to integrate (more than 1000). ReadAllText puts the whole file into ram
                        string StartupJSON = File.ReadAllText(dllSartupJSONPath);
                        try
                        {
                            _IntegrationStartup = JsonConvert.DeserializeObject(StartupJSON) as IntegrationStartup;
                            if (_IntegrationStartup != null)
                            {
                                ReadAndStartDLLIntegration(_IntegrationStartup);
                            }
                            else
                            {
                                Utilities.Log(LogLevel.Error, $"Could not deserialize json. JSON: {StartupJSON}");
                                Utilities.ShowMessage($"There was an issue reading the JSON file located at: {dllSartupJSONPath}. Please make sure the JSON is valid, or delete the file to reset the DLL Integration settings.", "DLL Integration Startup JSON Error");
                                return;
                            }
                        }
                        catch (Exception e)
                        {
                            Utilities.Log(LogLevel.Error, $"Message: {e.Message} Stack: {e.StackTrace}");
                            Utilities.ShowMessage($"There was an issue reading the JSON file located at: {dllSartupJSONPath}. Please make sure the JSON is valid, or delete the file to reset the DLL Integration settings.", "DLL Integration Startup JSON Error");
                            return;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Utilities.Log(LogLevel.Error, $"Message: {e.Message} Stack: {e.StackTrace}");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="integrationStartup"></param>
        private static void ReadAndStartDLLIntegration(IntegrationStartup integrationStartup)
        {
            foreach (DLLStartup dLLStartup in integrationStartup.dllsToStart)
            {
                AddDLLToIntegration(dLLStartup);
            }

            foreach(DLLIntegrationModel integrationModel in _DLLIntegratrions)
            {
                if (integrationModel.dllProperties.isEnabled)
                {
                    integrationModel.dllIntegratrion.SendLogMessage += DLLIntegratrion_LogMessage;
                }
            }
        }

        /// <summary>
        /// Handles the logging of any integrated dll
        /// </summary>
        /// <param name="integrationName">DLL Name</param>
        /// <param name="level">Log Level</param>
        /// <param name="message">Log Message</param>
        private static void DLLIntegratrion_LogMessage(string integrationName, LogLevel level, string message)
        {
            Utilities.Log(level, $"*{integrationName}* - {message}");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dLLStartup"></param>
        public static void AddDLLToIntegration(DLLStartup dLLStartup)
        {
            try
            {
                var DLL = Assembly.LoadFile(dLLStartup.dllPath);
                foreach (Type type in DLL.GetExportedTypes())
                {
                    var dllClass = Activator.CreateInstance(type);
                    DLLIntegratrionInterface dLLIntegratrion = dllClass as DLLIntegratrionInterface;
                    if (dLLIntegratrion != null)
                    {
                        _DLLIntegratrions.Add(new DLLIntegrationModel(dLLIntegratrion, dLLStartup));
                    }
                }
            }
            catch (Exception e)
            {
                Utilities.Log(LogLevel.Error, $"Could not add integration {dLLStartup.dllName}.");
                Utilities.Log(LogLevel.Error, $"Message: {e.Message} Stack: {e.StackTrace}");
                DisableDLL(dLLStartup.dllPath);
            }
        }

        public static void EnableDLL(Guid integrationGuidID)
        {
            //enable the DLL in the UI and run the DLLs onstartup method
        }

        public static void DisableDLL(string dllFilePath)
        {
            //disable the dll in both the json file and UI
        }
    }
}
