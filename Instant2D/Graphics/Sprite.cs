using Instant2D.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D;

/// <summary>
/// Wrapper for <see cref="Texture2D"/>s, SourceRects and Origins.
/// </summary>
public readonly struct Sprite {
    public Texture2D Texture { get; init; }
    public Rectangle SourceRect { get; init; }
    public Dictionary<string, Point> Points { get; init; }
    public string Key { get; init; }

    readonly Vector2 _origin;

    /// <summary>
    /// Direct access to sprite's origin.
    /// </summary>
    public Vector2 Origin { 
        get => _origin;
        init => _origin = value.Round();
    }

    /// <summary>
    /// Access to normalized origin, uses values ranging [0.0 - 1.0]
    /// </summary>
    public Vector2 NormalizedOrigin {
        get => _origin / new Vector2(SourceRect.Width, SourceRect.Height);
        init => Origin = new Vector2(SourceRect.Width, SourceRect.Height) * value;
    }

    /// <summary>
    /// Initializes <see cref="SourceRect"/> to <paramref name="texture"/>'s size and <see cref="Origin"/> to center.
    /// </summary>
    /// <param name="texture"></param>
    public Sprite(Texture2D texture, string key = default) {
        Texture = texture;
        SourceRect = new(0, 0, Texture.Width, Texture.Height);
        Points = null;
        Key = key;

        _origin = new(Texture.Width / 2, Texture.Height / 2);
    }

    public Sprite(Texture2D texture, Rectangle sourceRect, Vector2 origin, string key = default) {
        Texture = texture;
        SourceRect = sourceRect;
        Points = null;
        Key = key;

        _origin = origin.Round();
    }

    public override string ToString() => $"{Key ?? "???"}, {SourceRect}, {Origin}";

    // smooth conversion to SpriteAnimation for easier prototyping
    public static implicit operator SpriteAnimation(Sprite sprite) => new() { Frames = [sprite], Fps = 1.0f, Key = sprite.Key };
}
