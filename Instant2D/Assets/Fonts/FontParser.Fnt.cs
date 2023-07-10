using Instant2D.Assets.Loaders;
using Instant2D.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Instant2D.Assets.Fonts;

/// <summary>
/// Class for parsing fonts of various formats.
/// </summary>
public static partial class FontParser {
    static int ParseCharAttribute(string[] items, string attributeName) {
        if (Array.Find(items, i => i.StartsWith(attributeName)) is not string raw || !int.TryParse(raw[(attributeName.Length + 1)..], out var value))
            throw new InvalidOperationException($"'{attributeName}' attribute of 'char' is missing or invalid.");

        return value;
    }

    /// <summary>
    /// Attempts to parse and load '.fnt' font definition produced by BMFont.
    /// </summary>
    public static I2dFont LoadFnt(string input, Texture2D[] pages = default) {
        var lines = input.Split('\n');
        if (Array.Find(lines, l => l.StartsWith("common")) is not string infoLine) 
            throw new InvalidOperationException("'common' property is missing.");
        
        var infos = infoLine.Split(' ');
        if (Array.Find(infos, i => i.StartsWith("lineHeight=")) is not string rawLineHeight || Array.Find(infos, i => i.StartsWith("pages=")) is not string rawPagesCount)
            throw new InvalidOperationException("'common' property is missing 'lineHeight' and/or 'pages' attributes.");

        if (!int.TryParse(rawLineHeight["lineHeight=".Length..], out var lineHeight))
            throw new InvalidOperationException($"'lineHeight' attribute has invalid value. ({rawLineHeight["lineHeight=".Length..]})");

        if (!int.TryParse(rawPagesCount["pages=".Length..], out var pagesCount) || pagesCount < 0)
            throw new InvalidOperationException($"'pages' attribute has invalid value. ({rawPagesCount["pages=".Length..]})");

        if (pages is null) {
            pages = new Texture2D[pagesCount];
            foreach (var pageDef in lines.Where(l => l.StartsWith("page"))) {
                var items = pageDef.Split(' ');
                if (Array.Find(items, i => i.StartsWith("id")) is not string rawId || Array.Find(items, i => i.StartsWith("file")) is not string rawPath)
                    throw new InvalidOperationException("'page' property is missing 'id' and/or 'file' attributes.");

                if (!int.TryParse(rawId["id=".Length..], out var id) || id < 0 || id >= pages.Length)
                    throw new InvalidOperationException($"'id' attribute has invalid value. ({rawId["id".Length..]})");

                // load the page
                using var stream = AssetManager.Instance.OpenStream(Path.Combine(FontLoader.DIRECTORY, rawPath["file=".Length..].Trim('"')) + ".png");
                pages[id] = Texture2D.FromStream(InstantApp.Instance.GraphicsDevice, stream);
            }
        }

        var glyphs = new Dictionary<char, ISpriteFont.Glyph>(100);
        foreach (var charDef in lines.Where(l => l.StartsWith("char"))) {
            var items = charDef.Split(' ');
            var (id, x, y, w, h, offX, offY, advX, pageId) = (
                ParseCharAttribute(items, "id"),
                ParseCharAttribute(items, "x"),
                ParseCharAttribute(items, "y"),
                ParseCharAttribute(items, "width"),
                ParseCharAttribute(items, "height"),
                ParseCharAttribute(items, "xoffset"),
                ParseCharAttribute(items, "yoffset"),
                ParseCharAttribute(items, "xadvance"),
                ParseCharAttribute(items, "page")
            );

            glyphs.Add((char)id, new(
                new Sprite(pages[pageId], new(x, y, w, h), Vector2.Zero),
                (char)id,
                new(offX, offY),
                advX
            ));
        }

        return new I2dFont {
            LineSpacing = lineHeight,
            Glyphs = glyphs,
        };
    }
}
