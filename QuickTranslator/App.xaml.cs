﻿using System.Windows;
using Application = System.Windows.Application;

namespace QuickTranslator
{
    public partial class App : Application
    {
        private TrayApplicationContext? _trayContext;
        
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // 不显示主窗口
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            
            _trayContext = new TrayApplicationContext();
        }
        
        protected override void OnExit(ExitEventArgs e)
        {
            _trayContext?.Dispose();
            base.OnExit(e);
        }
    }
}
