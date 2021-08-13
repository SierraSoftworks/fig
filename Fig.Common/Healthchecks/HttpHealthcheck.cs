namespace Fig.Common.Healthchecks
{
    using System;
    using System.Net.Http;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    /// <summary>
    /// An HTTP healthcheck will submit an HTTP request to the provided endpoint and return
    /// a health indicator based on the response code and availability of the endpoint.
    /// </summary>
    public sealed class HttpHealthcheck : HealthcheckerBase, IDisposable
    {
        private readonly HttpClient httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpHealthcheck"/> class.
        /// </summary>
        /// <param name="httpClient">The <see cref="HttpClient"/> used to evaluate the health of endpoints.</param>
        public HttpHealthcheck(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        /// <inheritdoc/>
        public override string Kind => "http";

        /// <inheritdoc/>
        protected override async Task<HealthcheckResult> GetHealthInternalAsync(Healthcheck healthcheck)
        {
            var options = this.GetOptions<Options>(healthcheck);

            if (options?.Endpoint is null)
            {
                throw new ArgumentNullException(nameof(Options.Endpoint));
            }

            var response = await this.httpClient.GetAsync(options.Endpoint);
            var content = await response.Content.ReadAsStringAsync();
            return new HealthcheckResult { IsHealthy = response.IsSuccessStatusCode, Message = content };
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.httpClient.Dispose();
        }

        public record Options
        {
            /// <summary>
            /// Gets the endpoint used by the HTTP healthcheck.
            /// </summary>
            [JsonPropertyName("endpoint")]
            public string? Endpoint { get; init; }
        }
    }
}
