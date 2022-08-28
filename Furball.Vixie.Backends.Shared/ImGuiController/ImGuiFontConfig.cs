using System;

namespace Furball.Vixie.Backends.Shared.ImGuiController; 

public readonly struct ImGuiFontConfig
{
    public ImGuiFontConfig(string fontPath, int fontSize)
    {
        if (fontSize <= 0) throw new ArgumentOutOfRangeException(nameof(fontSize));
        this.FontPath = fontPath ?? throw new ArgumentNullException(nameof(fontPath));
        this.FontSize = fontSize;
    }

    public string FontPath { get; }
    public int    FontSize { get; }
}