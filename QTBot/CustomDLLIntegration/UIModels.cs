using System.Collections.Generic;

namespace QTBot.CustomDLLIntegration
{
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

    public class UISlider : UIObject
    {
        public UISlider(string ID, string propertyName, int order, string displayedText, int max, int min, int current, int increment) : base(ID, propertyName, order, displayedText)
        {
            maxValue = max;
            minValue = min;
            currentValue = current;
            incrementValue = increment;
        }

        public int maxValue { get; }
        public int minValue { get; }
        public int currentValue { get; }
        public int incrementValue { get; }
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
        public UIEditableDropdown(string ID, string propertyName, List<KeyValuePair<string, object>> dropDownList, int order, string displayedText, string valueDisplayText) : base(ID, propertyName, order, displayedText)
        {
            list = dropDownList;
            valueLabel = valueDisplayText;
        }

        public List<KeyValuePair<string, object>> list { get; }

        public string valueLabel { get; }
    }

    public class UITable : UIObject
    {
        public UITable(string ID, string propertyName, int order, string displayedText) : base(ID, propertyName, order, displayedText)
        {

        }
    }

    public class UIButton : UIObject
    {
        public UIButton(string ID, string propertyName, int order, string displayedText) : base(ID, propertyName, order, displayedText)
        {

        }
    }
}
