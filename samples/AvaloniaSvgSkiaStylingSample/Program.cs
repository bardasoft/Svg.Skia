﻿using System;
using Avalonia;
using Avalonia.Svg.Skia;
using Svg.Skia;

namespace AvaloniaSvgSkiaStylingSample;

internal class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
    {
        GC.KeepAlive(typeof(SvgImageExtension).Assembly);
        GC.KeepAlive(typeof(Avalonia.Svg.Skia.Svg).Assembly);

        SKSvg.CacheOriginalStream = true;

        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new X11PlatformOptions
            {
            })
            .LogToTrace();
    }
}
