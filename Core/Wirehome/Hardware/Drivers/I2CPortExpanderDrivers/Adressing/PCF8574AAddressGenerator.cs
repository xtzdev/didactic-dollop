﻿using System;
using Wirehome.Contracts.Hardware.I2C;

namespace Wirehome.Hardware.Drivers.I2CPortExpanderDrivers.Adressing
{
    public static class PCF8574AAddressGenerator
    {
        public static I2CSlaveAddress Generate(AddressPinState a0, AddressPinState a1, AddressPinState a2)
        {
            if (a0 == AddressPinState.Low && a1 == AddressPinState.Low && a2 == AddressPinState.Low)
            {
                return new I2CSlaveAddress(0x38);
            }

            if (a0 == AddressPinState.High && a1 == AddressPinState.Low && a2 == AddressPinState.Low)
            {
                return new I2CSlaveAddress(0x39);
            }

            if (a0 == AddressPinState.Low && a1 == AddressPinState.High && a2 == AddressPinState.Low)
            {
                return new I2CSlaveAddress(0x3A);
            }

            if (a0 == AddressPinState.High && a1 == AddressPinState.High && a2 == AddressPinState.Low)
            {
                return new I2CSlaveAddress(0x3B);
            }

            if (a0 == AddressPinState.Low && a1 == AddressPinState.Low && a2 == AddressPinState.High)
            {
                return new I2CSlaveAddress(0x3C);
            }

            if (a0 == AddressPinState.High && a1 == AddressPinState.Low && a2 == AddressPinState.High)
            {
                return new I2CSlaveAddress(0x3D);
            }

            if (a0 == AddressPinState.Low && a1 == AddressPinState.High && a2 == AddressPinState.High)
            {
                return new I2CSlaveAddress(0x3E);
            }

            if (a0 == AddressPinState.High && a1 == AddressPinState.High && a2 == AddressPinState.High)
            {
                return new I2CSlaveAddress(0x3F);
            }

            throw new NotSupportedException();
        }
    }
}
