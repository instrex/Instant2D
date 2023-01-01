using Instant2D.RectanglePacking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Assets.Sprites {
    public struct TexturePacker {
        public struct PackedTexture {
            public Texture2D tex;
            public Rectangle rect;
            public int page;
        }

        public struct Page {
            public PackedTexture[] textures;
            public Texture2D sheet;
        }

        readonly Texture2D[] _masterTextures;
        readonly int _maxTextureSize;

        public TexturePacker(Texture2D[] texturesToPack, int maxTextureSize) {
            _maxTextureSize = maxTextureSize;
            _masterTextures = texturesToPack;
        }

        public Page[] GetPages() {
            var result = new List<Page>();
            var spriteBuffer = new List<PackedTexture>();

            // pack all the textures into pages
            var textures = new Stack<Texture2D>(_masterTextures);
            var packer = new ArevaloRectanglePacker(_maxTextureSize, _maxTextureSize);
            while (textures.Count > 0) {
                var tex = textures.Peek();
                if (packer.TryPack(tex.Width, tex.Height, out var placement)) {
                    textures.Pop();
                    spriteBuffer.Add(new PackedTexture {
                        page = result.Count,
                        rect = new Rectangle(placement.X, placement.Y, tex.Width, tex.Height),
                        tex = tex
                    });

                    continue;
                }

                // save the page and create a new packer
                packer = new ArevaloRectanglePacker(_maxTextureSize, _maxTextureSize);
                result.Add(new Page { textures = spriteBuffer.ToArray() });
                spriteBuffer.Clear();
            }

            // if there are leftover sprites, add them before drawing
            if (spriteBuffer.Count > 0) {
                result.Add(new Page { textures = spriteBuffer.ToArray() });
                spriteBuffer.Clear();
            }

            // perform black magic ritual
            var pages = new Page[result.Count];
            for (var i = 0; i < pages.Length; i++) {
                var rt = DrawPage(result[i].textures);
                pages[i] = result[i] with { sheet = rt };

                // notify of the progress
                InstantApp.Logger.Info($"Finished drawing 'spritesheet_{i}': {rt.Width}x{rt.Height} with {pages[i].textures.Length} items.");
            }

            return pages;
        }

        static int ClosestPow2(int k) {
            k--;
            for (int i = 1; i < sizeof(int) * 8; i <<= 1)
                k |= k >> i;
            return k + 1;
        }

        RenderTarget2D DrawPage(in PackedTexture[] textures) {
            var gd = InstantApp.Instance.GraphicsDevice;

            var textureSize = -1;
            for (var i = 0; i < textures.Length; i++) {
                var texture = textures[i];

                // expand to the left
                if (texture.rect.Right > textureSize)
                    textureSize = texture.rect.Right;

                // expand to the right
                if (texture.rect.Bottom > textureSize)
                    textureSize = texture.rect.Bottom;
            }

            // normalize the texture size
            textureSize = Math.Min(_maxTextureSize, ClosestPow2(textureSize));

            var rt = new RenderTarget2D(gd, textureSize, textureSize);
            gd.SetRenderTarget(rt);
            gd.Clear(Color.Transparent);

            using var spriteBatch = new SpriteBatch(gd);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied);

            for (var i = 0; i < textures.Length; i++) {
                spriteBatch.Draw(textures[i].tex, textures[i].rect, Color.White);
            }

            spriteBatch.End();

            gd.SetRenderTarget(null);

            return rt;
        }
    }
}
