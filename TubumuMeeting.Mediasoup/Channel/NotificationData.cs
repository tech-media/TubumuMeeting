﻿using Newtonsoft.Json;

namespace TubumuMeeting.Mediasoup
{
    public class AudioLevelObserverVolumeNotificationData
    {
        /// <summary>
        /// The audio producer id.
        /// </summary>
        public string ProducerId { get; set; }

        /// <summary>
        /// The average volume (in dBvo from -127 to 0) of the audio producer in the
        /// last interval.
        /// </summary>
        public int Volume { get; set; }
    }

    public class TransportIceStateChangeNotificationData
    {
        public IceState IceState { get; set; }
    }

    public class TransportIceSelectedTupleChangeNotificationData
    {
        public TransportTuple IceSelectedTuple { get; set; }
    }

    public class TransportDtlsStateChangeNotificationData
    {
        public DtlsState DtlsState { get; set; }

        public string? DtlsRemoteCert { get; set; }
    }

    public class TransportSctpStateChangeNotificationData
    {
        public SctpState SctpState { get; set; }
    }
}
