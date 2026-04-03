using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using AdhdFocusOverlay.Controllers;
using AdhdFocusOverlay.Models;
using AdhdFocusOverlay.Views;

namespace AdhdFocusOverlay.Services;

public sealed class OverlayManager : IDisposable
{
    private static readonly TimeSpan InteractiveRefreshInterval = TimeSpan.FromMilliseconds(16);

    private readonly Dictionary<string, OverlayWindow> overlayWindows = new();
    private readonly FocusRegionController focusRegionController;
    private readonly VisualEffectController visualEffectController;
    private readonly MonitorManager monitorManager;
    private readonly InteractionWindow interactionWindow;
    private readonly DispatcherTimer interactiveRefreshTimer;
    private IReadOnlyList<MonitorInfo> monitorSnapshot = Array.Empty<MonitorInfo>();
    private string? lastRenderedActiveMonitorId;
    private bool fullRefreshRequested = true;
    private bool interactionActive;
    private bool overlaysVisible = true;
    private bool pickModeEnabled;
    private bool refreshQueued;

    public OverlayManager(
        MonitorManager monitorManager,
        FocusRegionController focusRegionController,
        VisualEffectController visualEffectController)
    {
        this.monitorManager = monitorManager;
        this.focusRegionController = focusRegionController;
        this.visualEffectController = visualEffectController;
        interactionWindow = new InteractionWindow(focusRegionController);
        interactionWindow.InteractionStateChanged += InteractionWindow_InteractionStateChanged;
        interactiveRefreshTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = InteractiveRefreshInterval
        };
        interactiveRefreshTimer.Tick += InteractiveRefreshTimer_Tick;
    }

    public event EventHandler<MonitorInfo>? MonitorClicked;

    public void Show()
    {
        RebuildWindows();
        interactionWindow.Show();
        Refresh();
        System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(Refresh), DispatcherPriority.Loaded);
        System.Windows.Application.Current.Dispatcher.BeginInvoke(
            new Action(() => RequestRefresh(forceAll: true)),
            DispatcherPriority.ContextIdle);
    }

    public void SetPickMode(bool enabled)
    {
        pickModeEnabled = enabled;
        fullRefreshRequested = true;
        foreach (var window in overlayWindows.Values)
        {
            window.SetPickMode(enabled);
        }

        interactionWindow.SetInteractionEnabled(overlaysVisible && !enabled);
    }

    public bool OverlaysVisible => overlaysVisible;

    public void ToggleVisibility()
    {
        overlaysVisible = !overlaysVisible;

        foreach (var window in overlayWindows.Values)
        {
            if (overlaysVisible)
            {
                window.Show();
            }
            else
            {
                window.Hide();
            }
        }

        if (overlaysVisible)
        {
            interactionWindow.Show();
            interactionWindow.SetInteractionEnabled(!pickModeEnabled);
            RequestRefresh(forceAll: true);
        }
        else
        {
            interactionWindow.Hide();
        }
    }

    public void RequestRefresh(bool forceAll = false)
    {
        fullRefreshRequested |= forceAll;
        if (interactionActive)
        {
            if (!interactiveRefreshTimer.IsEnabled)
            {
                interactiveRefreshTimer.Start();
            }

            return;
        }

        if (refreshQueued)
        {
            return;
        }

        refreshQueued = true;
        System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
        {
            refreshQueued = false;
            Refresh();
        }), DispatcherPriority.Render);
    }

    public void Refresh()
    {
        if (!overlaysVisible)
        {
            refreshQueued = false;
            return;
        }

        if (fullRefreshRequested || monitorSnapshot.Count == 0)
        {
            UpdateMonitorSnapshot();
            if (overlayWindows.Count != monitorSnapshot.Count || monitorSnapshot.Any(monitor => !overlayWindows.ContainsKey(monitor.Id)))
            {
                RebuildWindows();
            }
        }

        var activeMonitorId = focusRegionController.ActiveMonitor.Id;
        var redrawAll = fullRefreshRequested || lastRenderedActiveMonitorId != activeMonitorId;

        if (!redrawAll && overlayWindows.TryGetValue(activeMonitorId, out var activeWindow))
        {
            var activeMonitor = FindMonitor(activeMonitorId);
            if (activeMonitor is not null)
            {
                activeWindow.UpdateState(
                    activeMonitor,
                    focusRegionController.FocusRegion,
                    visualEffectController,
                    pickModeEnabled,
                    interactionActive);
            }
        }
        else
        {
            foreach (var monitor in monitorSnapshot)
            {
                if (!redrawAll &&
                    monitor.Id != activeMonitorId &&
                    monitor.Id != lastRenderedActiveMonitorId)
                {
                    continue;
                }

                overlayWindows[monitor.Id].UpdateState(
                    monitor,
                    monitor.Id == activeMonitorId ? focusRegionController.FocusRegion : null,
                    visualEffectController,
                    pickModeEnabled,
                    interactionActive && monitor.Id == activeMonitorId);
            }

            RefreshOverlayZOrder();
        }

        interactionWindow.SetInteractionEnabled(!pickModeEnabled);
        interactionWindow.SynchronizeBounds();
        lastRenderedActiveMonitorId = activeMonitorId;
        fullRefreshRequested = false;
    }

    public void Dispose()
    {
        interactiveRefreshTimer.Stop();
        interactionWindow.InteractionStateChanged -= InteractionWindow_InteractionStateChanged;
        interactionWindow.Close();
        foreach (var window in overlayWindows.Values)
        {
            window.Close();
        }

        overlayWindows.Clear();
    }

    private void RebuildWindows()
    {
        foreach (var window in overlayWindows.Values)
        {
            window.Close();
        }

        overlayWindows.Clear();
        UpdateMonitorSnapshot();

        foreach (var monitor in monitorSnapshot)
        {
            var window = new OverlayWindow();
            window.MonitorClicked += (_, info) => MonitorClicked?.Invoke(this, info);
            overlayWindows.Add(monitor.Id, window);
            if (overlaysVisible)
            {
                window.Show();
            }
        }

        fullRefreshRequested = true;
        lastRenderedActiveMonitorId = null;
        SetPickMode(pickModeEnabled);
    }

    private void InteractionWindow_InteractionStateChanged(object? sender, bool active)
    {
        interactionActive = active;
        if (active)
        {
            if (!interactiveRefreshTimer.IsEnabled)
            {
                interactiveRefreshTimer.Start();
            }

            return;
        }

        interactiveRefreshTimer.Stop();
        RequestRefresh(forceAll: true);
    }

    private void InteractiveRefreshTimer_Tick(object? sender, EventArgs e)
    {
        if (!interactionActive)
        {
            interactiveRefreshTimer.Stop();
            return;
        }

        Refresh();
    }

    private void UpdateMonitorSnapshot()
    {
        monitorSnapshot = monitorManager.GetMonitors();
    }

    private MonitorInfo? FindMonitor(string monitorId)
    {
        foreach (var monitor in monitorSnapshot)
        {
            if (monitor.Id == monitorId)
            {
                return monitor;
            }
        }

        return null;
    }

    private void RefreshOverlayZOrder()
    {
        foreach (var window in overlayWindows.Values)
        {
            window.RefreshTopmost();
        }
    }
}
