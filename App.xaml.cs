using System;
using System.Windows;
using System.Windows.Threading;
using AdhdFocusOverlay.Controllers;
using AdhdFocusOverlay.Models;
using AdhdFocusOverlay.Services;
using AdhdFocusOverlay.Views;
using Microsoft.Win32;
using DrawingSystemIcons = System.Drawing.SystemIcons;
using Forms = System.Windows.Forms;

namespace AdhdFocusOverlay;

public partial class App : System.Windows.Application
{
    private const int ToggleMonitorHotkeyId = 100;
    private const int ToggleOverlayHotkeyId = 101;

    private Forms.NotifyIcon? notifyIcon;
    private MonitorManager? monitorManager;
    private SettingsService? settingsService;
    private StartupService? startupService;
    private HotkeyService? hotkeyService;
    private OverlayManager? overlayManager;
    private FocusRegionController? focusRegionController;
    private VisualEffectController? visualEffectController;
    private SettingsWindow? settingsWindow;
    private DispatcherTimer? saveTimer;
    private OverlaySettings? settings;

    protected override void OnStartup(StartupEventArgs e)
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        base.OnStartup(e);

        monitorManager = new MonitorManager();
        settingsService = new SettingsService();
        startupService = new StartupService();

        var primary = monitorManager.GetPrimaryMonitor();
        settings = settingsService.Load(primary.WorkingArea);
        settings.LaunchAtStartup = startupService.IsEnabled();

        focusRegionController = new FocusRegionController(monitorManager, settings);
        visualEffectController = new VisualEffectController(settings);
        hotkeyService = new HotkeyService();
        overlayManager = new OverlayManager(monitorManager, focusRegionController, visualEffectController);
        settingsWindow = new SettingsWindow(settings, focusRegionController, visualEffectController, startupService);

        focusRegionController.StateChanged += OnStateChanged;
        visualEffectController.VisualEffectChanged += OnStateChanged;
        settingsWindow.SettingsChanged += SettingsWindow_SettingsChanged;
        settingsWindow.StartupChanged += SettingsWindow_StartupChanged;

        saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        saveTimer.Tick += SaveTimer_Tick;

        ConfigureNotifyIcon();
        ConfigureHotkeys();
        SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
        overlayManager.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        PersistSettings();
        hotkeyService?.Dispose();
        overlayManager?.Dispose();

        if (settingsWindow is not null)
        {
            settingsWindow.AllowClose();
            settingsWindow.Close();
        }

        if (notifyIcon is not null)
        {
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
        }

        SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;

        base.OnExit(e);
    }

    private void ConfigureNotifyIcon()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("设置", null, (_, _) => ShowSettings());
        menu.Items.Add("切换屏幕", null, (_, _) => ToggleMonitor());
        menu.Items.Add("隐藏/恢复遮罩", null, (_, _) => ToggleOverlayVisibility());
        menu.Items.Add("退出", null, (_, _) => Shutdown());

        notifyIcon = new Forms.NotifyIcon
        {
            Icon = DrawingSystemIcons.Application,
            Text = "FocusFrame 聚焦遮罩",
            Visible = true,
            ContextMenuStrip = menu
        };
        notifyIcon.DoubleClick += (_, _) => ShowSettings();
    }

    private void ConfigureHotkeys()
    {
        if (hotkeyService is null || settings is null)
        {
            return;
        }

        hotkeyService.UnregisterAll();

        var switchRegistered = hotkeyService.Register(
            ToggleMonitorHotkeyId,
            new HotkeyBinding(settings.MovePickModifiers, settings.MovePickVirtualKey),
            ToggleMonitor);

        var overlayRegistered = hotkeyService.Register(
            ToggleOverlayHotkeyId,
            new HotkeyBinding(settings.ToggleOverlayModifiers, settings.ToggleOverlayVirtualKey),
            ToggleOverlayVisibility);

        settingsWindow?.SetHotkeyStatus(switchRegistered, overlayRegistered);
    }

    private void ToggleMonitor()
    {
        if (monitorManager is null || focusRegionController is null)
        {
            return;
        }

        var monitors = monitorManager.GetMonitors();
        if (monitors.Count < 2)
        {
            return;
        }

        var currentIndex = -1;
        for (var index = 0; index < monitors.Count; index++)
        {
            if (monitors[index].Id == focusRegionController.ActiveMonitor.Id)
            {
                currentIndex = index;
                break;
            }
        }

        if (currentIndex < 0)
        {
            currentIndex = 0;
        }

        var nextIndex = (currentIndex + 1) % monitors.Count;
        focusRegionController.SwitchToMonitor(monitors[nextIndex]);
    }

    private void ToggleOverlayVisibility()
    {
        overlayManager?.ToggleVisibility();
    }

    private void ShowSettings()
    {
        if (settingsWindow is null)
        {
            return;
        }

        if (!settingsWindow.IsVisible)
        {
            settingsWindow.Show();
        }

        if (settingsWindow.WindowState == WindowState.Minimized)
        {
            settingsWindow.WindowState = WindowState.Normal;
        }

        settingsWindow.Activate();
    }

    private void SystemEvents_DisplaySettingsChanged(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            focusRegionController?.RefreshMonitorLayout();
            overlayManager?.RequestRefresh(forceAll: true);
        }), DispatcherPriority.Background);
    }

    private void SettingsWindow_SettingsChanged(object? sender, EventArgs e)
    {
        ConfigureHotkeys();
        overlayManager?.RequestRefresh(forceAll: true);
        ScheduleSave();
    }

    private void SettingsWindow_StartupChanged(object? sender, bool enabled)
    {
        if (settings is not null)
        {
            settings.LaunchAtStartup = enabled;
        }

        ScheduleSave();
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        overlayManager?.RequestRefresh();
        ScheduleSave();
    }

    private void ScheduleSave()
    {
        saveTimer?.Stop();
        saveTimer?.Start();
    }

    private void SaveTimer_Tick(object? sender, EventArgs e)
    {
        saveTimer?.Stop();
        PersistSettings();
    }

    private void PersistSettings()
    {
        if (settings is null || settingsService is null || focusRegionController is null || visualEffectController is null)
        {
            return;
        }

        focusRegionController.CaptureState(settings);
        settings.TintColor = visualEffectController.TintHex;
        settings.DimOpacity = visualEffectController.OpacityByte;
        settingsService.Save(settings);
    }
}
