using System;
using System.Threading;
using System.Threading.Tasks;
using IEC104.Constants;
using IEC104.Protocol.Enum;

namespace IEC104.Adapter
{
    /// <summary>
    /// Manages interrogation operations for IEC104 devices
    /// </summary>
    public class InterrogationAdapter : IDisposable
    {
        #region FIELDS

        private DeviceAdapter deviceAdapter;
        private Timer generalInterrogationTimer;
        private Timer counterInterrogationTimer;
        private DateTime lastGeneralInterrogation;
        private DateTime lastCounterInterrogation;
        private bool disposed = false;

        #endregion

        #region PROPERTIES

        public bool AutoGeneralInterrogation { get; set; } = true;
        public int GeneralInterrogationInterval { get; set; } = 300000; // 5 minutes
        public bool AutoCounterInterrogation { get; set; } = false;
        public int CounterInterrogationInterval { get; set; } = 3600000; // 1 hour

        #endregion

        #region EVENTS

        public event EventHandler<InterrogationEventArgs> InterrogationStarted;
        public event EventHandler<InterrogationEventArgs> InterrogationCompleted;
        public event EventHandler<InterrogationEventArgs> InterrogationFailed;

        #endregion

        #region CONSTRUCTOR

        public InterrogationAdapter(DeviceAdapter deviceAdapter)
        {
            this.deviceAdapter = deviceAdapter ?? throw new ArgumentNullException(nameof(deviceAdapter));

            lastGeneralInterrogation = DateTime.MinValue;
            lastCounterInterrogation = DateTime.MinValue;
        }

        #endregion

        #region TIMER MANAGEMENT

        /// <summary>
        /// Start automatic interrogation timers
        /// </summary>
        public void StartTimers()
        {
            StopTimers();

            if (AutoGeneralInterrogation && GeneralInterrogationInterval > 0)
            {
                generalInterrogationTimer = new Timer(OnGeneralInterrogationTimer, null,
                                                    GeneralInterrogationInterval,
                                                    GeneralInterrogationInterval);
            }

            if (AutoCounterInterrogation && CounterInterrogationInterval > 0)
            {
                counterInterrogationTimer = new Timer(OnCounterInterrogationTimer, null,
                                                    CounterInterrogationInterval,
                                                    CounterInterrogationInterval);
            }
        }

        /// <summary>
        /// Stop automatic interrogation timers
        /// </summary>
        public void StopTimers()
        {
            generalInterrogationTimer?.Dispose();
            counterInterrogationTimer?.Dispose();
            generalInterrogationTimer = null;
            counterInterrogationTimer = null;
        }

        #endregion

        #region INTERROGATION OPERATIONS

        /// <summary>
        /// Send General Interrogation (synchronous)
        /// </summary>
        public bool SendGeneralInterrogation()
        {
            if (!deviceAdapter.Connected)
                return false;

            try
            {
                OnInterrogationStarted(InterrogationType.General);

                deviceAdapter.SendGeneralInterrogation();
                lastGeneralInterrogation = DateTime.Now;

                // Note: In real implementation, we'd wait for confirmation
                // For now, assume success
                OnInterrogationCompleted(InterrogationType.General);

                return true;
            }
            catch (Exception ex)
            {
                OnInterrogationFailed(InterrogationType.General, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Send General Interrogation (asynchronous)
        /// </summary>
        public async Task<bool> SendGeneralInterrogationAsync()
        {
            return await Task.Run(() => SendGeneralInterrogation());
        }

        /// <summary>
        /// Send Counter Interrogation (synchronous)
        /// </summary>
        public bool SendCounterInterrogation()
        {
            if (!deviceAdapter.Connected)
                return false;

            try
            {
                OnInterrogationStarted(InterrogationType.Counter);

                deviceAdapter.SendCounterInterrogation();
                lastCounterInterrogation = DateTime.Now;

                OnInterrogationCompleted(InterrogationType.Counter);

                return true;
            }
            catch (Exception ex)
            {
                OnInterrogationFailed(InterrogationType.Counter, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Send Counter Interrogation (asynchronous)
        /// </summary>
        public async Task<bool> SendCounterInterrogationAsync()
        {
            return await Task.Run(() => SendCounterInterrogation());
        }

        #endregion

        #region TIMER CALLBACKS

        private void OnGeneralInterrogationTimer(object state)
        {
            if (deviceAdapter.Connected)
            {
                Task.Run(() => SendGeneralInterrogation());
            }
        }

        private void OnCounterInterrogationTimer(object state)
        {
            if (deviceAdapter.Connected)
            {
                Task.Run(() => SendCounterInterrogation());
            }
        }

        #endregion

        #region STATUS

        /// <summary>
        /// Get interrogation status information
        /// </summary>
        public InterrogationStatus GetStatus()
        {
            return new InterrogationStatus
            {
                LastGeneralInterrogation = lastGeneralInterrogation,
                LastCounterInterrogation = lastCounterInterrogation,
                AutoGeneralEnabled = AutoGeneralInterrogation,
                AutoCounterEnabled = AutoCounterInterrogation,
                GeneralInterval = GeneralInterrogationInterval,
                CounterInterval = CounterInterrogationInterval,
                NextGeneralInterrogation = AutoGeneralInterrogation ?
                    lastGeneralInterrogation.AddMilliseconds(GeneralInterrogationInterval) :
                    DateTime.MinValue,
                NextCounterInterrogation = AutoCounterInterrogation ?
                    lastCounterInterrogation.AddMilliseconds(CounterInterrogationInterval) :
                    DateTime.MinValue
            };
        }

        /// <summary>
        /// Check if interrogation is due
        /// </summary>
        public bool IsGeneralInterrogationDue()
        {
            if (!AutoGeneralInterrogation || GeneralInterrogationInterval <= 0)
                return false;

            return DateTime.Now - lastGeneralInterrogation > TimeSpan.FromMilliseconds(GeneralInterrogationInterval);
        }

        /// <summary>
        /// Check if counter interrogation is due
        /// </summary>
        public bool IsCounterInterrogationDue()
        {
            if (!AutoCounterInterrogation || CounterInterrogationInterval <= 0)
                return false;

            return DateTime.Now - lastCounterInterrogation > TimeSpan.FromMilliseconds(CounterInterrogationInterval);
        }

        #endregion

        #region EVENT RAISING

        private void OnInterrogationStarted(InterrogationType type)
        {
            InterrogationStarted?.Invoke(this, new InterrogationEventArgs
            {
                Type = type,
                Status = "Started",
                Timestamp = DateTime.Now
            });
        }

        private void OnInterrogationCompleted(InterrogationType type)
        {
            InterrogationCompleted?.Invoke(this, new InterrogationEventArgs
            {
                Type = type,
                Status = "Completed",
                Timestamp = DateTime.Now
            });
        }

        private void OnInterrogationFailed(InterrogationType type, string error)
        {
            InterrogationFailed?.Invoke(this, new InterrogationEventArgs
            {
                Type = type,
                Status = "Failed",
                Error = error,
                Timestamp = DateTime.Now
            });
        }

        #endregion

        #region DISPOSE

        public void Dispose()
        {
            if (disposed) return;

            disposed = true;
            StopTimers();
            deviceAdapter = null;
        }

        #endregion
    }
}