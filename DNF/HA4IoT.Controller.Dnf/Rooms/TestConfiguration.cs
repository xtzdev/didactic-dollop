﻿using System;
using HA4IoT.Actuators;
using HA4IoT.Automations;
using HA4IoT.Sensors;
using HA4IoT.Controller.Dnf.Enums;
using HA4IoT.Extensions.Extensions;
using HA4IoT.Contracts.Areas;
using HA4IoT.Extensions.Core;
using HA4IoT.Contracts.Components.States;
using HA4IoT.Contracts.Core;
using HA4IoT.Hardware.RemoteSockets;
using HA4IoT.Areas;
using HA4IoT.Contracts.Hardware.RemoteSockets.Protocols;
using HA4IoT.Hardware.Drivers.CCTools;
using HA4IoT.Contracts.Scheduling;
using HA4IoT.Hardware.Drivers.CCTools.Devices;
using HA4IoT.Hardware.Drivers.RemoteSockets;
using HA4IoT.Contracts.Hardware.RemoteSockets;

namespace HA4IoT.Controller.Dnf.Rooms
{
    internal partial class TestConfiguration
    {
        private readonly IDeviceRegistryService _deviceService;
        private readonly IAreaRegistryService _areaService;
        private readonly SensorFactory _sensorFactory;
        private readonly ActuatorFactory _actuatorFactory;
        private readonly AutomationFactory _automationFactory;
        private readonly IRemoteSocketService _remoteSocketService;

        private const int TIME_TO_ON = 30;
        private const int TIME_WHILE_ON = 5;
        private Speaker _speaker;


        public TestConfiguration(IDeviceRegistryService deviceService, 
                                    IAreaRegistryService areaService,
                                    SensorFactory sensorFactory,
                                    ActuatorFactory actuatorFactory,
                                    AutomationFactory automationFactory,
                                    IRemoteSocketService remoteSocketService
                                    ) 
        {
            _deviceService = deviceService ?? throw new ArgumentNullException(nameof(deviceService));
            _areaService = areaService ?? throw new ArgumentNullException(nameof(areaService));
            _actuatorFactory = actuatorFactory ?? throw new ArgumentNullException(nameof(actuatorFactory));
            _sensorFactory = sensorFactory ?? throw new ArgumentNullException(nameof(sensorFactory));
            _automationFactory = automationFactory ?? throw new ArgumentNullException(nameof(automationFactory));
            _remoteSocketService = remoteSocketService ?? throw new ArgumentNullException(nameof(remoteSocketService));

        }

        public void Apply()
        {
            
            var room = _areaService.RegisterArea(Room.Test);
            
           
            var codePair = DipswitchCodeProvider.GetCodePair(DipswitchSystemCode.AllOn, DipswitchUnitCode.D, 20);
            var socket = _actuatorFactory.RegisterSocket(room, BalconyElements.RemoteSocket, _remoteSocketService.RegisterRemoteSocket(BalconyElements.RemoteSocket.ToString(), "RemoteSocketBridge", codePair));

            var livingRoomAutomation = _automationFactory.RegisterTurnOnAndOffAutomation(room, LivingroomElements.SchedulerAutomation)
            .WithSchedulerTime(new AutomationScheduler
            {
                StartTime = new TimeSpan(18, 0, 0),
                TurnOnInterval = new TimeSpan(0, 0, 20),
                WorkingTime = new TimeSpan(0, 0, 5)
            })
            .WithTarget(socket);
        }

       
    } 
}