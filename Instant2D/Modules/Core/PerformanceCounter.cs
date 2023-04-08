using Instant2D.Modules;
using System;
using System.Diagnostics;

namespace Instant2D;

/// <summary>
/// Handy class with diagnostic values.
/// </summary>
public class PerformanceCounter : IGameSystem {
    Process _currentProcess;
    DateTimeOffset _lastCpuCheck;
    TimeSpan _lastCpuUsage;
    float _updateTimer;
    int _fpsCounter;

    /// <summary>
    /// Current FPS value.
    /// </summary>
    public static int FramesPerSecond { get; private set; }

    /// <summary>
    /// Current CPU usage.
    /// </summary>
    public static float CpuUsage { get; private set; }

    /// <summary>
    /// Current GC memory usage in MB.
    /// </summary>
    public static float MemoryUsage { get; private set; }

    /// <summary>
    /// When <see langword="true"/>, performance metrics will be displayed in game's title.
    /// </summary>
    public bool DisplayInTitle { get; set; }

    void CalculateCpuUsage() {
        var now = DateTimeOffset.UtcNow;
        var cpuUsage = _currentProcess.TotalProcessorTime;
        var usedCpuMs = (float)(cpuUsage - _lastCpuUsage).TotalMilliseconds;
        var timePassedMs = (float)(now - _lastCpuCheck).TotalMilliseconds;
        CpuUsage = usedCpuMs / (Environment.ProcessorCount * timePassedMs);

        // cache the previous values
        _lastCpuUsage = cpuUsage;
        _lastCpuCheck = now;
    }

    float IGameSystem.UpdateOrder => float.MinValue + 0.1f;
    void IGameSystem.Initialize(InstantApp app) {
        _currentProcess = Process.GetCurrentProcess();
    }

    void IGameSystem.Update(InstantApp app, float deltaTime) {
        _updateTimer += deltaTime;
        _fpsCounter++;

        if (_updateTimer > 1.0f) {
            MemoryUsage = GC.GetTotalMemory(false) / 1048576f;
            FramesPerSecond = _fpsCounter;
            CalculateCpuUsage();

            _updateTimer--;
            _fpsCounter = 0;
        }

        if (DisplayInTitle) {
            app.Window.Title = $"{app.DefaultTitle} [{FramesPerSecond} FPS, {MemoryUsage:F1} MB, {CpuUsage:P0} CPU]";
        }
    }
}

