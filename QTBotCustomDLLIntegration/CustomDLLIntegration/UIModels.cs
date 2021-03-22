using System.Collections.Generic;

namespace QTBot.CustomDLLIntegration
{
    public class SettingsUI
    {
        public SettingsUI() { }

        public SettingsUI(UISection uiS)
        {
            Sections.Add(uiS);
        }

        public SettingsUI(List<UISection> uiS)
        {
            Sections = uiS;
        }

        public List<UISection> Sections = new List<UISection>();
    }

    public class UISection
    {
        public UISection() { }

        public UISection(string name)
        {
            SectionName = name;
        }

        public string SectionName { get; set; }
        public List<UIObject> SectionElements = new List<UIObject>();
    }

    public class UIObject
    {
        public UIObject() { }

        public UIObject(string ID, string propertyName, int order, string displayedText)
        {
            UIObjectID = ID;
            UIObjectsOrder = order;
            UIPropertyName = propertyName;
            UIText = displayedText;
        }

        public string UIObjectID { get; set; }
        public string UIPropertyName { get; set; }
        public int UIObjectsOrder { get; set; }
        public object UIValue { get; set; }
        public string UIText { get; set; }
    }

    public class UICheckbox : UIObject
    {
        public UICheckbox()
        {
            
        }

        public UICheckbox(string ID, string propertyName, int order, string displayedText) : base(ID, propertyName, order, displayedText)
        {
        }
    }

    public class UIRadioButton : UIObject
    {
        public UIRadioButton() { }

        public UIRadioButton(string ID, string propertyName, int order, List<KeyValuePair<string, object>> userOptions, string displayedText) : base(ID, propertyName, order, displayedText)
        {
            Options = userOptions;
        }

        public List<KeyValuePair<string, object>> Options { get; set; }
    }

    public class UITextBox : UIObject
    {
        public UITextBox() { }

        public UITextBox(string ID, string propertyName, int order, string displayedText) : base(ID, propertyName, order, displayedText)
        {

        }
    }

    public class UISlider : UIObject
    {
        public UISlider() { }

        public UISlider(string ID, string propertyName, int order, string displayedText, int max, int min, int current, int increment) : base(ID, propertyName, order, displayedText)
        {
            MaxValue = max;
            MinValue = min;
            CurrentValue = current;
            IncrementValue = increment;
        }

        public int MaxValue { get; set; }
        public int MinValue { get; set; }
        public int CurrentValue { get; set; }
        public int IncrementValue { get; set; }
    }

    public class UISelectionDropdown : UIObject
    {
        public UISelectionDropdown() { }

        public UISelectionDropdown(string ID, string propertyName, int order, List<KeyValuePair<string, object>> dropDownList, string displayedText) : base(ID, propertyName, order, displayedText)
        {
            List = dropDownList;
        }

        public List<KeyValuePair<string, object>> List { get; set; }
    }

    public class UIEditableDropdown : UIObject
    {
        public UIEditableDropdown() { }

        public UIEditableDropdown(string ID, string propertyName, List<KeyValuePair<string, object>> dropDownList, int order, string displayedText, string valueDisplayText) : base(ID, propertyName, order, displayedText)
        {
            List = dropDownList;
            ValueLabel = valueDisplayText;
        }

        public List<KeyValuePair<string, object>> List { get; set; }

        public string ValueLabel { get; set; }
    }

    public class UITable : UIObject
    {
        public UITable() { }

        public UITable(string ID, string propertyName, int order, string displayedText) : base(ID, propertyName, order, displayedText)
        {

        }
    }

    public class UIButton : UIObject
    {
        public UIButton() { }

        public UIButton(string ID, string propertyName, int order, string displayedText) : base(ID, propertyName, order, displayedText)
        {

        }
    }
}
