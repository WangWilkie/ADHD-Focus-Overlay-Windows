using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using AdhdFocusOverlay.Controllers;
using AdhdFocusOverlay.Models;
using AdhdFocusOverlay.Services;
using MediaColor = System.Windows.Media.Color;
using WpfColor = System.Windows.Media.Color;

namespace AdhdFocusOverlay.Views;

public partial class SettingsWindow : Window
{
    private readonly FocusRegionController focusRegionController;
    private readonly VisualEffectController visualEffectController;
    private readonly StartupService startupService;
    private readonly OverlaySettings settings;
    private bool allowClose;
    private bool loading = true;

    public SettingsWindow(
        OverlaySettings settings,
        FocusRegionController focusRegionController,
        VisualEffectController visualEffectController,
        StartupService startupService)
    {
        InitializeComponent();
        this.settings = settings;
        this.focusRegionController = focusRegionController;
        this.visualEffectController = visualEffectController;
        this.startupService = startupService;

        var color = VisualEffectController.ParseColor(settings.TintColor);
        RedSlider.Value = color.R;
        GreenSlider.Value = color.G;
        BlueSlider.Value = color.B;
        OpacitySlider.Value = settings.DimOpacity;
        DesaturationCheckBox.IsChecked = settings.DesaturationEnabled;
        StartupCheckBox.IsChecked = settings.LaunchAtStartup;
        SwitchMonitorHotkeyTextBox.Text = FormatHotkey(settings.MovePickModifiers, settings.MovePickVirtualKey);
        ToggleOverlayHotkeyTextBox.Text = FormatHotkey(settings.ToggleOverlayModifiers, settings.ToggleOverlayVirtualKey);
        RefreshPreview();
        loading = false;
    }

    public event EventHandler<bool>? StartupChanged;

    public event EventHandler? SettingsChanged;

    public void AllowClose()
    {
        allowClose = true;
    }

    public void SetHotkeyStatus(bool switchRegistered, bool overlayRegistered)
    {
        if (switchRegistered && overlayRegistered)
        {
            HotkeyStatusText.Text = "当前快捷键已生效。";
            HotkeyStatusText.Foreground = new SolidColorBrush(WpfColor.FromRgb(102, 102, 102));
            return;
        }

        var message = switchRegistered
            ? "隐藏/恢复遮罩快捷键注册失败，可能与系统或其他软件冲突。"
            : overlayRegistered
                ? "切换屏幕快捷键注册失败，可能与系统或其他软件冲突。"
                : "两个快捷键都注册失败，可能与系统或其他软件冲突。";
        HotkeyStatusText.Text = message;
        HotkeyStatusText.Foreground = new SolidColorBrush(WpfColor.FromRgb(178, 34, 34));
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!allowClose)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        base.OnClosing(e);
    }

    private void ColorSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        RefreshPreview();
    }

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        RefreshPreview();
    }

    private void SettingsControl_Changed(object sender, RoutedEventArgs e)
    {
        ApplySettings();
    }

    private void StartupCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        if (loading)
        {
            return;
        }

        var enabled = StartupCheckBox.IsChecked == true;
        startupService.SetEnabled(enabled, Environment.ProcessPath!);
        StartupChanged?.Invoke(this, enabled);
    }

    private void ResetFocusButton_Click(object sender, RoutedEventArgs e)
    {
        focusRegionController.ResetToDefault();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void SwitchMonitorHotkeyTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        RecordHotkey(
            e,
            (modifiers, virtualKey) =>
            {
                settings.MovePickModifiers = modifiers;
                settings.MovePickVirtualKey = virtualKey;
                SwitchMonitorHotkeyTextBox.Text = FormatHotkey(modifiers, virtualKey);
            });
    }

    private void ToggleOverlayHotkeyTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        RecordHotkey(
            e,
            (modifiers, virtualKey) =>
            {
                settings.ToggleOverlayModifiers = modifiers;
                settings.ToggleOverlayVirtualKey = virtualKey;
                ToggleOverlayHotkeyTextBox.Text = FormatHotkey(modifiers, virtualKey);
            });
    }

    private void RecordHotkey(System.Windows.Input.KeyEventArgs e, Action<uint, int> applyHotkey)
    {
        if (loading)
        {
            return;
        }

        e.Handled = true;
        var actualKey = e.Key == Key.System ? e.SystemKey : e.Key;
        if (IsModifierKey(actualKey))
        {
            return;
        }

        var modifiers = ToNativeModifiers(Keyboard.Modifiers);
        if (modifiers == 0)
        {
            HotkeyStatusText.Text = "请至少按住一个修饰键再录制快捷键。";
            HotkeyStatusText.Foreground = new SolidColorBrush(WpfColor.FromRgb(178, 34, 34));
            return;
        }

        var virtualKey = KeyInterop.VirtualKeyFromKey(actualKey);
        applyHotkey(modifiers, virtualKey);
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RefreshPreview()
    {
        var color = MediaColor.FromRgb((byte)RedSlider.Value, (byte)GreenSlider.Value, (byte)BlueSlider.Value);
        var opacity = (byte)OpacitySlider.Value;
        PreviewSwatch.Background = new SolidColorBrush(MediaColor.FromArgb(opacity, color.R, color.G, color.B));
        OpacityValueText.Text = $"{opacity}/255";

        if (!loading)
        {
            visualEffectController.Update(color, opacity);
            settings.TintColor = visualEffectController.TintHex;
            settings.DimOpacity = opacity;
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void ApplySettings()
    {
        if (loading)
        {
            return;
        }

        settings.DesaturationEnabled = DesaturationCheckBox.IsChecked == true;
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    private static bool IsModifierKey(Key key)
    {
        return key is Key.LeftAlt or Key.RightAlt or
               Key.LeftCtrl or Key.RightCtrl or
               Key.LeftShift or Key.RightShift or
               Key.LWin or Key.RWin;
    }

    private static uint ToNativeModifiers(ModifierKeys modifiers)
    {
        uint value = 0;
        if (modifiers.HasFlag(ModifierKeys.Alt)) value |= 0x0001;
        if (modifiers.HasFlag(ModifierKeys.Control)) value |= 0x0002;
        if (modifiers.HasFlag(ModifierKeys.Shift)) value |= 0x0004;
        if (modifiers.HasFlag(ModifierKeys.Windows)) value |= 0x0008;
        return value;
    }

    private static string FormatHotkey(uint modifiers, int virtualKey)
    {
        var parts = new List<string>();
        if ((modifiers & 0x0008) != 0) parts.Add("Win");
        if ((modifiers & 0x0002) != 0) parts.Add("Ctrl");
        if ((modifiers & 0x0001) != 0) parts.Add("Alt");
        if ((modifiers & 0x0004) != 0) parts.Add("Shift");
        parts.Add(FormatVirtualKey(virtualKey));
        return string.Join(" + ", parts);
    }

    private static string FormatVirtualKey(int virtualKey)
    {
        if (virtualKey >= 0x41 && virtualKey <= 0x5A)
        {
            return ((char)virtualKey).ToString();
        }

        if (virtualKey >= 0x30 && virtualKey <= 0x39)
        {
            return ((char)virtualKey).ToString();
        }

        if (virtualKey >= 0x70 && virtualKey <= 0x7B)
        {
            return $"F{virtualKey - 0x6F}";
        }

        return virtualKey switch
        {
            0x20 => "Space",
            0x09 => "Tab",
            0xC0 => "`",
            0xBD => "-",
            0xBB => "=",
            _ => $"VK {virtualKey:X2}"
        };
    }
}
