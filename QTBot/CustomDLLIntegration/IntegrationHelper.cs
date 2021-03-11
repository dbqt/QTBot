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
        private static string _DLLDirectoryPath = "";
        private static string _DLLSartupJSONPath = "";

        public static List<DLLIntegrationModel> GetDLLIntegrations()
        {
            return _DLLIntegratrions;
        }

        /// <summary>
        /// This method is responsible for starting an thread that handles all DLL integration 
        /// </summary>
        public static void SetupDLLIntegration()
        {
            
            try
            {
                if (Directory.Exists(_DLLDirectoryPath) == false)
                {
                    Directory.CreateDirectory(_DLLDirectoryPath);
                }

                List<string> filePaths = Directory.GetFiles(_DLLDirectoryPath).ToList<string>();
                if (filePaths.Count > 0)
                {
                    if (File.Exists(_DLLSartupJSONPath) == false)
                    {
                        //DLLs detected but no startup file was found. Add new file with all DLLs disabled.  
                        _IntegrationStartup = new IntegrationStartup(filePaths);                        
                    }

                    //*warning* this way of reading the file will fail if there is a large amount of DLLs to integrate (more than 1000). ReadAllText puts the whole file into ram
                    string StartupJSON = File.ReadAllText(_DLLSartupJSONPath);
                    try
                    {
                        _IntegrationStartup = JsonConvert.DeserializeObject(StartupJSON) as IntegrationStartup;
                        if (_IntegrationStartup != null)
                        {
                            ReadAndStartDLLIntegration();
                        }
                        else
                        {
                            Utilities.Log(LogLevel.Error, $"Could not deserialize json. JSON: {StartupJSON}");
                            Utilities.ShowMessage($"There was an issue reading the JSON file located at: {_DLLSartupJSONPath}. Please make sure the JSON is valid, or delete the file to reset the DLL Integration settings.", "DLL Integration Startup JSON Error");
                            return;
                        }

                        UpdateDLLStartupFile();
                    }
                    catch (Exception e)
                    {
                        Utilities.Log(LogLevel.Error, $"Message: {e.Message} Stack: {e.StackTrace}");
                        Utilities.ShowMessage($"There was an issue reading the JSON file located at: {_DLLSartupJSONPath}. Please make sure the JSON is valid, or delete the file to reset the DLL Integration settings.", "DLL Integration Startup JSON Error");
                        return;
                    }                    
                }
            }
            catch (Exception e)
            {
                Utilities.Log(LogLevel.Error, $"Message: {e.Message} Stack: {e.StackTrace}");
            }
        }

        /// <summary>
        /// Will create the managed list that keeps track of the DLL integrations during runtime and will start any integration already enabled
        /// </summary>
        private static void ReadAndStartDLLIntegration()
        {
            foreach (DLLStartup dLLStartup in _IntegrationStartup.dllsToStart)
            {
                AddDLLToIntegration(dLLStartup);
            }

            foreach(DLLIntegrationModel integrationModel in _DLLIntegratrions)
            {
                if (integrationModel.dllProperties.isEnabled)
                {
                    integrationModel.dllIntegratrion.SendLogMessage += DLLIntegratrion_LogMessage;
                    integrationModel.dllIntegratrion.OnDLLStartup();
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
        /// Adds the DLL assembly to a managed list using the properties in the DLLStartup object passed by the user
        /// </summary>
        /// <param name="dLLStartup">The DLLStartup object that contains the properties of the dll assembly</param>
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
                DisableDLL(dLLStartup.dllGuidID);
            }
        }

        /// <summary>
        /// Will attempt to enable the DLL associated with the GuidID
        /// </summary>
        /// <param name="integrationGuidID">GuidID associated with the DLL integration the user wants to enable</param>
        public static void EnableDLL(Guid integrationGuidID)
        {
            //enable the DLL in the UI and run the DLLs onstartup method
            int count = _DLLIntegratrions.Where(x => x.dllProperties.dllGuidID == integrationGuidID).Count();            

            if (count > 1)
            {
                //error, 2 integrations with the same Guid exist in current system, Disable Both
                Utilities.Log(LogLevel.Warning, $"Two or more DLL integrations were found with the same Guid ID. Could not enable any. To correct this issue, change one of the Guids in the startup file located at: {_DLLSartupJSONPath}, GuidID: {integrationGuidID}");
                Utilities.ShowMessage($"Error! Two or more DLL integrations share the same Guid ID in the startup file located at: {_DLLSartupJSONPath}. Cannot enable DLL integration until this error is corrected. Problem ID: {integrationGuidID}.", "Error, DLL ID Mismatch");
            }
            else if (count == 1)
            {
                foreach (DLLIntegrationModel integratrion in _DLLIntegratrions.Where(x => x.dllProperties.dllGuidID == integrationGuidID))
                {
                    if (integratrion.dllIntegratrion.OnDLLStartup())
                    {
                        integratrion.dllProperties.isEnabled = true;
                        count++;
                    }
                }
                UpdateDLLStartupFile();
            }
            else
            {
                //integration not found in current list
                Utilities.Log(LogLevel.Error, $"There were no DLL integrations found with the Guid ID: {integrationGuidID}.");
                Utilities.ShowMessage($"Error! There were no DLL integrations found with the Guid ID: {integrationGuidID} in the startup file located at: {_DLLSartupJSONPath}. Cannot enable DLL integration.", "Error, DLL ID Mismatch");
            }
            UpdateUI();
        }

        /// <summary>
        /// Method used to disable the DLL integration. Thia will prevent the DLL from recieving Twitch and Bot Events, and will call the DLLs disable method to stop any created threads.
        /// </summary>
        /// <param name="integrationGuidID">The internal ID of the DLL integration. This ID should be unique to the DLL.</param>
        public static void DisableDLL(Guid integrationGuidID)
        {
            //disable the dll in both the json file and UI
            int count = 0;
            foreach (DLLIntegrationModel integratrion in _DLLIntegratrions.Where(x => x.dllProperties.dllGuidID == integrationGuidID))
            {
                integratrion.dllProperties.isEnabled = false;
                if(integratrion.dllIntegratrion.DisableDLL() == false)
                {
                    Utilities.Log(LogLevel.Warning, $"DLL was unable to disable properly. GuidID: {integrationGuidID}");
                }
                count++;
            }

            if(count > 1)
            {
                //error, 2 integrations with the same Guid exist in current system, Disable Both
                Utilities.Log(LogLevel.Warning, $"Two DLL integrations were found with the same Guid ID. Disabling both by default. To correct this issue, change one of the Guids in the startup file located at: {_DLLSartupJSONPath}, GuidID: {integrationGuidID}");
                UpdateDLLStartupFile();
            }
            else if(count == 1)
            {
                UpdateDLLStartupFile();
            }
            else
            {
                //integration not found in current list
                Utilities.Log(LogLevel.Error, $"There were no DLL integrations found with the Guid ID: {integrationGuidID}.");
            }
            UpdateUI();
        }

        /// <summary>
        /// Method called when an update has happened and the UI needs to be updated to reflect the change
        /// </summary>
        private static void UpdateUI()
        {
            //message UI to update
        }

        /// <summary>
        /// Method used to save the DLL integration startup file
        /// </summary>
        private static void UpdateDLLStartupFile()
        {
            try
            {
                File.WriteAllText(_DLLSartupJSONPath, JsonConvert.SerializeObject(_IntegrationStartup));
            }
            catch(Exception e)
            {
                Utilities.Log(LogLevel.Error, $"Could not save DLL Startup File. Error: {e.Message} Stack: {e.StackTrace}.");
                Utilities.ShowMessage($"Error! Could not save DLL integration startup file! Please check log for more details.", "Error, failed to save file!");
            }            
        }
    }
}
