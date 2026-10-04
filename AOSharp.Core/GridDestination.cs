using System;
using System.Collections.Concurrent;
using System.Linq;
using AOSharp.Common.GameData;
using AOSharp.Common.Unmanaged.Imports;
using AOSharp.Core;
using AOSharp.Core.UI;
using SmokeLounge.AOtomation.Messaging.GameData;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

namespace AOSharp.Core
{
    public static class GridDestination
    {
        private static GridInteractionToken _token;
        private static GridDestinationInfo[] _gridDestinations;

        internal static void OnGridDestinationSelectMessage(GridDestinationSelectMessage gridDestSelectMsg)
        {
            _token = gridDestSelectMsg.Token;
            _gridDestinations = gridDestSelectMsg.GridDestinations;
        }

        public static void TeleportTo(string locationName)
        {
            if (_gridDestinations == null)
                return;

            var destination = _gridDestinations.FirstOrDefault(x => string.Equals(x.Location, locationName, StringComparison.OrdinalIgnoreCase));

            if (destination == null)
                return;

            Network.Send(new GridSelectedMessage
            {
                Destination = destination.DestinationInfo,
                Token = _token
            });
        }

        public static void TeleportTo(DestinationInfo destinationInfo)
        {
            if (_token == null)
                return;

            Network.Send(new GridSelectedMessage
            {
                Destination = destinationInfo,
                Token = _token
            });
        }
    }
}