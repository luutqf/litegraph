namespace LiteGraph.Storage
{
    /// <summary>
    /// Entity counts for a repository.
    /// </summary>
    public class StorageEntityCounts
    {
        /// <summary>
        /// Tenants.
        /// </summary>
        public int Tenants { get; set; } = 0;

        /// <summary>
        /// Users.
        /// </summary>
        public int Users { get; set; } = 0;

        /// <summary>
        /// Credentials.
        /// </summary>
        public int Credentials { get; set; } = 0;

        /// <summary>
        /// Graphs.
        /// </summary>
        public int Graphs { get; set; } = 0;

        /// <summary>
        /// Nodes.
        /// </summary>
        public int Nodes { get; set; } = 0;

        /// <summary>
        /// Edges.
        /// </summary>
        public int Edges { get; set; } = 0;

        /// <summary>
        /// Labels.
        /// </summary>
        public int Labels { get; set; } = 0;

        /// <summary>
        /// Tags.
        /// </summary>
        public int Tags { get; set; } = 0;

        /// <summary>
        /// Vectors.
        /// </summary>
        public int Vectors { get; set; } = 0;

        /// <summary>
        /// Authorization roles.
        /// </summary>
        public int AuthorizationRoles { get; set; } = 0;

        /// <summary>
        /// User role assignments.
        /// </summary>
        public int UserRoleAssignments { get; set; } = 0;

        /// <summary>
        /// Credential scope assignments.
        /// </summary>
        public int CredentialScopeAssignments { get; set; } = 0;

        /// <summary>
        /// Total graph child objects.
        /// </summary>
        public int GraphChildObjects
        {
            get
            {
                return Nodes + Edges + Labels + Tags + Vectors;
            }
        }
    }
}
