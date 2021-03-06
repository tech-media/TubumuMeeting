﻿using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace TubumuMeeting.Mediasoup
{
    public class AudioLevelObserver : RtpObserver
    {
        /// <summary>
        /// Logger.
        /// </summary>
        private readonly ILogger<AudioLevelObserver> _logger;

        /// <summary>
        /// <para>Events:</para>
        /// <para>@emits volumes - (volumes: AudioLevelObserverVolume[])</para>
        /// <para>@emits silence</para>
        /// <para>Observer events:</para>
        /// <para>@emits close</para>
        /// <para>@emits pause</para>
        /// <para>@emits resume</para>
        /// <para>@emits addproducer - (producer: Producer)</para>
        /// <para>@emits removeproducer - (producer: Producer)</para>
        /// <para>@emits volumes - (volumes: AudioLevelObserverVolume[])</para>
        /// <para>@emits silence</para>
        /// </summary>
        /// <param name="loggerFactory"></param>
        /// <param name="rtpObserverInternalData"></param>
        /// <param name="channel"></param>
        /// <param name="payloadChannel"></param>
        /// <param name="appData"></param>
        /// <param name="getProducerById"></param>
        public AudioLevelObserver(ILoggerFactory loggerFactory,
            RtpObserverInternalData rtpObserverInternalData,
            Channel channel,
            PayloadChannel payloadChannel,
            Dictionary<string, object>? appData,
            Func<string, Producer?> getProducerById
            ) : base(loggerFactory, rtpObserverInternalData, channel, payloadChannel, appData, getProducerById)
        {
            _logger = loggerFactory.CreateLogger<AudioLevelObserver>();
        }

        protected override void OnChannelMessage(string targetId, string @event, string data)
        {
            if (targetId != Internal.RtpObserverId)
            {
                return;
            }

            switch (@event)
            {
                case "volumes":
                    {
                        var notification = JsonConvert.DeserializeObject<AudioLevelObserverVolumeNotificationData[]>(data);

                        List<AudioLevelObserverVolume> volumes = new List<AudioLevelObserverVolume>();
                        foreach (var item in notification)
                        {
                            var producer = GetProducerById(item.ProducerId);
                            if (producer != null)
                            {
                                volumes.Add(new AudioLevelObserverVolume
                                {
                                    Producer = producer,
                                    Volume = item.Volume,
                                });
                            }
                        }

                        if (volumes.Count > 0)
                        {
                            Emit("volumes", volumes);

                            // Emit observer event.
                            Observer.Emit("volumes", volumes);
                        }

                        break;
                    }
                case "silence":
                    {
                        Emit("silence");

                        // Emit observer event.
                        Observer.Emit("silence");

                        break;
                    }
                default:
                    {
                        _logger.LogError($"OnChannelMessage() | ignoring unknown event{@event}");
                        break;
                    }
            }
        }
    }
}
