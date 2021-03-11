using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using QTBot.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;


namespace QTBot.CustomDLLIntegration
{
    public static class IntegrationHelper
    {
        //dll integration variables
        private static List<DLLIntegrationModel> _DLLIntegratrions = new List<DLLIntegrationModel>();
        private static IntegrationStartup _IntegrationStartup;

        private static string _DLLDirectoryPath = "";
        private static string _DLLStartupJSONPath = "";

        private static bool _WasStartupCalled = false;

        public static void SetupIntegrationhelper(string dllDirectoryPath, string dllStartupJsonPath)
        {
            if (_WasStartupCalled == false)
            {
                _DLLDirectoryPath = dllDirectoryPath;
                _DLLStartupJSONPath = dllStartupJsonPath;

                _WasStartupCalled = true;
            }
        }

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
                if (_WasStartupCalled == false)
                {
                    throw new Exception("Error, SetupDLLIntegration was called before SetupIntegrationhelper.");
                }

                if (Directory.Exists(_DLLDirectoryPath) == false)
                {
                    Directory.CreateDirectory(_DLLDirectoryPath);
                }

                List<string> filePaths = Directory.GetFiles(_DLLDirectoryPath).ToList<string>();
                if (filePaths.Count > 0)
                {
                    string startupJSON = string.Empty;
                    if (File.Exists(_DLLStartupJSONPath))
                    {
                        //*warning* this way of reading the file will fail if there is a large amount of DLLs to integrate (more than 1000). ReadAllText puts the whole file into ram
                        startupJSON = File.ReadAllText(_DLLStartupJSONPath);

                        _IntegrationStartup = JsonConvert.DeserializeObject<IntegrationStartup>(startupJSON);
                    }
                    else
                    {
                        //DLLs detected but no startup file was found. Add new file with all DLLs disabled.  
                        _IntegrationStartup = new IntegrationStartup(filePaths);
                    }

                    try
                    {
                        
                        if (_IntegrationStartup != null)
                        {
                            ReadAndStartDLLIntegration();
                        }
                        else
                        {
                            Utilities.Log(LogLevel.Error, $"Could not deserialize json. JSON: {startupJSON}");
                            Utilities.ShowMessage($"There was an issue reading the JSON file located at: {_DLLStartupJSONPath}. Please make sure the JSON is valid, or delete the file to reset the DLL Integration settings.", "DLL Integration Startup JSON Error");
                            return;
                        }

                        UpdateDLLStartupFile();
                    }
                    catch (Exception e)
                    {
                        Utilities.Log(LogLevel.Error, $"Message: {e.Message} Stack: {e.StackTrace}");
                        Utilities.ShowMessage($"There was an issue reading the JSON file located at: {_DLLStartupJSONPath}. Please make sure the JSON is valid, or delete the file to reset the DLL Integration settings.", "DLL Integration Startup JSON Error");
                    }                    
                }
            }
            catch (Exception e)
            {
                Utilities.Log(LogLevel.Error, $"Message: {e.Message} Stack: {e.StackTrace}");
                Utilities.ShowMessage($"Error, there was an issue with integration startup. Look to logs for more detail.", "DLL Integration Startup Error");
            }
        }

        /// <summary>
        /// Will create the managed list that keeps track of the DLL integrations during runtime and will start any integration already enabled
        /// </summary>
        private static void ReadAndStartDLLIntegration()
        {
            foreach (DLLStartup dLLStartup in _IntegrationStartup.dllsToStart.ToList())
            {
                if(!AddDLLToIntegration(dLLStartup))
                {
                    _IntegrationStartup.dllsToStart.Remove(dLLStartup);
                }
            }

            foreach(DLLIntegrationModel integrationModel in _DLLIntegratrions)
            {
                if (integrationModel.dllProperties.isEnabled)
                {
                    integrationModel.dllIntegration.SendLogMessage += DLLIntegratrion_LogMessage;
                    integrationModel.dllIntegration.OnDLLStartup();
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
        public static bool AddDLLToIntegration(DLLStartup dLLStartup)
        {
            try
            {
                if (_WasStartupCalled == false)
                {
                    throw new Exception("Error, AddDLLToIntegration was called before SetupIntegrationhelper.");
                }

                var DLL = Assembly.LoadFile(dLLStartup.dllPath);
                try
                {
                    foreach (Type type in DLL.GetExportedTypes())
                    {
                    
                        var dllClass = Activator.CreateInstance(type);
                        DLLIntegratrionInterface dLLIntegratrion = dllClass as DLLIntegratrionInterface;
                        if (dLLIntegratrion != null)
                        {
                            if (dLLStartup.isEnabled)
                            {
                                AddHandlersToDLLAssembly(dLLIntegratrion);
                            }
                            RetrieveDLLSettings(dLLIntegratrion);
                            _DLLIntegratrions.Add(new DLLIntegrationModel(dLLIntegratrion, dLLStartup));
                        }
                    }

                    return true;
                }
                catch (FileNotFoundException f)
                {
                    Utilities.Log(LogLevel.Warning, $"Could not get types for {dLLStartup.dllName}. {f.StackTrace}");
                }
            }
            catch (Exception e)
            {
                Utilities.Log(LogLevel.Error, $"Could not add integration {dLLStartup.dllName}.");
                Utilities.Log(LogLevel.Error, $"Message: {e.Message} Stack: {e.StackTrace}");
                DisableDLL(dLLStartup.dllGuidID);
            }

            return false;
        }

        /// <summary>
        /// Will attempt to enable the DLL associated with the GuidID
        /// </summary>
        /// <param name="integrationGuidID">GuidID associated with the DLL integration the user wants to enable</param>
        public static void EnableDLL(Guid integrationGuidID)
        {
            try
            {
                if (_WasStartupCalled == false)
                {
                    throw new Exception("Error, AddDLLToIntegration was called before SetupIntegrationhelper.");
                }

                //enable the DLL in the UI and run the DLLs onstartup method
                int count = _DLLIntegratrions.Where(x => x.dllProperties.dllGuidID == integrationGuidID).Count();

                if (count > 1)
                {
                    //error, 2 integrations with the same Guid exist in current system, Disable Both
                    Utilities.Log(LogLevel.Warning, $"Two or more DLL integrations were found with the same Guid ID. Could not enable any. To correct this issue, change one of the Guids in the startup file located at: {_DLLStartupJSONPath}, GuidID: {integrationGuidID}");
                    Utilities.ShowMessage($"Error! Two or more DLL integrations share the same Guid ID in the startup file located at: {_DLLStartupJSONPath}. Cannot enable DLL integration until this error is corrected. Problem ID: {integrationGuidID}.", "Error, DLL ID Mismatch");
                }
                else if (count == 1)
                {
                    foreach (DLLIntegrationModel integratrion in _DLLIntegratrions.Where(x => x.dllProperties.dllGuidID == integrationGuidID))
                    {
                        if (integratrion.dllIntegration.OnDLLStartup())
                        {
                            integratrion.dllProperties.isEnabled = true;
                            AddHandlersToDLLAssembly(integratrion.dllIntegration);
                        }
                    }
                    UpdateDLLStartupFile();
                }
                else
                {
                    //integration not found in current list
                    Utilities.Log(LogLevel.Error, $"There were no DLL integrations found with the Guid ID: {integrationGuidID}.");
                    Utilities.ShowMessage($"Error! There were no DLL integrations found with the Guid ID: {integrationGuidID} in the startup file located at: {_DLLStartupJSONPath}. Cannot enable DLL integration.", "Error, DLL ID Mismatch");
                }
            }
            catch (Exception e)
            {
                Utilities.Log(LogLevel.Error, $"Could not add enable dll integration with GuidID: {integrationGuidID}.");
                Utilities.Log(LogLevel.Error, $"Message: {e.Message} Stack: {e.StackTrace}");
                Utilities.ShowMessage($"Error! Cannot enable DLL integration. Look in logs for more information.", "Error, Unable to start DLL Integration");
            }
        }

        /// <summary>
        /// Method used to disable the DLL integration. Thia will prevent the DLL from recieving Twitch and Bot Events, and will call the DLLs disable method to stop any created threads.
        /// </summary>
        /// <param name="integrationGuidID">The internal ID of the DLL integration. This ID should be unique to the DLL.</param>
        public static void DisableDLL(Guid integrationGuidID)
        {
            try
            {
                if (_WasStartupCalled == false)
                {
                    throw new Exception("Error, AddDLLToIntegration was called before SetupIntegrationhelper.");
                }

                //disable the dll in both the json file and UI
                int count = 0;
                foreach (DLLIntegrationModel integratrion in _DLLIntegratrions.Where(x => x.dllProperties.dllGuidID == integrationGuidID))
                {
                    integratrion.dllProperties.isEnabled = false;
                    if (integratrion.dllIntegration.DisableDLL() == false)
                    {
                        Utilities.Log(LogLevel.Warning, $"DLL was unable to disable properly. GuidID: {integrationGuidID}");
                    }
                    RemoveHandlersFromDLLAssembly(integratrion.dllIntegration);
                    count++;
                }

                if (count > 1)
                {
                    //error, 2 integrations with the same Guid exist in current system, Disable Both
                    Utilities.Log(LogLevel.Warning, $"Two DLL integrations were found with the same Guid ID. Disabling both by default. To correct this issue, change one of the Guids in the startup file located at: {_DLLStartupJSONPath}, GuidID: {integrationGuidID}");
                    UpdateDLLStartupFile();
                }
                else if (count == 1)
                {
                    UpdateDLLStartupFile();
                }
                else
                {
                    //integration not found in current list
                    Utilities.Log(LogLevel.Error, $"There were no DLL integrations found with the Guid ID: {integrationGuidID}.");
                }

            }
            catch(Exception e)
            {
                Utilities.Log(LogLevel.Error, $"Message: {e.Message} Stack: {e.StackTrace}");
                Utilities.ShowMessage($"Error! Cannot disable DLL integration. Look in logs for more information.", "Error, Unable to stop DLL Integration");
            }            
        }

        /// <summary>
        /// Removes all handlers from the integration DLLs that may exist
        /// </summary>
        public static void DisableAllEnabledDLLsHandlers()
        {
            foreach(DLLIntegrationModel integrationModel in _DLLIntegratrions)
            {
                if (integrationModel.dllProperties.isEnabled)
                {
                    RemoveHandlersFromDLLAssembly(integrationModel.dllIntegration);
                }
            }
        }

        /// <summary>
        /// Add handlers to all enabled integration DLLs
        /// </summary>
        public static void ReEnableAllEnabledDLLsHandlers()
        {
            foreach (DLLIntegrationModel integrationModel in _DLLIntegratrions)
            {
                if (integrationModel.dllProperties.isEnabled)
                {
                    AddHandlersToDLLAssembly(integrationModel.dllIntegration);
                }
            }
        }

        /// <summary>
        /// Add all handlers to DLL integration for events from twitch
        /// </summary>
        /// <param name="dLLIntegration">DLL integration</param>
        private static void AddHandlersToDLLAssembly(DLLIntegratrionInterface dLLIntegration)
        {
            try
            {
                RemoveHandlersFromDLLAssembly(dLLIntegration);
                QTCore.Instance.EventsManager.OnBitsReceived += dLLIntegration.OnBitsReceived;
                QTCore.Instance.EventsManager.OnChannelSubscription += dLLIntegration.OnChannelSubscription;
                QTCore.Instance.EventsManager.OnEmoteOnly += dLLIntegration.OnEmoteOnlyOn;
                QTCore.Instance.EventsManager.OnEmoteOnlyOff += dLLIntegration.OnEmoteOnlyOff;
                QTCore.Instance.EventsManager.OnFollow += dLLIntegration.OnFollow;
                QTCore.Instance.EventsManager.OnMessageReceived += dLLIntegration.OnMessageReceived;
                QTCore.Instance.EventsManager.OnRaid += dLLIntegration.OnRaidNotification;
                QTCore.Instance.EventsManager.OnRewardRedeemed += dLLIntegration.OnRewardRedeemed;
                QTCore.Instance.EventsManager.OnListenResponse += dLLIntegration.OnListenResponse;
                QTCore.Instance.EventsManager.OnStreamUpResponse += dLLIntegration.OnStreamUp;
                QTCore.Instance.EventsManager.OnStreamDownResponse += dLLIntegration.OnStreamDown;
                QTCore.Instance.EventsManager.OnBeingHostResponse += dLLIntegration.OnBeingHosted;
                QTCore.Instance.EventsManager.OnHostingStartedResponse += dLLIntegration.OnHostingStarted;
                QTCore.Instance.EventsManager.OnJoinedChannelResponse += dLLIntegration.OnBotJoinedChannel;
            }
            catch (Exception e)
            {
                Utilities.Log(LogLevel.Error, $"Message: {e.Message} Stack: {e.StackTrace}");
            }
        }

        /// <summary>
        /// Remove all handlers to DLL integration for events from twitch
        /// </summary>
        /// <param name="dLLIntegration">DLL integration</param>
        private static void RemoveHandlersFromDLLAssembly(DLLIntegratrionInterface dLLIntegration)
        {
            try
            {
                QTCore.Instance.EventsManager.OnBitsReceived -= dLLIntegration.OnBitsReceived;
                QTCore.Instance.EventsManager.OnChannelSubscription -= dLLIntegration.OnChannelSubscription;
                QTCore.Instance.EventsManager.OnEmoteOnly -= dLLIntegration.OnEmoteOnlyOn;
                QTCore.Instance.EventsManager.OnEmoteOnlyOff -= dLLIntegration.OnEmoteOnlyOff;
                QTCore.Instance.EventsManager.OnFollow -= dLLIntegration.OnFollow;
                QTCore.Instance.EventsManager.OnMessageReceived -= dLLIntegration.OnMessageReceived;
                QTCore.Instance.EventsManager.OnRaid -= dLLIntegration.OnRaidNotification;
                QTCore.Instance.EventsManager.OnRewardRedeemed -= dLLIntegration.OnRewardRedeemed;
                QTCore.Instance.EventsManager.OnListenResponse -= dLLIntegration.OnListenResponse;
                QTCore.Instance.EventsManager.OnStreamUpResponse -= dLLIntegration.OnStreamUp;
                QTCore.Instance.EventsManager.OnStreamDownResponse -= dLLIntegration.OnStreamDown;
                QTCore.Instance.EventsManager.OnBeingHostResponse -= dLLIntegration.OnBeingHosted;
                QTCore.Instance.EventsManager.OnHostingStartedResponse -= dLLIntegration.OnHostingStarted;
                QTCore.Instance.EventsManager.OnJoinedChannelResponse -= dLLIntegration.OnBotJoinedChannel;
            }
            catch (Exception e)
            {
                Utilities.Log(LogLevel.Error, $"Message: {e.Message} Stack: {e.StackTrace}");
            }
        }

        /// <summary>
        /// Method used to save the DLL integration startup file
        /// </summary>
        private static void UpdateDLLStartupFile()
        {
            try
            {
                File.WriteAllText(_DLLStartupJSONPath, JsonConvert.SerializeObject(_IntegrationStartup, Formatting.Indented));
            }
            catch(Exception e)
            {
                Utilities.Log(LogLevel.Error, $"Could not save DLL Startup File. Error: {e.Message} Stack: {e.StackTrace}.");
                Utilities.ShowMessage($"Error! Could not save DLL integration startup file! Please check log for more details.", "Error, failed to save file!");
            }            
        }

        /// <summary>
        /// Turns the SettingsUI object into json and saves the json to the DLL integrations settings file
        /// </summary>
        /// <param name="dLLIntegration">DLL integration</param>
        /// <param name="uiValues"></param>
        /// <returns>Returns if the save failed or worked</returns>
        public static bool SaveDLLSettingsToFile(DLLIntegratrionInterface dLLIntegration, SettingsUI uiValues)
        {
            if (uiValues != null)
            {
                try
                {
                    string dllDirectoryPath = Path.Combine(_DLLDirectoryPath, dLLIntegration.IntegrationName);
                    string dllSettingsFilePath = Path.Combine(dllDirectoryPath, dLLIntegration.DLLSettingsFileName);
                    if (Directory.Exists(dllDirectoryPath) == false)
                    {
                        Directory.CreateDirectory(dllDirectoryPath);
                    }

                    File.WriteAllText(dllSettingsFilePath, JsonConvert.SerializeObject(uiValues, Formatting.Indented));

                    Utilities.Log(LogLevel.Information, $"DLL: {dLLIntegration.IntegrationName} settings saved.");
                    return true;
                }
                catch (Exception e)
                {
                    Utilities.Log(LogLevel.Information, $"DLL: {dLLIntegration.IntegrationName} failed to save settings.");
                    Utilities.Log(e);
                }
            }

            return false;
        }

        /// <summary>
        /// Opens the DLL integration settings file, reads the json from the file and creates a SettingsUI object from the json
        /// </summary>
        /// <param name="dLLIntegration">DLL integration</param>
        /// <returns>Returns the SettingsUI stored in the DLL integrations settings file</returns>
        public static SettingsUI RetrieveDLLSettings(DLLIntegratrionInterface dLLIntegration)
        {
            try
            {
                SettingsUI rtn;
                string dllDirectoryPath = Path.Combine(_DLLDirectoryPath, dLLIntegration.IntegrationName);
                string dllSettingsFilePath = Path.Combine(dllDirectoryPath, dLLIntegration.DLLSettingsFileName);

                if (File.Exists(dllSettingsFilePath))
                {
                    string StartupJSON = File.ReadAllText(dllSettingsFilePath);
                    rtn = JsonConvert.DeserializeObject<SettingsUI>(StartupJSON);

                    if (rtn != null)
                    {
                        dLLIntegration.CurrentSettingsUI = rtn;
                        return rtn;
                    }

                    Utilities.Log(LogLevel.Warning, $"DLL: {dLLIntegration.IntegrationName} settings file could not be deserialized. SettingsFilePath: {dLLIntegration.IntegrationName}");
                }
                else
                {
                    if (Directory.Exists(dllDirectoryPath) == false)
                    {
                        Directory.CreateDirectory(dllDirectoryPath);
                    }

                    File.WriteAllText(dllSettingsFilePath, JsonConvert.SerializeObject(dLLIntegration.DefaultUI, Formatting.Indented));

                    Utilities.Log(LogLevel.Information, $"DLL: {dLLIntegration.IntegrationName} settings did not exist, creating default. SettingsFilePath: {dLLIntegration.IntegrationName}");
                }
            }
            catch (Exception e)
            {
                Utilities.Log(e);
            }

            Utilities.Log(LogLevel.Information, $"DLL: {dLLIntegration.IntegrationName} failed to load settings.");
            return null;
        }

    }
}
