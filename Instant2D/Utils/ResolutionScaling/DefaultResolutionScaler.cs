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
        public enum DisplayMode {
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
        DisplayMode _displayMode;
        bool _isPixelPerfect;
        float _minScale;

        /// <summary>
        /// Display mode to use during resolution scaling. Check <see cref="DisplayMode"/> descriptions for more info.
        /// </summary>
        public DisplayMode Mode {
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

        /// <summary>
        /// The minimal scaling value.
        /// </summary>
        public float MinimalScale {
            get => _minScale;
            set => _minScale = value;
        }

        #region Setters

        /// <inheritdoc cref="DisplayMode"/>
        public DefaultResolutionScaler SetDisplayMode(DisplayMode displayMode) {
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

        /// <inheritdoc cref="MinimalScale"/>
        public DefaultResolutionScaler SetMinimalScale(float minScale) {
            _minScale = minScale;
            return this;
        }


        #endregion

        public ScaledResolution Calculate(Point screenDimensions) {
            var (scaleX, scaleY) = (screenDimensions.X / (float)_designResolution.X, screenDimensions.Y / (float)_designResolution.Y);

            // calculate the scale factor
            var rawScale = MathF.Max(1.0f, MathF.Min(scaleX, scaleY));
            var scale = rawScale;

            // clamp the scale
            if (_minScale > 0) {
                scale = MathF.Max(_minScale, scale);
            }

            // correct the scale if pixel-perfect
            if (_isPixelPerfect) {
                scale = MathF.Floor(scale);
            }

            var result = new ScaledResolution { 
                rawScreenSize = screenDimensions,
                scaleFactor = scale
            };

            // prepare the results
            switch (_displayMode) {
                default: throw new NotImplementedException($"{_displayMode} is not supported yet.");

                case DisplayMode.CutOff:
                    result.offset = screenDimensions.ToVector2() * 0.5f - _designResolution.ToVector2() * 0.5f * scale;
                    result.renderTargetSize = _designResolution;
                    break;

                case DisplayMode.ShowAll:
                    result.renderTargetSize = (screenDimensions.ToVector2() / scale).RoundToPoint();
                    break;
            }

            return result;
        }
    }
}
