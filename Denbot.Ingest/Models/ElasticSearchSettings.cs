using System;

namespace Denbot.Ingest.Models {
    public class ElasticSearchSettings {
        /// <summary>
        /// Cloud ID if using Elastic Cloud
        /// </summary>
        public string CloudId { get; set; }
        /// <summary>
        /// Cloud Auth in the form username:password for the Elastic Cloud - will override other settings
        /// </summary>
        public string CloudAuth { get; set; }
        /// <summary>
        /// Base64 API key
        /// </summary>
        public string ApiKey { get; set; }
        /// <summary>
        /// Gets the Username when using Cloud Auth
        /// </summary>
        /// <exception cref="FormatException"><see cref="CloudAuth"/> is malformed</exception>
        public string Username => GetAuthItem(CloudAuth, 0);
        /// <summary>
        /// Gets the Password when using Cloud Auth
        /// </summary>
        /// <exception cref="FormatException"><see cref="CloudAuth"/> is malformed</exception>
        public string Password => GetAuthItem(CloudAuth, 1);

        private static string GetAuthItem(string value, int index)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var splitCloudAuth = value.Split(":");
            if (splitCloudAuth.Length != 2)
            {
                throw new FormatException("Cloud Auth parameter is malformed");
            }

            return splitCloudAuth[index];
        }
    }
}