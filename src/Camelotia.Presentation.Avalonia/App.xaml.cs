using Avalonia;
using Avalonia.Markup.Xaml;
using MegaCom.UI.Services;
using MegaCom.UI.Views;
using Camelotia.Presentation.ViewModels;
using Camelotia.Services.Interfaces;
using Camelotia.Services.Models;
using Camelotia.Services.Providers;
using System;
using System.Collections.Generic;
using MegaCom.UI.ViewModels;

namespace MegaCom.UI
{
    public class App : Application
    {
        private MegaCom.ComHost m_host;

        public override void Initialize() => AvaloniaXamlLoader.Load(this);

        public override void OnFrameworkInitializationCompleted()
        {
            m_host = new MegaCom.ComHost("COM4");
            var window = new MainWindow();
            var files = new AvaloniaFileManager(window);
            var styles = new AvaloniaStyleManager(window);
            window.SwitchThemeButton.Click += (sender, args) => styles.UseNextTheme(); 

            var file_vm = new FileBrowserViewModel(new AvaloniaFileManager(window), new MegaCommandProvider(m_host));
            var main_vm = new MainWindowViewModel(file_vm);

            window.DataContext = main_vm;
            window.Show();
            base.OnFrameworkInitializationCompleted();
        }
    }
}