using Instant2D.Core;
using Instant2D.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Instant2D;

namespace Instant2D.Graphics {
    /// <summary>
    /// Implementation of <see cref="IDrawingBackend"/> using FNA's <see cref="SpriteBatch"/>. 
    /// Probably needs to be replaced with something more performant. (later)
    /// </summary>
    public class SpriteBatchBackend : IDrawingBackend, IDisposable {
        readonly struct BatchState {
            public Material Material { get; init; }
            public Matrix Transform { get; init; }

            public void Deconstruct(out Material material, out Matrix transform) {
                transform = Transform;
                material = Material;
            }
        }

        readonly SpriteBatch _spriteBatch;
        readonly Stack<BatchState> _states = new(6);
        int _batchDepth;

        public SpriteBatchBackend() {
            _spriteBatch = new SpriteBatch(InstantGame.Instance.GraphicsDevice);
        }

        public void Draw(in Sprite sprite, Vector2 position, Color color, float rotation, Vector2 scale, SpriteEffects spriteEffects = SpriteEffects.None) {
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

        public void DrawTexture(Texture2D texture, Vector2 position, Color color, float rotation, Vector2 scale, Vector2 origin, SpriteEffects spriteEffects = SpriteEffects.None, Rectangle? sourceRect = null) {
            if (_batchDepth == 0) {
                throw new InvalidOperationException("Cannot Draw: the batch didn't begin.");
            }

            _spriteBatch.Draw(
                texture,
                position,
                sourceRect,
                color,
                rotation,
                origin,
                scale,
                spriteEffects,
                0
            );
        }

        public void Pop(bool endCompletely = false) {
            if (_batchDepth == 0) {
                throw new InvalidOperationException("Cannot Pop the batch: it didn't begin.");
            }

            _spriteBatch.End();
            if (endCompletely) {
                _states.Clear();
                _batchDepth = 0;

                return;
            }

            // restart the batch if ending wasn't requested
            _states.Pop();
            _batchDepth--;

            // retrieve previous batch
            if (_batchDepth > 0) {
                var (material, transform) = _states.Peek();
                _spriteBatch.Begin(material.Effect != null ? SpriteSortMode.Immediate : SpriteSortMode.Deferred, material.BlendState,
                    material.SamplerState, material.DepthStencilState, material.RasterizerState, material.Effect, transform);
            }
        }

        public void Push(in Material material, Matrix transformMatrix = default) {
            if (_batchDepth > 0) {
                _spriteBatch.End();
            }

            if (transformMatrix == default) {
                transformMatrix = _batchDepth > 0 ? _states.Peek().Transform : Matrix.Identity;
            }

            // apply material's properties
            _spriteBatch.Begin(material.Effect != null ? SpriteSortMode.Immediate : SpriteSortMode.Deferred, material.BlendState,
                material.SamplerState, material.DepthStencilState, material.RasterizerState, material.Effect, transformMatrix);

            // push the state
            _states.Push(new BatchState { Material = material, Transform = transformMatrix });
            _batchDepth++;
        }

        void IDisposable.Dispose() {
            _spriteBatch.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
