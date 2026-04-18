namespace LiteGraph.Server.Classes
{
    using System;

    /// <summary>
    /// Observability settings for metrics and traces.
    /// </summary>
    public class ObservabilitySettings
    {
        #region Public-Members

        /// <summary>
        /// Enable observability instrumentation.
        /// </summary>
        public bool Enable { get; set; } = true;

        /// <summary>
        /// Enable the Prometheus scrape endpoint.
        /// </summary>
        public bool EnablePrometheus { get; set; } = true;

        /// <summary>
        /// Enable OpenTelemetry-compatible Meter and ActivitySource hooks.
        /// </summary>
        public bool EnableOpenTelemetry { get; set; } = true;

        /// <summary>
        /// Enable built-in OTLP export for metrics and traces.
        /// </summary>
        public bool EnableOtlpExporter { get; set; } = false;

        /// <summary>
        /// OTLP endpoint URI. When null or empty, OpenTelemetry exporter defaults and environment variables are used.
        /// </summary>
        public string OtlpEndpoint { get; set; } = null;

        /// <summary>
        /// OTLP protocol: grpc or http/protobuf.
        /// </summary>
        public string OtlpProtocol
        {
            get
            {
                return _OtlpProtocol;
            }
            set
            {
                if (String.IsNullOrWhiteSpace(value)) value = "grpc";
                string normalized = value.Trim();
                if (!String.Equals(normalized, "grpc", StringComparison.OrdinalIgnoreCase)
                    && !String.Equals(normalized, "http/protobuf", StringComparison.OrdinalIgnoreCase)
                    && !String.Equals(normalized, "http-protobuf", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("OTLP protocol must be 'grpc' or 'http/protobuf'.", nameof(OtlpProtocol));
                }

                _OtlpProtocol = normalized;
            }
        }

        /// <summary>
        /// Optional OTLP headers in the exporter format key=value,key2=value2.
        /// </summary>
        public string OtlpHeaders { get; set; } = null;

        /// <summary>
        /// OTLP export timeout in milliseconds.
        /// </summary>
        public int OtlpTimeoutMilliseconds
        {
            get
            {
                return _OtlpTimeoutMilliseconds;
            }
            set
            {
                if (value < 1 || value > 600000) throw new ArgumentOutOfRangeException(nameof(OtlpTimeoutMilliseconds));
                _OtlpTimeoutMilliseconds = value;
            }
        }

        /// <summary>
        /// Prometheus metrics endpoint path.
        /// </summary>
        public string MetricsPath
        {
            get
            {
                return _MetricsPath;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(MetricsPath));
                _MetricsPath = value.StartsWith("/") ? value : "/" + value;
            }
        }

        /// <summary>
        /// Service name used by OpenTelemetry-compatible instruments.
        /// </summary>
        public string ServiceName
        {
            get
            {
                return _ServiceName;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(ServiceName));
                _ServiceName = value;
            }
        }

        #endregion

        #region Private-Members

        private string _MetricsPath = "/metrics";
        private string _ServiceName = "LiteGraph.Server";
        private string _OtlpProtocol = "grpc";
        private int _OtlpTimeoutMilliseconds = 10000;

        #endregion
    }
}
