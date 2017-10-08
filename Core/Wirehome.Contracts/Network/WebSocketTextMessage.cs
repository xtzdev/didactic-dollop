﻿using System;
using Wirehome.Contracts.Network.Websockets;

namespace Wirehome.Contracts.Network.Websockets
{
    public class WebSocketTextMessage : WebSocketMessage
    {
        public WebSocketTextMessage(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));

            Text = text;
        }

        public string Text { get; }
    }
}