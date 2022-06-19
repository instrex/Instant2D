using Instant2D.Core;
using Instant2D.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Instant2D;

namespace Instant2D.Graphics {
    /// <summary>
    /// Implementation of <see cref="DrawingBackend"/> using FNA's <see cref="SpriteBatch"/>. 
    /// Probably needs to be replaced with something more performant. (later)
    /// </summary>
    public class SpriteBatchBackend : DrawingBackend, IDisposable {
        readonly SpriteBatch _spriteBatch;
        readonly Stack<Material> _materials = new(6);
        Matrix _finalMatrix = Matrix.Identity;
        int _batchDepth;

        public SpriteBatchBackend() {
            _spriteBatch = new SpriteBatch(InstantGame.Instance.GraphicsDevice);
        }

        public override void Draw(in Sprite sprite, Vector2 position, Color color, float rotation, Vector2 scale, SpriteEffects spriteEffects = SpriteEffects.None) {
            if (_batchDepth == 0) {
                throw new InvalidOperationException("Cannot Draw: the batch didn't begin.");
            }

            _spriteBatch.Draw(
                sprite.Texture,
                position,
                sprite.SourceRect,
                color,
                rotation,
                sprite.Origin,
                scale,
                spriteEffects,
                0
            );
        }

        public override void Pop(bool endCompletely = false) {
            if (_batchDepth == 0) {
                throw new InvalidOperationException("Cannot Pop the batch: it didn't begin.");
            }

            _spriteBatch.End();

            if (endCompletely) {
                _materials.Clear();
                _batchDepth = 0;

                return;
            }

            // restart the batch if ending wasn't requested
            _materials.Pop();
            _batchDepth--;

            // retrieve previous batch
            if (_batchDepth > 0) {
                var material = _materials.Peek();
                _spriteBatch.Begin(material.Effect != null ? SpriteSortMode.Immediate : SpriteSortMode.Deferred, material.BlendState,
                    material.SamplerState, material.DepthStencilState, material.RasterizerState, material.Effect, _finalMatrix);
            }
        }

        public override void Push(in Material material) {
            if (_batchDepth > 0) {
                _spriteBatch.End();
            }

            // apply material's properties
            _spriteBatch.Begin(material.Effect != null ? SpriteSortMode.Immediate : SpriteSortMode.Deferred, material.BlendState,
                material.SamplerState, material.DepthStencilState, material.RasterizerState, material.Effect, _finalMatrix);

            // push the material
            _materials.Push(material);
            _batchDepth++;
        }

        void IDisposable.Dispose() {
            _spriteBatch.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
