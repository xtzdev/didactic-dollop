﻿using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Data.Json;
using Windows.Devices.Gpio;
using HA4IoT.Contracts.Core;
using HA4IoT.Core.Timer;
using HA4IoT.Networking;

namespace HA4IoT.Core
{
    public class HealthMonitor
    {
        private readonly List<int> _durations = new List<int>(100);
        private readonly Timeout _ledTimeout = new Timeout();
        private readonly DateTime _startedDate = DateTime.Now;
        private readonly GpioPin _statusLed;
        private float? _averageTimerDuration;

        private bool _ledState;
        private float? _maxTimerDuration;

        private float? _minTimerDuration;

        public HealthMonitor(GpioPin statusLed, IHomeAutomationTimer timer, IHttpRequestController httpApiController)
        {
            if (timer == null) throw new ArgumentNullException(nameof(timer));
            if (httpApiController == null) throw new ArgumentNullException(nameof(httpApiController));

            _statusLed = statusLed;
            if (statusLed != null)
            {
                _ledTimeout.Start(TimeSpan.FromMilliseconds(1));
            }

            timer.Tick += Tick;
            httpApiController.Handle(HttpMethod.Get, "health").Using(HandleApiGet);
            httpApiController.Handle(HttpMethod.Post, "health").WithSegment("reset").Using(c => ResetStatistics());
        }

        private void HandleApiGet(HttpContext httpContext)
        {
            var status = new JsonObject();
            status.SetNamedValue("timerMin", _minTimerDuration.ToJsonValue());
            status.SetNamedValue("timerMax", _maxTimerDuration.ToJsonValue());
            status.SetNamedValue("timerAverage", _averageTimerDuration.ToJsonValue());
            status.SetNamedValue("upTime", (DateTime.Now - _startedDate).ToJsonValue());
            status.SetNamedValue("systemTime", DateTime.Now.ToJsonValue());

            httpContext.Response.Body = new JsonBody(status);
        }

        private void ResetStatistics()
        {
            _minTimerDuration = null;
            _maxTimerDuration = null;
            _averageTimerDuration = null;
        }

        private void Tick(object sender, TimerTickEventArgs e)
        {
            if (_ledTimeout.IsRunning)
            {
                _ledTimeout.Tick(e.ElapsedTime);
                if (_ledTimeout.IsElapsed)
                {
                    ToggleStatusLed();
                }
            }

            _durations.Add((int)e.ElapsedTime.TotalMilliseconds);
            if (_durations.Count == _durations.Capacity)
            {
                _averageTimerDuration = _durations.Sum() / (float)_durations.Count;
                _durations.Clear();

                if (!_maxTimerDuration.HasValue || _averageTimerDuration > _maxTimerDuration.Value)
                {
                    _maxTimerDuration = _averageTimerDuration;
                }

                if (!_minTimerDuration.HasValue || _averageTimerDuration < _minTimerDuration.Value)
                {
                    _minTimerDuration = _averageTimerDuration;
                }
            }
        }

        private void ToggleStatusLed()
        {
            if (_ledState)
            {
                _statusLed.Write(GpioPinValue.High);
                _ledTimeout.Start(TimeSpan.FromSeconds(5));
            }
            else
            {
                _statusLed.Write(GpioPinValue.Low);
                _ledTimeout.Start(TimeSpan.FromMilliseconds(200));
            }

            _ledState = !_ledState;
        }
    }
}
