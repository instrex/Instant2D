using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Utils.Math {
    /// <summary>
    /// A nice wrapper for working with int flag values. May be converted in/out of int values.
    /// </summary>
    public struct IntFlags {
        /// <summary>
        /// Represents an <see cref="IntFlags"/> instance with all the flags flipped to <see langword="true"/>.
        /// </summary>
        public static IntFlags All => new IntFlags().Invert();

        int _bitMask;

        /// <summary>
        /// Creates a new <see cref="IntFlags"/> instance with specified flags set.
        /// </summary>
        public IntFlags(params int[] flags) {
            _bitMask = 0;
            for (var i = 0; i < flags.Length; i++) {
                SetFlag(flags[i]);
            }
        }

        /// <summary>
        /// Checks if a flag is set. By default, asumes that the provided <paramref name="flag"/> is unshifted.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsFlagSet(int flag, bool isUnshifted = true) {
            if (isUnshifted) {
                flag = 1 << flag;
            }

            return (_bitMask & flag) != 0;
        }

        /// <summary>
        /// Resets the value to that of only one flag set. 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntFlags SetFlagExclusive(int flag, bool isUnshifted = true) {
            if (isUnshifted) {
                flag = 1 << flag;
            }

            _bitMask = flag;
            return this;
        }

        /// <summary>
        /// Sets an unshifted flag to <see langword="true"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntFlags SetFlag(int flag) {
            _bitMask |= 1 << flag;
            return this;
        }

        /// <summary>
        /// Removes an unshifted flag.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntFlags ClearFlag(int flag) {
            _bitMask &= ~(1 << flag);
            return this;
        }

        /// <summary>
        /// Flips the bits to the opposite values.
        /// </summary>
        public IntFlags Invert() {
            _bitMask = ~_bitMask;
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator int(IntFlags flags) => flags._bitMask;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator IntFlags(int mask) => new IntFlags().SetFlagExclusive(mask, false);
    }
}
