﻿using System.Collections.Generic;

namespace TubumuMeeting.Mediasoup
{
    public class ConsumerOptions
    {
        /// <summary>
        /// The id of the Producer to consume.
        /// </summary>
        public string ProducerId { get; set; }

        /// <summary>
        /// RTP capabilities of the consuming endpoint.
        /// </summary>
        public RtpCapabilities? RtpCapabilities { get; set; }

        /// <summary>
        /// Whether the Consumer must start in paused mode. Default false.
        ///
        /// When creating a video Consumer, it's recommended to set paused to true,
        /// then transmit the Consumer parameters to the consuming endpoint and, once
        /// the consuming endpoint has created its local side Consumer, unpause the
        /// server side Consumer using the resume() method. This is an optimization
        /// to make it possible for the consuming endpoint to render the video as far
        /// as possible. If the server side Consumer was created with paused: false,
        /// mediasoup will immediately request a key frame to the remote Producer and
        /// suych a key frame may reach the consuming endpoint even before it's ready
        /// to consume it, generating “black” video until the device requests a keyframe
        /// by itself.
        /// </summary>
        public bool? Paused { get; set; } = false;

        /// <summary>
        /// Preferred spatial and temporal layer for simulcast or SVC media sources.
        /// If unset, the highest ones are selected.
        /// </summary>
        public ConsumerLayers? PreferredLayers { get; set; }

        /// <summary>
        /// Custom application data.
        /// </summary>
        public Dictionary<string, object>? AppData { get; set; }
    }
}
