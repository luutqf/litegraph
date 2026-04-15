namespace LiteGraph
{
    using System.Collections.Generic;

    /// <summary>
    /// Request history detail, including headers and bodies.
    /// </summary>
    public class RequestHistoryDetail : RequestHistoryEntry
    {
        #region Public-Members

        /// <summary>
        /// Request headers.
        /// </summary>
        public Dictionary<string, string> RequestHeaders { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Response headers.
        /// </summary>
        public Dictionary<string, string> ResponseHeaders { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Request body (decoded).
        /// </summary>
        public string RequestBody { get; set; } = null;

        /// <summary>
        /// Response body (decoded).
        /// </summary>
        public string ResponseBody { get; set; } = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public RequestHistoryDetail()
        {
        }

        #endregion
    }
}
