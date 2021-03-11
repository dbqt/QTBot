
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
        public UIObject(string ID, string propertyName, int order, string displayedText)
        {
            uiObjectID = ID;
            uiObjectsOrder = order;
            uiPropertyName = propertyName;
            text = displayedText;
        }

        public string uiObjectID { get; }
        public string uiPropertyName { get; }
        public int uiObjectsOrder { get; }
        public object uiValue { get; set; }

        private string text;
    }

    public class UIButton : UIObject
    {
        public UIButton(string ID, string propertyName, int order, string displayedText) : base(ID, propertyName, order, displayedText)
        {

        }
    }

    public class UICheckbox : UIObject
    {
        public UICheckbox(string ID, string propertyName, int order, string displayedText) : base(ID, propertyName, order, displayedText)
        {

        }
    }

    public class RadialButton : UIObject
    {
        public RadialButton(string ID, string propertyName, int order, List<KeyValuePair<string, object>> userOptions, string displayedText) : base(ID, propertyName, order, displayedText)
        {
            options = userOptions;
        }

        public List<KeyValuePair<string, object>> options { get; }
    }

    public class UITextBox : UIObject
    {
        public UITextBox(string ID, string propertyName, int order, string displayedText) : base(ID, propertyName, order, displayedText)
        {

        }
    }

    public class UISelectionDropdown : UIObject
    {
        public UISelectionDropdown(string ID, string propertyName, int order, List<KeyValuePair<string, object>> dropDownList, string displayedText) : base(ID, propertyName, order, displayedText)
        {
            list = dropDownList;
        }

        public List<KeyValuePair<string, object>> list { get; }
    }

    public class UIEditableDropdown : UIObject
    {
        public UIEditableDropdown(string ID, string propertyName, List<KeyValuePair<string, object>> dropDownList, int order, string displayedText) : base(ID, propertyName, order, displayedText)
        {
            list = dropDownList;
        }

        public List<KeyValuePair<string, object>> list { get; }
    }

    public class UITable : UIObject
    {
        public UITable(string ID, string propertyName, int order, string displayedText) : base(ID, propertyName, order, displayedText)
        {

        }
    }

    #endregion
}
