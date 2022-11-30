using Instant2D.Core;
using Instant2D.EC;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Graphics {
    /// <summary>
    /// An extension of FNA's <see cref="SpriteBatch"/> class with a couple of useful features.
    /// </summary>
    public class DrawingContext : IDisposable {
		public readonly GraphicsDevice GraphicsDevice;

		const int MAX_SPRITES = 2048;
		const int MAX_VERTICES = MAX_SPRITES * 4;
		const int MAX_INDICES = MAX_SPRITES * 6;

		// helper struct for tracking batch properties
		record struct BatchInfo {
			public Material Material;
			public Matrix TransformMatrix;
			public bool ImmediateMode;
		}

		/// <summary>
		/// When set to <see langword="true"/>, destination positions of Draw methods will be rounded before submitting. <br/>
		/// Works best for pixel art, in other cases it shouldn't really matter if this is set.
		/// </summary>
		public bool EnableRounding = false;

		// batching info
		bool _batchBegun;
		Stack<BatchInfo> _batchStack = new();
		Material _currentMaterial;

		// FNA buffers for batching
		DynamicVertexBuffer _vertexBuffer;
        IndexBuffer _indexBuffer;

		// CPU batched data
		VertexPositionColorTexture4[] _vertexInfo;
		Texture2D[] _textureInfo;

		// drawing info
		bool _isBatchingDisabled;
		int _spriteCount;
		Effect _effect;

		// TODO: use for primitive batching
		readonly BasicEffect _basicEffect;

		// default sprite effect
		readonly EffectParameter _spriteMatrixParam;
		readonly Effect _spriteEffect;

		// material state
		BlendState _blendState;
		SamplerState _samplerState;
		DepthStencilState _depthStencilState;
		RasterizerState _rasterizerState;
		
		// transformation matrices
		Matrix _transformMatrix, _projectionMatrix;
		Matrix _tempMatrix;

		// IDisposable
        bool _isDisposed;

		// Used to calculate texture coordinates
		readonly float[] _cornerOffsetX = new float[] { 0.0f, 1.0f, 0.0f, 1.0f };
		readonly float[] _cornerOffsetY = new float[] { 0.0f, 0.0f, 1.0f, 1.0f };
		readonly short[] _indexData;

		public DrawingContext() : this(InstantGame.Instance.GraphicsDevice) { }
		public DrawingContext(GraphicsDevice graphicsDevice) {
			GraphicsDevice = graphicsDevice;

			// generate index array
			_indexData = new short[MAX_INDICES];
			for (int i = 0, j = 0; i < MAX_INDICES; i += 6, j += 4) {
				_indexData[i] = (short)j;
				_indexData[i + 1] = (short)(j + 1);
				_indexData[i + 2] = (short)(j + 2);
				_indexData[i + 3] = (short)(j + 3);
				_indexData[i + 4] = (short)(j + 2);
				_indexData[i + 5] = (short)(j + 1);
			}

			// initialize FNA buffers
			_vertexBuffer = new DynamicVertexBuffer(graphicsDevice, typeof(VertexPositionColorTexture), MAX_VERTICES, BufferUsage.WriteOnly);
			_indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.SixteenBits, MAX_INDICES, BufferUsage.WriteOnly);
			_indexBuffer.SetData(_indexData);

			// initialize CPU buffers
			_vertexInfo = new VertexPositionColorTexture4[MAX_SPRITES];
			_textureInfo = new Texture2D[MAX_SPRITES];

			// initialize default effect
			_spriteEffect = new Effect(GraphicsDevice, typeof(DrawingContext).Assembly.GetManifestResourceStream("Microsoft.Xna.Framework.Graphics.Effect.Resources.SpriteEffect.fxb").ReadBytes(true));
			_spriteMatrixParam = _spriteEffect.Parameters["MatrixTransform"];
			_basicEffect = new BasicEffect(graphicsDevice) {
				VertexColorEnabled = true,
				TextureEnabled = true,
				World = Matrix.Identity,
			};

			// initialize projection matrix
			_projectionMatrix = new Matrix(0f, 0.0f, 0.0f, 0.0f, 0.0f, 0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, -1.0f, 1.0f, 0.0f, 1.0f);
		}

		void PrepareRenderState() {
			// set graphics device state
			GraphicsDevice.BlendState = _blendState;
			GraphicsDevice.SamplerStates[0] = _samplerState;
			GraphicsDevice.DepthStencilState = _depthStencilState;
			GraphicsDevice.RasterizerState = _rasterizerState;

			// set buffers
			GraphicsDevice.SetVertexBuffer(_vertexBuffer);
			GraphicsDevice.Indices = _indexBuffer;

			// update projection matrix
			var viewport = GraphicsDevice.Viewport /*new Rectangle(0, 0, SceneManager.Instance.Current.Resolution.Width, SceneManager.Instance.Current.Resolution.Height)*/;
            //(_projectionMatrix.M11, _projectionMatrix.M22) = ((float)(2.0 / (viewport.Width / 2 * 2 - 1)), (float)(-2.0 / (viewport.Height / 2 * 2 - 1)));
            _projectionMatrix.M11 = (float)(2.0 / (double)viewport.Width);
            _projectionMatrix.M22 = (float)(-2.0 / (double)viewport.Height);
			// TODO:  wtf is going on here
            //_projectionMatrix.M11 = (float)(2.0 / (double)(viewport.Width / 2 * 2 - 1));
            //_projectionMatrix.M22 = (float)(-2.0 / (double)(viewport.Height / 2 * 2 - 1));
            //(_projectionMatrix.M41, _projectionMatrix.M42) = (-1 - 0.5f * _projectionMatrix.M11, 1 - 0.5f * _projectionMatrix.M22);

			Matrix.Multiply(ref _transformMatrix, ref _projectionMatrix, out _tempMatrix);

			_spriteMatrixParam.SetValue(_tempMatrix);
			_spriteEffect.CurrentTechnique.Passes[0].Apply();
		}

		void DrawPrimitives(Texture tex, int index, int batchSize) {
			if (_effect != null) {
				for (var i = 0; i < _effect.CurrentTechnique.Passes.Count; i++) {
					_effect.CurrentTechnique.Passes[i].Apply();

					GraphicsDevice.Textures[0] = tex;
					GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, index * 4, 0, batchSize * 4, 0, batchSize * 2);
                }

				return;
            }

			GraphicsDevice.Textures[0] = tex;
			GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, index * 4, 0, batchSize * 4, 0, batchSize * 2);
		}

		void CheckBegin() {
			if (!_batchBegun) {
				throw new InvalidOperationException("The batch wasn't started.");
            }
        }

		#region Unsafe drawing methods (dangerous territory)

		public unsafe void Flush() {
			if (_spriteCount == 0)
				return;

			Texture currentTexture = null;
			var spriteOffset = 0;

			PrepareRenderState();

			// update the vertex buffer
			fixed (VertexPositionColorTexture4* p = &_vertexInfo[0]) {
				_vertexBuffer.SetDataPointerEXT(0, (IntPtr)p, _spriteCount * VertexPositionColorTexture4.RealStride, SetDataOptions.Discard);
			}

			currentTexture = _textureInfo[0];
			for (var i = 0; i < _spriteCount; i++) {
				if (_textureInfo[i] != currentTexture) {
					// when texture changes, flush all of the existing sprites
					DrawPrimitives(currentTexture, spriteOffset, i - spriteOffset);

					// and update offsets
					currentTexture = _textureInfo[i];
					spriteOffset = i;
                }
            }

			// draw the final batch and reset the sprite counter
			DrawPrimitives(currentTexture, spriteOffset, _spriteCount - spriteOffset);

			_spriteCount = 0;
		}

		// this is a huge chunk of code taken from https://github.com/prime31/Nez/ (I have no idea how it works)
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		unsafe void PushSprite(Texture2D texture, Rectangle? sourceRectangle, float destinationX, float destinationY,
						float destinationW, float destinationH, Color color, Vector2 origin,
						float rotation, float depth, byte effects, bool destSizeInPixels, float skewTopX,
						float skewBottomX, float skewLeftY, float skewRightY) {
			// out of space, flush
			if (_spriteCount >= MAX_SPRITES)
				Flush();

			if (EnableRounding) {
				destinationX = MathF.Floor(destinationX);
				destinationY = MathF.Floor(destinationY);
			}

			// Source/Destination/Origin Calculations
			float sourceX, sourceY, sourceW, sourceH;
			float originX, originY;
			if (sourceRectangle.HasValue) {
				var inverseTexW = 1.0f / (float)texture.Width;
				var inverseTexH = 1.0f / (float)texture.Height;

				sourceX = sourceRectangle.Value.X * inverseTexW;
				sourceY = sourceRectangle.Value.Y * inverseTexH;
				sourceW = sourceRectangle.Value.Width * inverseTexW;
				sourceH = sourceRectangle.Value.Height * inverseTexH;

				originX = (origin.X / sourceW) * inverseTexW;
				originY = (origin.Y / sourceH) * inverseTexH;

				if (!destSizeInPixels) {
					destinationW *= sourceRectangle.Value.Width;
					destinationH *= sourceRectangle.Value.Height;
				}
			} else {
				sourceX = 0.0f;
				sourceY = 0.0f;
				sourceW = 1.0f;
				sourceH = 1.0f;

				originX = origin.X * (1.0f / texture.Width);
				originY = origin.Y * (1.0f / texture.Height);

				if (!destSizeInPixels) {
					destinationW *= texture.Width;
					destinationH *= texture.Height;
				}
			}

			// Rotation Calculations
			float rotationMatrix1X;
			float rotationMatrix1Y;
			float rotationMatrix2X;
			float rotationMatrix2Y;
			if (MathF.Abs(rotation) > float.Epsilon) {
				var sin = MathF.Sin(rotation);
				var cos = MathF.Cos(rotation);
				rotationMatrix1X = cos;
				rotationMatrix1Y = sin;
				rotationMatrix2X = -sin;
				rotationMatrix2Y = cos;
			} else {
				rotationMatrix1X = 1.0f;
				rotationMatrix1Y = 0.0f;
				rotationMatrix2X = 0.0f;
				rotationMatrix2Y = 1.0f;
			}

            // flip our skew values if we have a flipped sprite
            if (effects != 0) {
                skewTopX *= -1;
                skewBottomX *= -1;
                skewLeftY *= -1;
                skewRightY *= -1;
            }

            fixed (VertexPositionColorTexture4* vertexInfo = &_vertexInfo[_spriteCount]) {
				// calculate vertices
				// top-left
				var cornerX = (_cornerOffsetX[0] - originX) * destinationW + skewTopX;
				var cornerY = (_cornerOffsetY[0] - originY) * destinationH - skewLeftY;
				vertexInfo->Position0.X = (
					(rotationMatrix2X * cornerY) +
					(rotationMatrix1X * cornerX) +
					destinationX
				);
				vertexInfo->Position0.Y = (
					(rotationMatrix2Y * cornerY) +
					(rotationMatrix1Y * cornerX) +
					destinationY
				);

				// top-right
				cornerX = (_cornerOffsetX[1] - originX) * destinationW + skewTopX;
				cornerY = (_cornerOffsetY[1] - originY) * destinationH - skewRightY;
				vertexInfo->Position1.X = (
					(rotationMatrix2X * cornerY) +
					(rotationMatrix1X * cornerX) +
					destinationX
				);
				vertexInfo->Position1.Y = (
					(rotationMatrix2Y * cornerY) +
					(rotationMatrix1Y * cornerX) +
					destinationY
				);

				// bottom-left
				cornerX = (_cornerOffsetX[2] - originX) * destinationW + skewBottomX;
				cornerY = (_cornerOffsetY[2] - originY) * destinationH - skewLeftY;
				vertexInfo->Position2.X = (
					(rotationMatrix2X * cornerY) +
					(rotationMatrix1X * cornerX) +
					destinationX
				);
				vertexInfo->Position2.Y = (
					(rotationMatrix2Y * cornerY) +
					(rotationMatrix1Y * cornerX) +
					destinationY
				);

				// bottom-right
				cornerX = (_cornerOffsetX[3] - originX) * destinationW + skewBottomX;
				cornerY = (_cornerOffsetY[3] - originY) * destinationH - skewRightY;
				vertexInfo->Position3.X = (
					(rotationMatrix2X * cornerY) +
					(rotationMatrix1X * cornerX) +
					destinationX
				);
				vertexInfo->Position3.Y = (
					(rotationMatrix2Y * cornerY) +
					(rotationMatrix1Y * cornerX) +
					destinationY
				);

				vertexInfo->TextureCoordinate0.X = (_cornerOffsetX[0 ^ effects] * sourceW) + sourceX;
				vertexInfo->TextureCoordinate0.Y = (_cornerOffsetY[0 ^ effects] * sourceH) + sourceY;
				vertexInfo->TextureCoordinate1.X = (_cornerOffsetX[1 ^ effects] * sourceW) + sourceX;
				vertexInfo->TextureCoordinate1.Y = (_cornerOffsetY[1 ^ effects] * sourceH) + sourceY;
				vertexInfo->TextureCoordinate2.X = (_cornerOffsetX[2 ^ effects] * sourceW) + sourceX;
				vertexInfo->TextureCoordinate2.Y = (_cornerOffsetY[2 ^ effects] * sourceH) + sourceY;
				vertexInfo->TextureCoordinate3.X = (_cornerOffsetX[3 ^ effects] * sourceW) + sourceX;
				vertexInfo->TextureCoordinate3.Y = (_cornerOffsetY[3 ^ effects] * sourceH) + sourceY;
				vertexInfo->Position0.Z = depth;
				vertexInfo->Position1.Z = depth;
				vertexInfo->Position2.Z = depth;
				vertexInfo->Position3.Z = depth;
				vertexInfo->Color0 = color;
				vertexInfo->Color1 = color;
				vertexInfo->Color2 = color;
				vertexInfo->Color3 = color;
			}

			if (_isBatchingDisabled) {
				_vertexBuffer.SetData(0, _vertexInfo, 0, 1, VertexPositionColorTexture4.RealStride, SetDataOptions.None);
				DrawPrimitives(texture, 0, 1);
				return;
			}

			_textureInfo[_spriteCount++] = texture;
		}

		#endregion

		#region Public API

		/// <summary>
		/// Temporarily replaces current rendering properties with another <paramref name="material"/>, <paramref name="transformMatrix"/> or <paramref name="immediateMode"/> setting. <br/>
		/// This could be used to render something with a different blend mode, sampler state or etc mid-batch. Note that calling this flushes the current batch though. <br/>
		/// You must call <see cref="Pop"/> immediately after drawing, otherwise an error would be produced.
		/// </summary>
		public void Push(Material material, Matrix? transformMatrix = default, bool? immediateMode = default) {
			if (!_batchBegun) {
				Begin(material, transformMatrix ?? Matrix.Identity, immediateMode ?? false);
				return;
			}

			// save previous state
			var prevState = new BatchInfo {
				Material = _currentMaterial,
				ImmediateMode = _isBatchingDisabled,
				TransformMatrix = _transformMatrix
			};

			End();
			Begin(material, transformMatrix ?? prevState.TransformMatrix, immediateMode ?? prevState.ImmediateMode);

			// push the prev state to retrieve it later in Pop()
			_batchStack.Push(prevState);
        }

		/// <summary>
		/// Return to previous batch after calling <see cref="Push(Material, Matrix?, bool?)"/>.
		/// </summary>
		public void Pop() {
			if (!_batchStack.TryPop(out var prevBatch)) {
				//throw new InvalidOperationException("No batch to Pop.");
				End();
				return;
            }

			End();
			Begin(prevBatch.Material, prevBatch.TransformMatrix, prevBatch.ImmediateMode);
        }

		/// <summary>
		/// Begins a new batch. Pass in <see langword="true"/> for <paramref name="immediateMode"/> to disable batching and draw sprites immediately.
		/// </summary>
		public void Begin(Material material, Matrix transformMatrix, bool immediateMode = false) {
			_batchBegun = true;

			// set material properties
			_currentMaterial = material;
			_blendState = material.BlendState;
			_rasterizerState = material.RasterizerState;
			_depthStencilState = material.DepthStencilState;
			_samplerState = material.SamplerState;
			_effect = material.Effect;

			// set transform matrices
			_transformMatrix = transformMatrix;

			// prepare render state if batching is disabled
			if (_isBatchingDisabled = immediateMode) {
				PrepareRenderState();
            }
        }

		/// <summary>
		/// Ends the current batch, flushing all the sprites (if set to non-immediate mode).
		/// </summary>
		public void End() {
			if (!_batchBegun) {
				throw new InvalidOperationException("The batch wasn't started.");
            }

			// flush the batch if not immediate
			if (!_isBatchingDisabled)
				Flush();

			_batchBegun = false;
			_effect = null;
        }

		/// <summary>
		/// Renders a texture using provided properties.
		/// </summary>
		public void DrawTexture(Texture2D texture,
			Vector2 position,
			Rectangle? sourceRectangle,
			Color color,
			float rotation,
			Vector2 origin,
			Vector2 scale,
			SpriteEffects spriteEffects = SpriteEffects.None,
			float layerDepth = 0f) {
			CheckBegin();
			PushSprite(
				texture,
				sourceRectangle,
				position.X,
				position.Y,
				scale.X,
				scale.Y,
				color,
				origin,
				rotation,
				layerDepth,
				(byte)(spriteEffects & (SpriteEffects)0x03),
				false,
				0, 0, 0, 0
			);
		}

		/// <summary>
		/// Renders a sprite using provided properties.
		/// </summary>
		public void DrawSprite(in Sprite sprite, Vector2 position, Color color, float rotation, Vector2 scale, SpriteEffects spriteEffects = SpriteEffects.None, float layerDepth = 0f) {
			CheckBegin();
			PushSprite(
				sprite.Texture,
				sprite.SourceRect,
				position.X,
				position.Y,
				scale.X,
				scale.Y,
				color,
				sprite.Origin,
				rotation,
				layerDepth,
				(byte)(spriteEffects & (SpriteEffects)0x03),
				false,
				0, 0, 0, 0
			);
		}

        #endregion

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
		struct VertexPositionColorTexture4 : IVertexType {
			public const int RealStride = 96;

			VertexDeclaration IVertexType.VertexDeclaration => throw new NotImplementedException();

			public Vector3 Position0;
			public Color Color0;
			public Vector2 TextureCoordinate0;
			public Vector3 Position1;
			public Color Color1;
			public Vector2 TextureCoordinate1;
			public Vector3 Position2;
			public Color Color2;
			public Vector2 TextureCoordinate2;
			public Vector3 Position3;
			public Color Color3;
			public Vector2 TextureCoordinate3;
		}

        protected virtual void Dispose(bool disposing) {
            if (!_isDisposed) {
                if (disposing) {
                    // TODO: dispose managed state (managed objects)
                }

                _isDisposed = true;

				_spriteEffect.Dispose();
				_basicEffect.Dispose();
				_vertexBuffer.Dispose();
				_indexBuffer.Dispose();
            }
        }

        // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~DrawingContext() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        void IDisposable.Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

    }
}
