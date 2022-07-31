using Microsoft.Xna.Framework;

namespace Instant2D.Assets.Sprites {
    /// <summary>
    /// An item definition stored in <see cref="SpriteManifest"/>, note that it might be an individual sprite or sprite collection.
    /// </summary>
    public record SpriteDef {
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

        /// <summary>
        /// Type of this definition, could be <see cref="SpriteDefType.Sprite"/> or <see cref="SpriteDefType.Texture"/>.
        /// </summary>
        public SpriteDefType type;

        /// <summary>
        /// Sprite manifest reference this definition belongs to.
        /// </summary>
        public SpriteManifest manifest;

        /// <summary>
        /// Checks if this definition is default - has nothing defined other than fileName and key.
        /// </summary>
        public bool IsDefault => manifest == null
            && split.type == SpriteSplitOptions.None
            && origin.type == SpriteOriginType.Default
            && animation == null;

        /// <summary>
        /// Helper method for easily formatting frame keys. If <see cref="manifest"/> is null, <see cref="SpriteManifest.DEFAULT_FRAME_FORMAT"/> will be used.
        /// <code> '{0}_{1}' -> 'sprite_0'</code>
        /// </summary>
        public string FormatFrameKey(string key) => string.Format(manifest?.FrameFormat ?? SpriteManifest.DEFAULT_FRAME_FORMAT, this.key, key);

        /// <summary>
        /// Gets an actual sprite origin.
        /// </summary>
        public static Vector2 TransformOrigin(SpriteOrigin origin, Rectangle sourceRect, SpriteManifest manifest = default, SpriteOrigin? parent = default) {
            var size = new Vector2(sourceRect.Width, sourceRect.Height);
            return origin.type switch {
                SpriteOriginType.Absolute => origin.value,
                SpriteOriginType.Normalized => size * origin.value,
                SpriteOriginType.Default when parent is not null => TransformOrigin(parent.Value, sourceRect, manifest),
                SpriteOriginType.Default when manifest is not null => size * manifest.DefaultOrigin,
                _ => size * new Vector2(0.5f),
            };
        }
    }
}
