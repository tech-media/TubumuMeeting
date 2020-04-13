﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TubumuMeeting.Mediasoup.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Tubumu.Core.Extensions;

namespace TubumuMeeting.Mediasoup
{
    public class Consumer : EventEmitter
    {
        // Logger
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<Consumer> _logger;

        #region Internal data.

        public string RouterId { get; }

        public string TransportId { get; }

        /// <summary>
        /// Consumer id.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Associated Producer id.
        /// </summary>
        public string ProducerId { get; }

        private object _internal;

        #endregion

        #region Producer data.

        /// <summary>
        /// Media kind.
        /// </summary>
        public MediaKind Kind { get; }

        /// <summary>
        /// RTP parameters.
        /// </summary>
        public RtpParameters RtpParameters { get; }

        /// <summary>
        /// Consumer type.
        /// </summary>
        public ConsumerType Type { get; }

        #endregion

        /// <summary>
        /// Channel instance.
        /// </summary>
        public Channel Channel { get; private set; }

        /// <summary>
        /// App custom data.
        /// </summary>
        public object? AppData { get; private set; }

        /// <summary>
        /// Whether the Consumer is closed.
        /// </summary>
        public bool Closed { get; private set; }

        /// <summary>
        /// Paused flag.
        /// </summary>
        public bool Paused { get; private set; }

        /// <summary>
        /// Whether the associate Producer is paused.
        /// </summary>
        public bool ProducerPaused { get; private set; }

        /// <summary>
        /// Current priority.
        /// </summary>
        public int Priority { get; private set; } = 1;

        /// <summary>
        /// Current score.
        /// </summary>
        public ConsumerScore? Score;

        // Preferred layers.
        public ConsumerLayers? PreferredLayers { get; private set; }

        // Curent layers.
        public ConsumerLayers? CurrentLayers { get; private set; }

        public EventEmitter Observer { get; } = new EventEmitter();

        /// <summary>
        /// @emits transportclose
        /// @emits producerclose
        /// @emits producerpause
        /// @emits producerresume
        /// @emits score - (score: ConsumerScore)
        /// @emits layerschange - (layers: ConsumerLayers | null)
        /// @emits trace - (trace: ConsumerTraceEventData)
        /// @emits @close
        /// @emits @producerclose
        /// Observer:
        /// @emits close
        /// @emits pause
        /// @emits resume
        /// @emits score - (score: ConsumerScore)
        /// @emits layerschange - (layers: ConsumerLayers | null)
        /// @emits trace - (trace: ConsumerTraceEventData)
        /// </summary>
        /// <param name="loggerFactory"></param>
        /// <param name="routerId"></param>
        /// <param name="transportId"></param>
        /// <param name="consumerId"></param>
        /// <param name="producerId"></param>
        /// <param name="kind"></param>
        /// <param name="rtpParameters"></param>
        /// <param name="type"></param>
        /// <param name="channel"></param>
        /// <param name="appData"></param>
        /// <param name="paused"></param>
        /// <param name="producerPaused"></param>
        /// <param name="score"></param>
        /// <param name="preferredLayers"></param>
        public Consumer(ILoggerFactory loggerFactory,
                    string routerId,
                    string transportId,
                    string consumerId,
                    string producerId,
                    MediaKind kind,
                    RtpParameters rtpParameters,
                    ConsumerType type,
                    Channel channel,
                    object? appData,
                    bool paused,
                    bool producerPaused,
                    ConsumerScore? score,
                    ConsumerLayers? preferredLayers
                    )
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<Consumer>();
            RouterId = routerId;
            TransportId = transportId;
            Id = consumerId;
            ProducerId = producerId;
            _internal = new
            {
                RouterId,
                TransportId,
                ConsumerId = Id,
                ProducerId
            };
            Kind = kind;
            RtpParameters = rtpParameters;
            Type = type;
            Channel = channel;
            AppData = appData;
            Paused = paused;
            ProducerPaused = producerPaused;
            Score = score;
            PreferredLayers = preferredLayers;

            HandleWorkerNotifications();
        }

        /// <summary>
        /// Close the Producer.
        /// </summary>
        public void Close()
        {
            if (Closed)
                return;

            _logger.LogDebug("Close()");

            Closed = true;

            // Fire and forget
            Channel.RequestAsync(MethodId.CONSUMER_CLOSE.GetEnumStringValue(), _internal).ContinueWithOnFaultedHandleLog(_logger);

            Emit("@close");

            // Emit observer event.
            Observer.Emit("close");
        }

        /// <summary>
        /// Transport was closed.
        /// </summary>
        public void TransportClosed()
        {
            if (Closed)
                return;

            _logger.LogDebug("TransportClosed()");

            Closed = true;

            Emit("transportclose");

            // Emit observer event.
            Observer.Emit("close");
        }

        /// <summary>
        /// Dump DataProducer.
        /// </summary>
        public Task<string?> DumpAsync()
        {
            _logger.LogDebug("DumpAsync()");
            return Channel.RequestAsync(MethodId.CONSUMER_DUMP.GetEnumStringValue(), _internal);
        }

        /// <summary>
        /// Get DataProducer stats.
        /// </summary>
        public Task<string?> GetStatsAsync()
        {
            _logger.LogDebug("GetStatsAsync()");
            return Channel.RequestAsync(MethodId.CONSUMER_GET_STATS.GetEnumStringValue(), _internal);
        }

        /// <summary>
        /// Pause the Consumer.
        /// </summary>
        public async Task PauseAsync()
        {
            _logger.LogDebug("PauseAsync()");

            var wasPaused = Paused;

            await Channel.RequestAsync(MethodId.CONSUMER_PAUSE.GetEnumStringValue(), _internal);

            Paused = true;

            // Emit observer event.
            if (!wasPaused)
                Observer.Emit("pause");
        }

        /// <summary>
        /// Resume the Consumer.
        /// </summary>
        public async Task ResumeAsync()
        {
            _logger.LogDebug("ResumeAsync()");

            var wasPaused = Paused;

            await Channel.RequestAsync(MethodId.CONSUMER_RESUME.GetEnumStringValue(), _internal);

            Paused = false;

            // Emit observer event.
            if (wasPaused)
                Observer.Emit("resume");
        }

        /// <summary>
        /// Set preferred video layers.
        /// </summary>
        public async Task SetPreferredLayersAsync(ConsumerLayers consumerLayers)
        {
            _logger.LogDebug("SetPreferredLayersAsync()");

            var reqData = consumerLayers;
            var data = await Channel.RequestAsync(MethodId.CONSUMER_SET_PREFERRED_LAYERS.GetEnumStringValue(), this._internal, reqData);
            //PreferredLayers = data || null;
        }

        /// <summary>
        /// Set priority.
        /// </summary>
        public async Task SetPriorityAsync(int priority)
        {
            _logger.LogDebug("SetPriorityAsync()");

            var reqData = new { Priority = priority };
            var data = await Channel.RequestAsync(MethodId.CONSUMER_SET_PRIORITY.GetEnumStringValue(), this._internal, reqData);
            //Priority = data.priority;

        }

        /// <summary>
        /// Unset priority.
        /// </summary>
        public async Task UnsetPriorityAsync()
        {
            _logger.LogDebug("UnsetPriorityAsync()");

            var reqData = new { Priority = 1 };
            var data = await Channel.RequestAsync(MethodId.CONSUMER_SET_PRIORITY.GetEnumStringValue(), this._internal, reqData);
            //Priority = data.priority;

        }

        /// <summary>
        /// Request a key frame to the Producer.
        /// </summary>
        public Task RequestKeyFrameAsync()
        {
            _logger.LogDebug("RequestKeyFrameAsync()");
            return Channel.RequestAsync(MethodId.CONSUMER_REQUEST_KEY_FRAME.GetEnumStringValue(), this._internal);
        }

        /// <summary>
        /// Enable 'trace' event.
        /// </summary>
        public Task EnableTraceEventAsync(TraceEventType[] types)
        {
            _logger.LogDebug("EnableTraceEventAsync()");
            var reqData = new
            {
                Types = types ?? new TraceEventType[0]
            };
            return Channel.RequestAsync(MethodId.CONSUMER_ENABLE_TRACE_EVENT.GetEnumStringValue(), this._internal, reqData);
        }

        #region Event Handlers

        private void HandleWorkerNotifications()
        {
            Channel.MessageEvent += OnChannelMessage;
        }

        private void OnChannelMessage(string targetId, string @event, string data)
        {
            if (targetId != Id) return;
            switch (@event)
            {
                case "producerclose":
                    {
                        if (Closed)
                            break;

                        Closed = true;

                        Emit("@producerclose");
                        Emit("producerclose");

                        // Emit observer event.
                        Observer.Emit("close");

                        break;
                    }
                case "producerpause":
                    {
                        if (ProducerPaused)
                            break;

                        var wasPaused = Paused || ProducerPaused;

                        ProducerPaused = true;

                        Emit("producerpause");

                        // Emit observer event.
                        if (!wasPaused)
                            Observer.Emit("pause");

                        break;
                    }
                case "producerresume":
                    {
                        if (!ProducerPaused)
                            break;

                        var wasPaused = Paused || ProducerPaused;

                        ProducerPaused = false;

                        Emit("producerresume");

                        // Emit observer event.
                        if (wasPaused && !Paused)
                            Observer.Emit("resume");

                        break;
                    }
                case "score":
                    {
                        var score = JsonConvert.DeserializeObject<ConsumerScore>(data);
                        Score = score;

                        Emit("score", score);

                        // Emit observer event.
                        Observer.Emit("score", score);

                        break;
                    }
                case "layerschange":
                    {
                        var layers = !data.IsNullOrWhiteSpace() ? JsonConvert.DeserializeObject<ConsumerLayers>(data) : null;

                        CurrentLayers = layers;

                        Emit("layerschange", layers);

                        // Emit observer event.
                        Observer.Emit("layersChange", layers);

                        break;
                    }
                case "trace":
                    {
                        var trace = JsonConvert.DeserializeObject<TraceEventData>(data);

                        Emit("trace", trace);

                        // Emit observer event.
                        Observer.Emit("trace", trace);

                        break;
                    }
                default:
                    {
                        _logger.LogError($"ignoring unknown event{@event}");
                        break;
                    }
            }
        }

        #endregion
    }
}
