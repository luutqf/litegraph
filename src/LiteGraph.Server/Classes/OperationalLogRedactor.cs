namespace LiteGraph.Server.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;

    /// <summary>
    /// Redacts sensitive values before they are written to operational logs or trace tags.
    /// </summary>
    public static class OperationalLogRedactor
    {
        #region Public-Members

        /// <summary>
        /// Redacted value marker.
        /// </summary>
        public const string RedactedValue = "***";

        #endregion

        #region Private-Members

        private static readonly HashSet<string> _SensitiveNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "authorization",
            "bearer",
            "bearertoken",
            "token",
            "securitytoken",
            "password",
            "connectionstring",
            "apikey",
            "api_key",
            "x-api-key",
            "x-password",
            "x-token",
            "cookie",
            "set-cookie",
            "embeddings",
            "vectors"
        };

        #endregion

        #region Public-Methods

        /// <summary>
        /// Redact a sensitive scalar value.
        /// </summary>
        /// <param name="value">Value.</param>
        /// <returns>Redacted value.</returns>
        public static string RedactValue(string value)
        {
            if (String.IsNullOrEmpty(value)) return value;
            return RedactedValue;
        }

        /// <summary>
        /// Return true if a key or property name should be redacted.
        /// </summary>
        /// <param name="name">Header, query-string, route, or property name.</param>
        /// <returns>True if the name is sensitive.</returns>
        public static bool IsSensitiveName(string name)
        {
            if (String.IsNullOrWhiteSpace(name)) return false;
            string normalized = name.Trim();
            if (_SensitiveNames.Contains(normalized)) return true;
            return normalized.EndsWith("token", StringComparison.OrdinalIgnoreCase)
                || normalized.EndsWith("password", StringComparison.OrdinalIgnoreCase)
                || normalized.EndsWith("connectionstring", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Redact sensitive headers.
        /// </summary>
        /// <param name="headers">Headers.</param>
        /// <returns>Redacted headers.</returns>
        public static Dictionary<string, string> RedactHeaders(NameValueCollection headers)
        {
            Dictionary<string, string> ret = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (headers == null) return ret;

            foreach (string key in headers.AllKeys)
            {
                if (String.IsNullOrEmpty(key)) continue;
                ret[key] = IsSensitiveName(key) ? RedactedValue : headers[key];
            }

            return ret;
        }

        /// <summary>
        /// Redact sensitive path and query-string values from a URL.
        /// </summary>
        /// <param name="url">URL.</param>
        /// <returns>Sanitized URL.</returns>
        public static string RedactUrl(string url)
        {
            if (String.IsNullOrEmpty(url)) return url;

            string fragment = String.Empty;
            int fragmentIndex = url.IndexOf('#');
            if (fragmentIndex >= 0)
            {
                fragment = url.Substring(fragmentIndex);
                url = url.Substring(0, fragmentIndex);
            }

            string query = null;
            int queryIndex = url.IndexOf('?');
            if (queryIndex >= 0)
            {
                query = url.Substring(queryIndex + 1);
                url = url.Substring(0, queryIndex);
            }

            string path = RedactSensitivePathSegments(url);
            if (!String.IsNullOrEmpty(query)) path += "?" + RedactQueryString(query);
            return path + fragment;
        }

        #endregion

        #region Private-Methods

        private static string RedactSensitivePathSegments(string path)
        {
            if (String.IsNullOrEmpty(path)) return path;

            string[] segments = path.Split('/');
            for (int i = 0; i < segments.Length; i++)
            {
                if (i > 0
                    && String.Equals(segments[i - 1], "bearer", StringComparison.OrdinalIgnoreCase)
                    && i > 1
                    && String.Equals(segments[i - 2], "credentials", StringComparison.OrdinalIgnoreCase))
                {
                    segments[i] = RedactedValue;
                }
                else if (i > 0 && IsSensitiveName(segments[i - 1]))
                {
                    segments[i] = RedactedValue;
                }
            }

            return String.Join("/", segments);
        }

        private static string RedactQueryString(string query)
        {
            if (String.IsNullOrEmpty(query)) return query;

            string[] parts = query.Split('&');
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                if (String.IsNullOrEmpty(part)) continue;

                int equalsIndex = part.IndexOf('=');
                string key = equalsIndex >= 0 ? part.Substring(0, equalsIndex) : part;
                if (!IsSensitiveName(Uri.UnescapeDataString(key))) continue;

                parts[i] = equalsIndex >= 0
                    ? part.Substring(0, equalsIndex + 1) + RedactedValue
                    : key + "=" + RedactedValue;
            }

            return String.Join("&", parts.Where(part => part != null));
        }

        #endregion
    }
}
