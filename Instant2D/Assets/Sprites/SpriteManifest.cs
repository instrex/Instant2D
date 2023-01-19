using Microsoft.Xna.Framework;

namespace Instant2D.Assets.Sprites {
    /// <summary>
    /// A class that houses all the sprite information you've defined in 'sprites/*' asset folder.
    /// </summary>
    public readonly record struct SpriteManifest {
        /// <summary>
        /// Specifies default way of naming subsprites/animation frames. Defaults to <c>"{0}_{1}"</c>.
        /// </summary>
        public static string DefaultNamingFormat { get; set; } = "{0}_{1}";

        /// <summary>
        /// Default sprite origin for each manifest. Note that default origins can only be defined as normalized.
        /// </summary>
        public static Vector2 DefaultSpriteOrigin { get; set; } = new(0.5f);

        /// <summary>
        /// Filename or the key of this manifest.
        /// </summary>
        public string Key { get; init; }

        /// <summary>
        /// All the sprite definition entries defined in this manifest.
        /// </summary>
        public SpriteDefinition[] Items { get; init; }

        /// <summary>
        /// Controls default origin for sprites in this manifest. This value will be used only if sprite doesn't specify origin explicitly.
        /// </summary>
        public Vector2 SpriteOrigin { get; init; }

        /// <summary>
        /// Controls how subsprites/animation frames are named in this manifest. Defaults to <see cref="DefaultNamingFormat"/>.
        /// </summary>
        public string NamingFormat { get; init; }
    }
}
