using System.Drawing;
using System.Numerics;
using ImGuiNET;
using Nayae.Engine.Extensions;

namespace Nayae.Editor;

public static class ImGuiUtility
{
    public static bool ToggleButton(string text, ref bool selected)
    {
        var shouldPop = false;
        if (!selected)
        {
            shouldPop = true;
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0));
        }

        var result = false;
        if (ImGui.Button(text))
        {
            result = true;
            selected = !selected;
        }

        if (shouldPop)
        {
            ImGui.PopStyleColor();
        }

        return result;
    }

    public static bool ToggleButton(string text, Color activeColor, bool selected)
    {
        return ToggleButton(text, activeColor.ToVector(), selected);
    }

    public static bool ToggleButton(string text, Vector4 activeColor, bool selected)
    {
        ImGui.PushStyleColor(ImGuiCol.Button, selected ? activeColor : new Vector4(0));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, selected ? activeColor : new Vector4(0));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, selected ? activeColor : new Vector4(0));

        var result = ImGui.Button(text);
        ImGui.PopStyleColor(3);
        return result;
    }

    public static bool ToggleButton(string text, bool selected)
    {
        var styleColors = ImGui.GetStyle().Colors;
        ImGui.PushStyleColor(ImGuiCol.Button, selected ? styleColors[(int)ImGuiCol.Button] : new Vector4(0));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered,
            selected ? styleColors[(int)ImGuiCol.ButtonHovered] : new Vector4(0));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive,
            selected ? styleColors[(int)ImGuiCol.ButtonActive] : new Vector4(0));

        var result = ImGui.Button(text);
        ImGui.PopStyleColor(3);
        return result;
    }
}