﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace MegaCom.UI {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "16.7.0.0")]
    internal sealed partial class MegaComSettings : global::System.Configuration.ApplicationSettingsBase {
        
        private static MegaComSettings defaultInstance = ((MegaComSettings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new MegaComSettings())));
        
        public static MegaComSettings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string ComPort {
            get {
                return ((string)(this["ComPort"]));
            }
            set {
                this["ComPort"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("MegaCommandPort1")]
        public string MidiPort1 {
            get {
                return ((string)(this["MidiPort1"]));
            }
            set {
                this["MidiPort1"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("MegaCommandPort2")]
        public string MidiPort2 {
            get {
                return ((string)(this["MidiPort2"]));
            }
            set {
                this["MidiPort2"] = value;
            }
        }
    }
}
