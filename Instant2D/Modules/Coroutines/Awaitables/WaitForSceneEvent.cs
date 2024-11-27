using Instant2D.EC;

namespace Instant2D.Coroutines;

/// <summary>
/// Await for specific Scene events such as FixedUpdate, Update or LateUpdate. <br/>
/// NOTE: using <see cref="WaitForSceneEvent"/> with <see cref="SceneManager.TickCoroutineEvents"/> set to <see langword="false"/> will freeze the coroutine
/// unless you manually call <see cref="Coroutine.TickBlockedCoroutines(SceneLoopEvent)"/>.
/// </summary>
/// <param name="Scene">The scene to await events from, inferred automatically if not specified.</param>
public record WaitForSceneEvent(SceneLoopEvent EventType, Scene Scene = default) : ICoroutineAwaitable {

    /// <summary>
    /// In case when multiple scenes are loaded, only events from specified scene will be processed.
    /// </summary>
    public Scene Scene { get; private set; } = Scene ?? SceneManager.Instance?.Current;

    void ICoroutineAwaitable.Initialize(Coroutine coroutine) {
        Scene ??= coroutine.Target switch {
            Entity entityTarget => entityTarget.Scene,
            Scene sceneTarget => sceneTarget,
            _ => SceneManager.Instance?.Current,
        };

        Coroutine.RegisterSceneLoopBlockedCoroutine(coroutine, EventType);
    }

    bool ICoroutineAwaitable.Tick(Coroutine coroutine) => false;
}
