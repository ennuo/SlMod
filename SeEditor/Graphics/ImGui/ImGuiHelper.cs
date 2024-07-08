using ImGuiNET;

namespace SeEditor;

public static class ImGuiHelper
{
    public static string DoLabelPrefix(string label)
    {
        float width = ImGui.CalcItemWidth();

        float x = ImGui.GetCursorPosX();
        ImGui.Text(label);
        ImGui.SameLine();
        ImGui.SetCursorPosX(x + width * 0.5f + ImGui.GetStyle().ItemInnerSpacing.X);
        ImGui.SetNextItemWidth(-1.0f);
        
        return $"##{label}";
    }

    public static void DoBoldText(string text)
    {
        ImGui.PushFont(ImGui.GetIO().Fonts.Fonts[(int)ImGuiFontList.InterBold]);
        ImGui.Text(text);
        ImGui.PopFont();
    }

    public static void DoIndexedEnum<T>(string text, ref int value) where T : Enum
    {
        string[] names = Enum.GetNames(typeof(T));
        ImGui.Combo(text, ref value, names, names.Length);
    }
    
    public static void DoHashedEnum<T>(string text, ref T value) where T : Enum
    {
        string[] names = Enum.GetNames(typeof(T));
        int[] values = (int[])Enum.GetValuesAsUnderlyingType(typeof(T));
        
        if (ImGui.BeginCombo(text, value.ToString()))
        {
            for (int i = 0; i < names.Length; ++i)
            {
                bool selected = (int)(object)value == values[i];
                if (ImGui.Selectable(names[i], selected))
                    value = (T)(object)values[i];
                if (selected)
                    ImGui.SetItemDefaultFocus();
            }
            
            ImGui.EndCombo();
        }
    }
}