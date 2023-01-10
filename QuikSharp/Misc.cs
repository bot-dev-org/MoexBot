﻿// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using System.Net.Sockets;

namespace QuikSharp
{
    public static class NetworkUtils
    {
        /// <summary>
        /// The connection state of a socket is reflected in the Connected property,
        ///  but this property only gets updated with the last send- or receive-action.
        /// To determine the connection state before send or receive the one an only way
        /// is polling the state directly from the socket it self. The following
        ///  extension class will help doing this.
        /// </summary>
        public static bool IsConnectedNow(this Socket s)
        {
            var part1 = s.Poll(1000, SelectMode.SelectRead);
            var part2 = s.Available == 0;
            if (part1 && part2 || !s.Connected) return false;
            return true;
        }
    }
}