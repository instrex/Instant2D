using Instant2D.Core;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FAudio;

namespace Instant2D.Audio {
    public class AudioManager : SubSystem, IDisposable {
        public readonly IntPtr AudioHandle;

        public readonly FAudioDeviceDetails DeviceDetails;

        public readonly IntPtr MasteringVoice;

        public float DopplerScale = 1.0f;

        internal List<WeakReference<StreamingAudioInstance>> _streamingInstances = new();
        bool _isDisposed;

        public unsafe AudioManager() {
            FAudioCreate(out AudioHandle, 0, 0);
            FAudio_GetDeviceCount(AudioHandle, out var deviceCount);

            // TODO: make it run even without audio connected
            if (deviceCount <= 0) {
                FAudio_Release(AudioHandle);
                throw new InvalidOperationException("No audio device has been found.");
            }

            uint deviceIndex;

            // loop over audio devices and find the one that fits
            for (deviceIndex = 0; deviceIndex < deviceCount; deviceIndex++) {
                FAudio_GetDeviceDetails(AudioHandle, deviceIndex, out var details);
                if ((details.Role & FAudioDeviceRole.FAudioDefaultGameDevice) == FAudioDeviceRole.FAudioDefaultGameDevice) {
                    DeviceDetails = details;
                    break;
                }

                // this is the last one and we still haven't found anything...
                // fallback to the first device then
                else if (deviceIndex + 1 >= deviceCount) {
                    FAudio_GetDeviceDetails(AudioHandle, 0, out DeviceDetails);
                }
            }

            // TODO: make it run even without audio connected
            if (FAudio_CreateMasteringVoice(AudioHandle, out MasteringVoice, FAUDIO_DEFAULT_CHANNELS, FAUDIO_DEFAULT_SAMPLERATE, 0, deviceIndex, IntPtr.Zero) != 0) {
                FAudio_Release(AudioHandle);
                throw new InvalidOperationException("Couldn't create mastering voice.");
            }
        }

        public override void Initialize() {
            IsUpdatable = true;
        }

        public override void Update(GameTime time) {
            for (var i = _streamingInstances.Count - 1; i >= 0; i--) {
                if (_streamingInstances[i].TryGetTarget(out var instance))
                    instance.Update();
                else _streamingInstances.RemoveAt(i);
            }
        }

        #region Disposal

        protected virtual void Dispose(bool disposing) {
            if (!_isDisposed) {
                if (disposing) {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _isDisposed = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~AudioManager() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
