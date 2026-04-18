namespace LiteGraph
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text.Json.Serialization;
    using PrettyId;

    /// <summary>
    /// Credentials.
    /// </summary>
    public class Credential
    {
        #region Public-Members

        /// <summary>
        /// GUID.
        /// </summary>
        public Guid GUID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Tenant GUID.
        /// </summary>
        public Guid TenantGUID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// User GUID.
        /// </summary>
        public Guid UserGUID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Name.
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// Access key.
        /// </summary>
        public string BearerToken { get; set; } = new IdGenerator().Generate(64);

        /// <summary>
        /// Active.
        /// </summary>
        public bool Active { get; set; } = true;

        /// <summary>
        /// Credential scopes.  Null or empty means full access for backward compatibility.
        /// Recommended values: read, write, admin.
        /// </summary>
        public List<string> Scopes { get; set; } = null;

        /// <summary>
        /// Graph GUID allow-list.  Null or empty means all graphs in the credential tenant.
        /// </summary>
        public List<Guid> GraphGUIDs { get; set; } = null;

        /// <summary>
        /// Creation timestamp, in UTC.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp from last update, in UTC.
        /// </summary>
        public DateTime LastUpdateUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public Credential()
        {

        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Check whether the credential has a scope.
        /// </summary>
        /// <param name="scope">Scope.</param>
        /// <returns>True if permitted.</returns>
        public bool HasScope(string scope)
        {
            if (String.IsNullOrEmpty(scope)) return true;
            if (Scopes == null || Scopes.Count < 1) return true;
            if (Scopes.Any(s => String.Equals(s, "*", StringComparison.OrdinalIgnoreCase))) return true;
            if (Scopes.Any(s => String.Equals(s, "admin", StringComparison.OrdinalIgnoreCase))) return true;
            return Scopes.Any(s => String.Equals(s, scope, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Check whether the credential can access a graph.
        /// </summary>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <returns>True if permitted.</returns>
        public bool CanAccessGraph(Guid? graphGuid)
        {
            if (graphGuid == null) return true;
            if (GraphGUIDs == null || GraphGUIDs.Count < 1) return true;
            return GraphGUIDs.Contains(graphGuid.Value);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
