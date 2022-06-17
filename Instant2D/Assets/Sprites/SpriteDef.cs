namespace Instant2D.Assets.Sprites {
    /// <summary>
    /// An item definition stored in <see cref="SpriteManifest"/>, note that it might be an individual sprite or sprite collection.
    /// </summary>
    public struct SpriteDef {
        /// <summary>
        /// This sprite's path relative to 'Assets/sprite/'
        /// </summary>
        public string fileName;

        /// <summary>
        /// Key of this sprite used to access it, defaults to <see cref="fileName"/>.
        /// </summary>
        public string key;

        /// <summary>
        /// Split options for this sprite.
        /// </summary>
        public SpriteSplit split;

        /// <summary>
        /// Origin data for this sprite.
        /// </summary>
        public SpriteOrigin origin;

        /// <summary>
        /// Sprite animation data (if present)
        /// </summary>
        public SpriteAnimationDef? animation;
    }
}
