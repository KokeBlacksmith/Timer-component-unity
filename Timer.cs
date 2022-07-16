using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KokeBlacksmith.Core
{
    public class Timer : MonoBehaviour
    {
        private class TimerHandle : ITimerHandle
        {
            private Action _callback;

            public TimerHandle(float time, Action onComplete)
            {
                Time = time;
                ElapsedTime = 0.0f;
                IsCompleted = false;
                _callback = onComplete;
                IsActive = true;
            }

            public float Time { get; private set; }
            public float ElapsedTime { get; private set; }
            public float RemainingTime { get; private set; }
            public bool IsCompleted { get; private set; }
            public bool IsActive { get; private set; }
            public float CompletedPercentage { get; private set; }

            public bool Update()
            {
                if (!IsActive || IsCompleted)
                {
                    Cancel();
                    return true;
                }

                ElapsedTime += UnityEngine.Time.deltaTime;
                RemainingTime = Mathf.Clamp(Time - ElapsedTime, 0.0f, Time);

                if (ElapsedTime >= Time)
                {
                    IsCompleted = true;
                    CompletedPercentage = 1.0f;
                    _callback?.Invoke();
                    Cancel();

                    return true;
                }

                CompletedPercentage = ElapsedTime / Time;
                return false;
            }

            public void Cancel()
            {
                IsActive = false;
                _callback = null;
            }
        }

        private Dictionary<TimerHandle, Coroutine> _timers = null;

        private void Awake()
        {
            _timers = new Dictionary<TimerHandle, Coroutine>();
        }

        /// <summary>
        /// Starts a timer and returns its handle.
        /// </summary>
        /// <param name="seconds">Seconds of the timer</param>
        /// <param name="callback">Method to be fired when the timer is succesfully completed</param>
        /// <returns>Handle of the timer</returns>
        public ITimerHandle StartTimer(float seconds, Action callback)
        {
            TimerHandle handle = new TimerHandle(Mathf.Abs(seconds), callback);
            _timers.Add(handle, StartCoroutine(nameof(_TimerCoroutine), handle));
            return handle;
        }


        /// <summary>
        /// Starts a timer with random duration and returns its handle.
        /// </summary>
        /// <param name="minSeconds"></param>
        /// <param name="maxSeconds"></param>
        /// <param name="callback">Method to be fired when the timer is succesfully completed</param>
        /// <returns>Handle of the timer</returns>
        public ITimerHandle StartRandomTimer(float minSeconds, float maxSeconds, Action callback)
        {
            if (minSeconds >= maxSeconds)
                throw new UnityException($"{nameof(minSeconds)} of the random timer can't be greater or equals to {nameof(maxSeconds)}");

            if (minSeconds <= 0 || maxSeconds <= 0)
                throw new UnityException($"{nameof(minSeconds)} and {nameof(maxSeconds)} of the random timer have to be greater than 0");

            UnityEngine.Random.Range(minSeconds, maxSeconds);
            return StartTimer(UnityEngine.Random.Range(minSeconds, maxSeconds), callback);
        }

        /// <summary>
        /// Stops the timer of the given handle
        /// </summary>
        /// <param name="handle"></param>
        /// <returns>True if the timer of the handle was successfully stoped</returns>
        public bool StopTimer(ITimerHandle handle)
        {
            if (handle is TimerHandle h && _timers.TryGetValue(h, out Coroutine coroutine))
            {
                h.Cancel();

                if (coroutine != null)
                    StopCoroutine(coroutine);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Cancels all the timers
        /// </summary>
        public void StopAllTimers()
        {
            base.StopAllCoroutines();
            foreach (TimerHandle handle in _timers.Keys)
                handle.Cancel();

            _timers.Clear();
        }

        private IEnumerator _TimerCoroutine(TimerHandle handle)
        {
            while (handle.Update() == false)
            {
                yield return new WaitForEndOfFrame();
            }

            _timers.Remove(handle);
        }

        [Obsolete("Timer does not allow coroutine modification.")]
        public new void StopAllCoroutines()
        {
            throw new UnityException($"{nameof(Timer)} component does not allow coroutine modification.");
        }
    }

    public interface ITimerHandle
    {
        /// <summary>
        /// Total seconds of the timer
        /// </summary>
        float Time { get; }
        /// <summary>
        /// Elapsed seconds
        /// </summary>
        float ElapsedTime { get; }

        /// <summary>
        /// Remaining seconds
        /// </summary>
        float RemainingTime { get; }

        /// <summary>
        /// Whether the timer is completed
        /// </summary>
        bool IsCompleted { get; }
        /// <summary>
        /// Value from 0.0f to 1.0f of completion
        /// </summary>
        float CompletedPercentage { get; }

        /// <summary>
        /// Wether the timer is active at this moment
        /// </summary>
        bool IsActive { get; }
    }
}
