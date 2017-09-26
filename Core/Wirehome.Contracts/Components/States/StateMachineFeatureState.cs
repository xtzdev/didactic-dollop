﻿namespace Wirehome.Contracts.Components.States
{
    public class StateMachineFeatureState : IComponentFeatureState
    {
        public StateMachineFeatureState(string value)
        {
            Value = value;
        }

        public string Value { get; }
    }
}
