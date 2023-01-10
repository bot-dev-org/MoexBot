﻿// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

namespace QuikSharp
{
    internal static class ServiceManager
    {
        private static QuikService quikService;

        public static void StartServices()
        {
            quikService = QuikService.Create(Quik.DefaultPort);
        }

        public static void StopServices()
        {
        }

        public static void RestartServices()
        {
            StopServices();
            StartServices();
        }
    }
}