using Instant2D.Modules;

namespace Instant2D;

// TODO: implement timestamps feature for TimeManager
public class Time : IGameSystem {
    /// <summary>
    /// Total time that has passed since beginning of the game.
    /// </summary>
    public static float Total { get; private set; }

    /// <summary>
    /// Time that has passed since the last frame.
    /// </summary>
    public static float Delta { get; private set; }

    /// <summary>
    /// Total number of frames passed.
    /// </summary>
    public static int FrameCount { get; private set; }

    // time calculations should happen before anything else
    float IGameSystem.UpdateOrder => float.MinValue;
    void IGameSystem.Initialize(InstantApp app) { }
    void IGameSystem.Update(InstantApp app, float deltaTime) {
        Total += deltaTime;
        Delta = deltaTime;
        FrameCount++;
    }
}
