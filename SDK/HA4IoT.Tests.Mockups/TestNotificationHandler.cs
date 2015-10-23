﻿using HA4IoT.Notifications;

namespace HA4IoT.Tests.Mockups
{
    public class TestNotificationHandler : INotificationHandler
    {
        public void Publish(NotificationType type, string message, params object[] parameters)
        {   
        }

        public void PublishFrom<TSender>(TSender sender, NotificationType type, string message, params object[] parameters) where TSender : class
        {
        }
    }
}