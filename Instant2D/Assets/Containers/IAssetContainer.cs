namespace Instant2D.Assets.Containers {
    /// <summary>
    /// Serves as typed container for different kinds of Asset classes.
    /// </summary>
    public interface IAssetContainer<T> {
        T Content { get; }
    }
}
