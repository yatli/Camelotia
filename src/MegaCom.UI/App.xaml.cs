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
using Camelotia.Presentation.Interfaces;
using System.IO;

namespace MegaCom.UI
{
    public class App : Application
    {
        private MegaCom.ComHost m_host;

        public override void Initialize() => AvaloniaXamlLoader.Load(this);

        public override void OnFrameworkInitializationCompleted()
        {
            m_host = new ComHost();
            var window = new MainWindow();
            var files = new AvaloniaFileManager(window);
            var styles = new AvaloniaStyleManager(window);
            window.SwitchThemeButton.Click += (sender, args) => styles.UseNextTheme();

            var file_vm = new FileBrowserViewModel(new AvaloniaFileManager(window), new MegaCommandProvider(m_host), MegaCommandFilenameValidator);
            var disp_vm = new DisplayMirrorViewModel(m_host);
            var port_vm = new PortStatusViewModel(m_host);
            var midi_vm = new MidiProxyViewModel(m_host);
            var debug_vm = new DebugViewModel(m_host);
            var main_vm = new MainWindowViewModel(file_vm, disp_vm, port_vm, midi_vm, debug_vm);

            window.DataContext = main_vm;
            window.Show();
            base.OnFrameworkInitializationCompleted();
        }

        private string MegaCommandFilenameValidator(IFileViewModel file)
        {
            List<string> diag = new List<string>();
            if (file.Name.Length > 16)
            {
                diag.Add("LONG");
            }
            var ext = Path.GetExtension(file.Name);
            if (ext.Length > 0 && ext == ext.ToUpper())
            {
                diag.Add("UEXT");
            }
            if (ext.ToLower() == ".wav" && file.RawSize >= 512*1024)
            {
                diag.Add("HUGE");
            }
            return String.Join(',', diag);
        }
    }
}