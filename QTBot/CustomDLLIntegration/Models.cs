
using System;
using System.Collections.Generic;
using System.IO;

namespace QTBot.CustomDLLIntegration
{
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


    #region UI Objects
    public class SettingsUI
    {
        public SettingsUI(UISection uiS)
        {
            sections.Add(uiS);
        }

        public SettingsUI(List<UISection> uiS)
        {
            sections = uiS;
        }

        public List<UISection> sections = new List<UISection>();
    }

    public class UISection
    {
        public UISection(string name)
        {
            sectionName = name;
        }

        public string sectionName { get; }
        public List<UIObject> sectionElements = new List<UIObject>();
    }

    public class UIObject
    {
        public UIObject(string ID, int order)
        {
            uiObjectID = ID;
            uiObjectsOrder = order;
        }

        public string uiObjectID { get; }
        public int uiObjectsOrder { get; }
        public object uiValue { get; set; }
    }

    public class UIButton : UIObject
    {
        public UIButton(string ID, int order) : base(ID, order)
        {

        }
    }

    public class UICheckbox : UIObject
    {
        public UICheckbox(string ID, int order) : base(ID, order)
        {

        }
    }

    public class RadialButton : UIObject
    {
        public RadialButton(string ID, int order) : base(ID, order)
        {

        }
    }

    public class UITextBox : UIObject
    {
        public UITextBox(string ID, int order) : base(ID, order)
        {

        }
    }

    public class UISelectionDropdown : UIObject
    {
        public UISelectionDropdown(string ID, int order) : base(ID, order)
        {

        }
    }

    public class UITable : UIObject
    {
        public UITable(string ID, int order) : base(ID, order)
        {

        }
    }

    #endregion
}
