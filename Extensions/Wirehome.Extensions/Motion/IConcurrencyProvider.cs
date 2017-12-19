﻿using System.Reactive.Concurrency;

namespace Wirehome.Extensions
{
    public interface IConcurrencyProvider
    {
        IScheduler Scheduler { get; }
        IScheduler Task { get; }
        IScheduler Thread { get; }
    }
}
