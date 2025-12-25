using System.Collections;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

public class KeySelectorDropdown : AdvancedDropdown
{
    private List<string> _keys;
    private Dictionary<string, string> _subtitles;
    public System.Action<string> OnKeySelected;

    public KeySelectorDropdown(AdvancedDropdownState state, List<string> keys, Dictionary<string, string> subtitles = null) : base(state)
    {
        _keys = keys;
        _subtitles = subtitles;

        var size = minimumSize;
        size.y = 300;
        if (_subtitles != null) size.x = 400;
        minimumSize = size;
    }

    protected override AdvancedDropdownItem BuildRoot()
    {
        var root = new AdvancedDropdownItem("Keys");

        if (_subtitles != null)
        {
            foreach (var kvp in _subtitles)
            {
                string display = $"{kvp.Key} | {Truncate(kvp.Value, 50)}";

                var item = new AdvancedDropdownItem(display);

                root.AddChild(item);
            }
        }
        else if (_keys != null)
        {
            foreach (var key in _keys)
            {
                root.AddChild(new AdvancedDropdownItem(key));
            }
        }
        else
        {
            root.AddChild(new AdvancedDropdownItem("(No Keys Found)"));
        }

        return root;
    }

    protected override void ItemSelected(AdvancedDropdownItem item)
    {
        base.ItemSelected(item);
        if (OnKeySelected != null)
        {
            string result = item.name;

            if (_subtitles != null)
            {
                int separatorIndex = result.IndexOf(" | ");
                if (separatorIndex > -1)
                {
                    result = result.Substring(0, separatorIndex);
                }
            }

            OnKeySelected.Invoke(result);
        }
    }

    private string Truncate(string value, int maxChars)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Length <= maxChars ? value : value.Substring(0, maxChars) + "...";
    }
}