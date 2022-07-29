using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Utils {
    /// <summary>
    /// A nice wrapper for working with int flag values. May also be used via int extensions.
    /// </summary>
    public static class IntFlags {
        /// <summary>
        /// Checks if a flag is set. By default, asumes that the provided <paramref name="flag"/> is unshifted.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFlagSet(this int value, int flag, bool isUnshifted = true) {
            if (isUnshifted) {
                flag = 1 << flag;
            }

            return (value & flag) != 0;
        }

        /// <summary>
        /// Resets the value to that of only one flag set. 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SetFlagExclusive(int flag, bool isUnshifted = true) {
            if (isUnshifted) {
                flag = 1 << flag;
            }

            return flag;
        }

        /// <summary>
        /// Sets an unshifted flag to <see langword="true"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SetFlag(this int value, int flag) {
            return value |= 1 << flag;
        }

        /// <summary>
        /// Removes an unshifted flag.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RemoveFlag(this int value, int flag) {
            return value &= ~(1 << flag);
        }

        /// <summary>
        /// Flips the bits to their opposite values.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FlipFlags(this int value) {
            return ~value;
        }
    }
}
