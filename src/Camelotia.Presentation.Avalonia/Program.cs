﻿using Avalonia;
using Avalonia.ReactiveUI;
using Avalonia.Logging.Serilog;

namespace MegaCom.UI
{
    public sealed class Program
    {
        public static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UseReactiveUI()
                .UsePlatformDetect()
                .LogToDebug();
    }
}
