﻿using Quartz;
using Wirehome.Core.Communication.I2C;
using Wirehome.Core.EventAggregator;
using Wirehome.Core.Services.Logging;

namespace Wirehome.ComponentModel.Adapters
{
    public interface IAdapterServiceFactory
    {
        IEventAggregator GetEventAggregator();
        II2CBusService GetI2CService();
        ILogService GetLogger();
        IScheduler GetScheduler();
    }
}