﻿using System;

namespace Wirehome.Contracts.Components.Exceptions
{
    public class ComponentNotFoundException : Exception
    {
        public ComponentNotFoundException(string id) : base($"Component with ID '{id}' not found.")
        {
            ComponentId = id;
        }
        
        public string ComponentId { get; }
    }
}
