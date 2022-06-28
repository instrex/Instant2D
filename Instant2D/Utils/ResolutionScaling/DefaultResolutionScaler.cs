using Microsoft.Xna.Framework;
using System;

namespace Instant2D.Utils.ResolutionScaling {
    /// <summary>
    /// Default implementation of <see cref="IResolutionScaler"/>. Has options for pixel perfect scaling and design resolution, as well as display modes.
    /// </summary>
    public class DefaultResolutionScaler : IResolutionScaler {
        /// <summary>
        /// Settings for how scenes should be presented to user.
        /// </summary>
        public enum ScreenDisplayMode {
            /// <summary>
            /// If only this is set, nothing will be cut off and resolution will keep aspect ratio of the window.
            /// </summary>
            ShowAll = 0,

            /// <summary>
            /// The excess space will be cut off horizontally. You can control the letterbox color.
            /// </summary>
            HorizontalLetterbox = 1,

            /// <summary>
            /// The excess space will be cut off vertically. You can control the letterbox color.
            /// </summary>
            VerticalLetterbox = 2,

            /// <summary>
            /// The excess space will be cut off entirely. You can control the letterbox color.
            /// </summary>
            CutOff = HorizontalLetterbox | VerticalLetterbox,
        }

        Point _designResolution;
        ScreenDisplayMode _displayMode;
        bool _isPixelPerfect;

        /// <summary>
        /// Display mode to use during resolution scaling. Check <see cref="ScreenDisplayMode"/> descriptions for more info.
        /// </summary>
        public ScreenDisplayMode DisplayMode {
            get => _displayMode;
            set => _displayMode = value;
        }

        /// <summary>
        /// Whether or not the scaling factor will round down to a nearest integer to prevent pixel art distortions.
        /// </summary>
        public bool IsPixelPerfect {
            get => _isPixelPerfect;
            set => _isPixelPerfect = value;
        }

        /// <summary>
        /// Default design resolution.
        /// </summary>
        public Point DesignResolution {
            get => _designResolution;
            set => _designResolution = value;
        }

        #region Setters

        /// <inheritdoc cref="DisplayMode"/>
        public DefaultResolutionScaler SetDisplayMode(ScreenDisplayMode displayMode) {
            _displayMode = displayMode;
            return this;
        }

        /// <inheritdoc cref="DesignResolution"/>
        public DefaultResolutionScaler SetDesignResolution(int width, int height) {
            _designResolution = new Point(width, height);
            return this;
        }

        /// <inheritdoc cref="IsPixelPerfect"/>
        public DefaultResolutionScaler SetPixelPerfect(bool isPixelPerfect = true) {
            _isPixelPerfect = isPixelPerfect;
            return this;
        }

        #endregion

        public ScaledResolution Calculate(Point screenDimensions) {
            var (scaleX, scaleY) = (screenDimensions.X / (float)_designResolution.X, screenDimensions.Y / (float)_designResolution.Y);

            // calculate the scale factor
            var rawScale = MathF.Max(1.0f, MathF.Min(scaleX, scaleY));
            var scale = rawScale;

            // correct the scale if pixel-perfect
            if (_isPixelPerfect) {
                scale = MathF.Floor(rawScale);
            }

            // DISPLAY MODE: CutOff
            // position the screen in the center, adjusting offset to fit it 
            if (_displayMode == ScreenDisplayMode.CutOff) {
                return new() {
                    offset = screenDimensions.ToVector2() * 0.5f - _designResolution.ToVector2() * 0.5f * scale,
                    renderTargetSize = _designResolution,
                    rawScreenSize = screenDimensions,
                    scaleFactor = scale,
                };
            }

            // DISPLAY MODE: ShowAll
            // simply downscale the rendertarget based on design resolution
            if (_displayMode == ScreenDisplayMode.ShowAll) {
                return new() {
                    offset = Vector2.Zero,
                    renderTargetSize = (screenDimensions.ToVector2() / scale).RoundToPoint(),
                    rawScreenSize = screenDimensions,
                    scaleFactor = scale
                };
            }

            // :(
            throw new NotImplementedException($"{_displayMode} is not supported yet.");
        }
    }
}
