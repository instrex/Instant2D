using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Assets.Sprites {
    public partial record struct SpriteDefinition {
        /// <summary>
        /// Sprite origin description. May be default, normalized or absolute.
        /// </summary>
        public record struct OriginDefinition(Vector2? OriginValue, bool IsNormalized) {
            public Vector2 Transform(Rectangle sourceRect, SpriteManifest? manifest = default, OriginDefinition? parentOrigin = default) {
                if (OriginValue is Vector2 origin) {
                    // calculate normalized origins when needed using sourceRect size
                    return IsNormalized ? new Vector2(sourceRect.Width * origin.X, sourceRect.Height * origin.Y) : origin;
                }

                if (parentOrigin is OriginDefinition parent) {
                    // try get parent origin when defined
                    return parent.Transform(sourceRect, manifest);
                }

                return new Vector2(sourceRect.Width, sourceRect.Height) * (manifest?.SpriteOrigin ?? new Vector2(0.5f));
            }
        }
    }
}
