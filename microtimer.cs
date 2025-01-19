using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace MicroTimerImproved
{
    public class MicroTimer : Component 
    {
        private Thread _thread;
        private long _nextWakeUpTickTime;
        private long _periodInTicks;
        private bool _isRunning;

        //public delegate void MicroTimerEventHandler(int senderThreadId, long delayInTicks);      
        public event EventHandler<MicroTimerEventArgs> OnMicroTimer;
       // public event MicroTimerEventHandler OnMicroTimer;
       // public event MicroTimerEventHandler OnMicroTimerSkipped;

        public delegate void QuickNoteEventHandler(int senderThreadId);
        public event QuickNoteEventHandler OnMicroTimerStart;
        public event QuickNoteEventHandler OnMicroTimerStop;

        [Category("Behavior")]
        [Description("Interval in microseconds between timer events.")]
        [DefaultValue(100)] // Design-time varsayılan değer
        public int Interval { get; set; } = 100; // Runtime varsayılan değer

        [Category("Behavior")]
        [Description("Indicates whether the timer is running.")]
        [DefaultValue(false)] // Design-time varsayılan değer
        public bool Enabled
        {
            get => _isRunning;
            set
            {
                if (value != _isRunning)
                {
                    if (value)
                        Start();
                    else
                        Stop();
                }
            }
        }


        public MicroTimer()
        {
            
            Enabled = false;

        }

        private void Initialize(int userMicroseconds)
        {

            /*
             *           Usperiod to tick count (userMicroseconds)                                Frequency
             *      ------------------------------------------------      >>>>>>>>>>>>         ----------------  X   Usperiod to tick count (userMicroseconds)    =  userperiod to tickcnt (_periodInTicks)
             *                           1                                                        1_000_000           
             *                      -----------  X   1_000_000 
             *                       Frequency
             */

            long ticksPerMicrosecond = Stopwatch.Frequency / 1_000_000;
            _periodInTicks = ticksPerMicrosecond * userMicroseconds;

        }



        /// <summary>
        /// Starts the timer.
        /// </summary>
        public int Start()
        {
            if (_thread != null && _thread.IsAlive)
            {
                return -1; // Timer is already running
            }

            Initialize(Interval);

            _isRunning = true;
            _thread = new Thread(Loop)
            {
                Priority = ThreadPriority.Highest,
                IsBackground = true
            };
            _thread.Start();

            OnMicroTimerStart?.Invoke(Thread.CurrentThread.ManagedThreadId);
            return _thread.ManagedThreadId;
        }

        /// <summary>
        /// Stops the timer.
        /// </summary>
        public void Stop()
        {
            if (_thread != null && _thread.IsAlive)
            {
                _isRunning = false;
                _thread.Join(); // Wait for the thread to exit
                OnMicroTimerStop?.Invoke(Thread.CurrentThread.ManagedThreadId);
            }
        }

        /// <summary>
        /// Timer loop that handles events and timing.
        /// </summary>
        private void Loop()
        {
            try
            {
                _nextWakeUpTickTime = Stopwatch.GetTimestamp() + _periodInTicks;

                while (_isRunning)
                {
                    long remainingTicks = _nextWakeUpTickTime - Stopwatch.GetTimestamp();
                                  
                    
                        while (remainingTicks > 0 && Stopwatch.GetTimestamp() < _nextWakeUpTickTime)
                        {
                            Thread.Yield(); // Release CPU for other threads
                        }
                    

                    long wakeUpTimeInTicks = Stopwatch.GetTimestamp();
                    long delayInTicks = wakeUpTimeInTicks - _nextWakeUpTickTime;

                    // OnMicroTimer?.Invoke(Thread.CurrentThread.ManagedThreadId, delayInTicks);
                    OnMicroTimer?.Invoke(this, new MicroTimerEventArgs(Thread.CurrentThread.ManagedThreadId, delayInTicks));

                    // Schedule the next event
                    _nextWakeUpTickTime += _periodInTicks;
                }
            }
            catch (ThreadInterruptedException)
            {
                // Thread interrupted gracefully
            }
        }

    }

    public class MicroTimerEventArgs : EventArgs
    {
        public int SenderThreadId { get; }
        public long DelayInTicks { get; }

        public MicroTimerEventArgs(int senderThreadId, long delayInTicks)
        {
            SenderThreadId = senderThreadId;
            DelayInTicks = delayInTicks;
        }
    }


}
