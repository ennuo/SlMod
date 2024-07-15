namespace SeEditor.Graphics.ImGui;

public static class ImGuiHelper
{
    public static string DoLabelPrefix(string label)
    {
        float width = ImGuiNET.ImGui.CalcItemWidth();

        float x = ImGuiNET.ImGui.GetCursorPosX();
        ImGuiNET.ImGui.Text(label);
        ImGuiNET.ImGui.SameLine();
        ImGuiNET.ImGui.SetCursorPosX(x + width * 0.5f + ImGuiNET.ImGui.GetStyle().ItemInnerSpacing.X);
        ImGuiNET.ImGui.SetNextItemWidth(-1.0f);
        
        return $"##{label}";
    }

    public static void DoBoldText(string text)
    {
        ImGuiNET.ImGui.PushFont(ImGuiNET.ImGui.GetIO().Fonts.Fonts[(int)ImGuiFontList.InterBold]);
        ImGuiNET.ImGui.Text(text);
        ImGuiNET.ImGui.PopFont();
    }

    public static bool DoIndexedEnum<T>(string text, ref int value) where T : Enum
    {
        string[] names = Enum.GetNames(typeof(T));
        return ImGuiNET.ImGui.Combo(text, ref value, names, names.Length);
    }
    
    public static void DoHashedEnum<T>(string text, ref T value) where T : Enum
    {
        string[] names = Enum.GetNames(typeof(T));
        int[] values = (int[])Enum.GetValuesAsUnderlyingType(typeof(T));
        
        if (ImGuiNET.ImGui.BeginCombo(text, value.ToString()))
        {
            for (int i = 0; i < names.Length; ++i)
            {
                bool selected = (int)(object)value == values[i];
                if (ImGuiNET.ImGui.Selectable(names[i], selected))
                    value = (T)(object)values[i];
                if (selected)
                    ImGuiNET.ImGui.SetItemDefaultFocus();
            }
            
            ImGuiNET.ImGui.EndCombo();
        }
    }
}