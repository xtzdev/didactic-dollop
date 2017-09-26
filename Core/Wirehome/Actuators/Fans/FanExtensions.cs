﻿using System;
using Wirehome.Contracts.Actuators;
using Wirehome.Contracts.Areas;

namespace Wirehome.Actuators.Fans
{
    public static class FanExtensions
    {
        public static IFan GetFan(this IArea area, Enum id)
        {
            if (area == null) throw new ArgumentNullException(nameof(area));

            return area.GetComponent<IFan>($"{area.Id}.{id}");
        }
    }
}
