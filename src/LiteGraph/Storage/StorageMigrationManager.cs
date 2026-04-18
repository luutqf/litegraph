namespace LiteGraph.Storage
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.GraphRepositories;

    /// <summary>
    /// Provider-neutral storage migration and verification helpers.
    /// </summary>
    public static class StorageMigrationManager
    {
        /// <summary>
        /// Migrate data between two repositories created from provider settings.
        /// </summary>
        /// <param name="sourceSettings">Source database settings.</param>
        /// <param name="destinationSettings">Destination database settings.</param>
        /// <param name="verify">True to verify counts and sampled records after migration.</param>
        /// <param name="sampleSize">Number of records per entity type to sample during verification.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Migration result.</returns>
        public static async Task<StorageMigrationResult> MigrateAsync(
            DatabaseSettings sourceSettings,
            DatabaseSettings destinationSettings,
            bool verify = true,
            int sampleSize = 25,
            CancellationToken token = default)
        {
            if (sourceSettings == null) throw new ArgumentNullException(nameof(sourceSettings));
            if (destinationSettings == null) throw new ArgumentNullException(nameof(destinationSettings));

            await using GraphRepositoryBase source = GraphRepositoryFactory.Create(sourceSettings);
            await using GraphRepositoryBase destination = GraphRepositoryFactory.Create(destinationSettings);

            await source.InitializeRepositoryAsync(token).ConfigureAwait(false);
            await destination.InitializeRepositoryAsync(token).ConfigureAwait(false);

            return await MigrateAsync(source, destination, verify, sampleSize, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Migrate data between two initialized repositories.
        /// </summary>
        /// <param name="source">Source repository.</param>
        /// <param name="destination">Destination repository.</param>
        /// <param name="verify">True to verify counts and sampled records after migration.</param>
        /// <param name="sampleSize">Number of records per entity type to sample during verification.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Migration result.</returns>
        public static async Task<StorageMigrationResult> MigrateAsync(
            GraphRepositoryBase source,
            GraphRepositoryBase destination,
            bool verify = true,
            int sampleSize = 25,
            CancellationToken token = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (destination == null) throw new ArgumentNullException(nameof(destination));

            StorageMigrationResult result = new StorageMigrationResult();
            Dictionary<Guid, Guid> roleGuidMap = new Dictionary<Guid, Guid>();

            await MigrateAuthorizationRoles(source, destination, result, roleGuidMap, token).ConfigureAwait(false);

            await foreach (TenantMetadata tenant in source.Tenant.ReadMany(token: token).WithCancellation(token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                await UpsertTenant(destination, tenant, token).ConfigureAwait(false);
                result.Migrated.Tenants++;

                await foreach (UserMaster user in source.User.ReadAllInTenant(tenant.GUID, token: token).WithCancellation(token).ConfigureAwait(false))
                {
                    await UpsertUser(destination, user, token).ConfigureAwait(false);
                    result.Migrated.Users++;
                }

                await foreach (Credential credential in source.Credential.ReadAllInTenant(tenant.GUID, token: token).WithCancellation(token).ConfigureAwait(false))
                {
                    await UpsertCredential(destination, credential, token).ConfigureAwait(false);
                    result.Migrated.Credentials++;
                }

                await foreach (Graph graph in source.Graph.ReadAllInTenant(tenant.GUID, token: token).WithCancellation(token).ConfigureAwait(false))
                {
                    await UpsertGraph(destination, graph, token).ConfigureAwait(false);
                    result.Migrated.Graphs++;

                    await foreach (Node node in source.Node.ReadAllInGraph(tenant.GUID, graph.GUID, token: token).WithCancellation(token).ConfigureAwait(false))
                    {
                        await UpsertNode(destination, node, token).ConfigureAwait(false);
                        result.Migrated.Nodes++;
                    }

                    await foreach (Edge edge in source.Edge.ReadAllInGraph(tenant.GUID, graph.GUID, token: token).WithCancellation(token).ConfigureAwait(false))
                    {
                        await UpsertEdge(destination, edge, token).ConfigureAwait(false);
                        result.Migrated.Edges++;
                    }

                    await foreach (LabelMetadata label in source.Label.ReadAllInGraph(tenant.GUID, graph.GUID, token: token).WithCancellation(token).ConfigureAwait(false))
                    {
                        await UpsertLabel(destination, label, token).ConfigureAwait(false);
                        result.Migrated.Labels++;
                    }

                    await foreach (TagMetadata tag in source.Tag.ReadAllInGraph(tenant.GUID, graph.GUID, token: token).WithCancellation(token).ConfigureAwait(false))
                    {
                        await UpsertTag(destination, tag, token).ConfigureAwait(false);
                        result.Migrated.Tags++;
                    }

                    await foreach (VectorMetadata vector in source.Vector.ReadAllInGraph(tenant.GUID, graph.GUID, token: token).WithCancellation(token).ConfigureAwait(false))
                    {
                        await UpsertVector(destination, vector, token).ConfigureAwait(false);
                        result.Migrated.Vectors++;
                    }
                }

                await MigrateAuthorizationAssignments(source, destination, tenant.GUID, result, roleGuidMap, token).ConfigureAwait(false);
            }

            if (verify)
                result.Verification = await VerifyAsync(source, destination, sampleSize, token).ConfigureAwait(false);

            result.CompletedUtc = DateTime.UtcNow;
            return result;
        }

        /// <summary>
        /// Verify that two repositories contain matching entity counts and sampled source records.
        /// </summary>
        /// <param name="source">Source repository.</param>
        /// <param name="destination">Destination repository.</param>
        /// <param name="sampleSize">Number of records per entity type to sample.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Verification result.</returns>
        public static async Task<StorageVerificationResult> VerifyAsync(
            GraphRepositoryBase source,
            GraphRepositoryBase destination,
            int sampleSize = 25,
            CancellationToken token = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            if (sampleSize < 0) throw new ArgumentOutOfRangeException(nameof(sampleSize));

            StorageVerificationResult result = new StorageVerificationResult
            {
                SourceCounts = await GetCountsAsync(source, token).ConfigureAwait(false),
                DestinationCounts = await GetCountsAsync(destination, token).ConfigureAwait(false)
            };

            CompareCount(result, nameof(StorageEntityCounts.Tenants), result.SourceCounts.Tenants, result.DestinationCounts.Tenants);
            CompareCount(result, nameof(StorageEntityCounts.Users), result.SourceCounts.Users, result.DestinationCounts.Users);
            CompareCount(result, nameof(StorageEntityCounts.Credentials), result.SourceCounts.Credentials, result.DestinationCounts.Credentials);
            CompareCount(result, nameof(StorageEntityCounts.Graphs), result.SourceCounts.Graphs, result.DestinationCounts.Graphs);
            CompareCount(result, nameof(StorageEntityCounts.Nodes), result.SourceCounts.Nodes, result.DestinationCounts.Nodes);
            CompareCount(result, nameof(StorageEntityCounts.Edges), result.SourceCounts.Edges, result.DestinationCounts.Edges);
            CompareCount(result, nameof(StorageEntityCounts.Labels), result.SourceCounts.Labels, result.DestinationCounts.Labels);
            CompareCount(result, nameof(StorageEntityCounts.Tags), result.SourceCounts.Tags, result.DestinationCounts.Tags);
            CompareCount(result, nameof(StorageEntityCounts.Vectors), result.SourceCounts.Vectors, result.DestinationCounts.Vectors);
            CompareCount(result, nameof(StorageEntityCounts.AuthorizationRoles), result.SourceCounts.AuthorizationRoles, result.DestinationCounts.AuthorizationRoles);
            CompareCount(result, nameof(StorageEntityCounts.UserRoleAssignments), result.SourceCounts.UserRoleAssignments, result.DestinationCounts.UserRoleAssignments);
            CompareCount(result, nameof(StorageEntityCounts.CredentialScopeAssignments), result.SourceCounts.CredentialScopeAssignments, result.DestinationCounts.CredentialScopeAssignments);

            await VerifySamples(source, destination, result, sampleSize, token).ConfigureAwait(false);
            return result;
        }

        /// <summary>
        /// Count entities in a repository.
        /// </summary>
        /// <param name="repository">Repository.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Entity counts.</returns>
        public static async Task<StorageEntityCounts> GetCountsAsync(GraphRepositoryBase repository, CancellationToken token = default)
        {
            if (repository == null) throw new ArgumentNullException(nameof(repository));

            StorageEntityCounts counts = new StorageEntityCounts
            {
                Tenants = await repository.Tenant.GetRecordCount(token: token).ConfigureAwait(false)
            };

            await foreach (TenantMetadata tenant in repository.Tenant.ReadMany(token: token).WithCancellation(token).ConfigureAwait(false))
            {
                counts.Users += await repository.User.GetRecordCount(tenant.GUID, token: token).ConfigureAwait(false);
                counts.Credentials += await repository.Credential.GetRecordCount(tenant.GUID, null, token: token).ConfigureAwait(false);
                counts.Graphs += await repository.Graph.GetRecordCount(tenant.GUID, token: token).ConfigureAwait(false);

                await foreach (Graph graph in repository.Graph.ReadAllInTenant(tenant.GUID, token: token).WithCancellation(token).ConfigureAwait(false))
                {
                    counts.Nodes += await repository.Node.GetRecordCount(tenant.GUID, graph.GUID, token: token).ConfigureAwait(false);
                    counts.Edges += await repository.Edge.GetRecordCount(tenant.GUID, graph.GUID, token: token).ConfigureAwait(false);
                    counts.Labels += await repository.Label.GetRecordCount(tenant.GUID, graph.GUID, token: token).ConfigureAwait(false);
                    counts.Tags += await repository.Tag.GetRecordCount(tenant.GUID, graph.GUID, token: token).ConfigureAwait(false);
                    counts.Vectors += await repository.Vector.GetRecordCount(tenant.GUID, graph.GUID, token: token).ConfigureAwait(false);
                }

                counts.UserRoleAssignments += (int)(await repository.AuthorizationRoles.SearchUserRoles(new UserRoleAssignmentSearchRequest
                {
                    TenantGUID = tenant.GUID,
                    PageSize = 1
                }, token).ConfigureAwait(false)).TotalCount;

                counts.CredentialScopeAssignments += (int)(await repository.AuthorizationRoles.SearchCredentialScopes(new CredentialScopeAssignmentSearchRequest
                {
                    TenantGUID = tenant.GUID,
                    PageSize = 1
                }, token).ConfigureAwait(false)).TotalCount;
            }

            counts.AuthorizationRoles = (int)(await repository.AuthorizationRoles.SearchRoles(new AuthorizationRoleSearchRequest
            {
                PageSize = 1
            }, token).ConfigureAwait(false)).TotalCount;

            return counts;
        }

        private static async Task MigrateAuthorizationRoles(
            GraphRepositoryBase source,
            GraphRepositoryBase destination,
            StorageMigrationResult result,
            Dictionary<Guid, Guid> roleGuidMap,
            CancellationToken token)
        {
            await foreach (AuthorizationRole role in ReadAuthorizationRoles(source, token))
            {
                token.ThrowIfCancellationRequested();
                AuthorizationRole roleToWrite = role.Clone();

                AuthorizationRole existing = await destination.AuthorizationRoles.ReadRoleByGuid(role.GUID, token).ConfigureAwait(false);
                if (existing != null)
                {
                    await destination.AuthorizationRoles.UpdateRole(roleToWrite, token).ConfigureAwait(false);
                    roleGuidMap[role.GUID] = role.GUID;
                    result.Migrated.AuthorizationRoles++;
                    continue;
                }

                AuthorizationRole existingByName = await destination.AuthorizationRoles.ReadRoleByName(role.TenantGUID, role.Name, token).ConfigureAwait(false);
                if (existingByName != null && role.BuiltIn)
                {
                    roleGuidMap[role.GUID] = existingByName.GUID;
                    continue;
                }

                await destination.AuthorizationRoles.CreateRole(roleToWrite, token).ConfigureAwait(false);
                roleGuidMap[role.GUID] = role.GUID;
                result.Migrated.AuthorizationRoles++;
            }
        }

        private static async Task MigrateAuthorizationAssignments(
            GraphRepositoryBase source,
            GraphRepositoryBase destination,
            Guid tenantGuid,
            StorageMigrationResult result,
            Dictionary<Guid, Guid> roleGuidMap,
            CancellationToken token)
        {
            await foreach (UserRoleAssignment assignment in ReadUserRoleAssignments(source, tenantGuid, token))
            {
                UserRoleAssignment writable = new UserRoleAssignment
                {
                    GUID = assignment.GUID,
                    TenantGUID = assignment.TenantGUID,
                    UserGUID = assignment.UserGUID,
                    RoleGUID = MapRoleGuid(assignment.RoleGUID, roleGuidMap),
                    RoleName = assignment.RoleName,
                    ResourceScope = assignment.ResourceScope,
                    GraphGUID = assignment.GraphGUID,
                    CreatedUtc = assignment.CreatedUtc,
                    LastUpdateUtc = assignment.LastUpdateUtc
                };

                if (await destination.AuthorizationRoles.ReadUserRoleByGuid(writable.GUID, token).ConfigureAwait(false) != null)
                    await destination.AuthorizationRoles.UpdateUserRole(writable, token).ConfigureAwait(false);
                else
                    await destination.AuthorizationRoles.CreateUserRole(writable, token).ConfigureAwait(false);

                result.Migrated.UserRoleAssignments++;
            }

            await foreach (CredentialScopeAssignment assignment in ReadCredentialScopeAssignments(source, tenantGuid, token))
            {
                CredentialScopeAssignment writable = new CredentialScopeAssignment
                {
                    GUID = assignment.GUID,
                    TenantGUID = assignment.TenantGUID,
                    CredentialGUID = assignment.CredentialGUID,
                    RoleGUID = MapRoleGuid(assignment.RoleGUID, roleGuidMap),
                    RoleName = assignment.RoleName,
                    ResourceScope = assignment.ResourceScope,
                    GraphGUID = assignment.GraphGUID,
                    Permissions = assignment.Permissions != null ? new List<AuthorizationPermissionEnum>(assignment.Permissions) : new List<AuthorizationPermissionEnum>(),
                    ResourceTypes = assignment.ResourceTypes != null ? new List<AuthorizationResourceTypeEnum>(assignment.ResourceTypes) : new List<AuthorizationResourceTypeEnum>(),
                    CreatedUtc = assignment.CreatedUtc,
                    LastUpdateUtc = assignment.LastUpdateUtc
                };

                if (await destination.AuthorizationRoles.ReadCredentialScopeByGuid(writable.GUID, token).ConfigureAwait(false) != null)
                    await destination.AuthorizationRoles.UpdateCredentialScope(writable, token).ConfigureAwait(false);
                else
                    await destination.AuthorizationRoles.CreateCredentialScope(writable, token).ConfigureAwait(false);

                result.Migrated.CredentialScopeAssignments++;
            }
        }

        private static Guid? MapRoleGuid(Guid? roleGuid, Dictionary<Guid, Guid> roleGuidMap)
        {
            if (!roleGuid.HasValue) return null;
            if (roleGuidMap.TryGetValue(roleGuid.Value, out Guid mapped)) return mapped;
            return roleGuid;
        }

        private static async IAsyncEnumerable<AuthorizationRole> ReadAuthorizationRoles(
            GraphRepositoryBase repository,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken token)
        {
            int page = 0;
            while (true)
            {
                AuthorizationRoleSearchResult result = await repository.AuthorizationRoles.SearchRoles(new AuthorizationRoleSearchRequest
                {
                    Page = page,
                    PageSize = 1000
                }, token).ConfigureAwait(false);

                if (result == null || result.Objects == null || result.Objects.Count == 0) yield break;
                foreach (AuthorizationRole role in result.Objects) yield return role;
                page++;
            }
        }

        private static async IAsyncEnumerable<UserRoleAssignment> ReadUserRoleAssignments(
            GraphRepositoryBase repository,
            Guid tenantGuid,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken token)
        {
            int page = 0;
            while (true)
            {
                UserRoleAssignmentSearchResult result = await repository.AuthorizationRoles.SearchUserRoles(new UserRoleAssignmentSearchRequest
                {
                    TenantGUID = tenantGuid,
                    Page = page,
                    PageSize = 1000
                }, token).ConfigureAwait(false);

                if (result == null || result.Objects == null || result.Objects.Count == 0) yield break;
                foreach (UserRoleAssignment assignment in result.Objects) yield return assignment;
                page++;
            }
        }

        private static async IAsyncEnumerable<CredentialScopeAssignment> ReadCredentialScopeAssignments(
            GraphRepositoryBase repository,
            Guid tenantGuid,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken token)
        {
            int page = 0;
            while (true)
            {
                CredentialScopeAssignmentSearchResult result = await repository.AuthorizationRoles.SearchCredentialScopes(new CredentialScopeAssignmentSearchRequest
                {
                    TenantGUID = tenantGuid,
                    Page = page,
                    PageSize = 1000
                }, token).ConfigureAwait(false);

                if (result == null || result.Objects == null || result.Objects.Count == 0) yield break;
                foreach (CredentialScopeAssignment assignment in result.Objects) yield return assignment;
                page++;
            }
        }

        private static async Task UpsertTenant(GraphRepositoryBase destination, TenantMetadata tenant, CancellationToken token)
        {
            if (await destination.Tenant.ExistsByGuid(tenant.GUID, token).ConfigureAwait(false))
                await destination.Tenant.Update(tenant, token).ConfigureAwait(false);
            else
                await destination.Tenant.Create(tenant, token).ConfigureAwait(false);
        }

        private static async Task UpsertUser(GraphRepositoryBase destination, UserMaster user, CancellationToken token)
        {
            if (await destination.User.ExistsByGuid(user.TenantGUID, user.GUID, token).ConfigureAwait(false))
                await destination.User.Update(user, token).ConfigureAwait(false);
            else
                await destination.User.Create(user, token).ConfigureAwait(false);
        }

        private static async Task UpsertCredential(GraphRepositoryBase destination, Credential credential, CancellationToken token)
        {
            if (await destination.Credential.ExistsByGuid(credential.TenantGUID, credential.GUID, token).ConfigureAwait(false))
                await destination.Credential.Update(credential, token).ConfigureAwait(false);
            else
                await destination.Credential.Create(credential, token).ConfigureAwait(false);
        }

        private static async Task UpsertGraph(GraphRepositoryBase destination, Graph graph, CancellationToken token)
        {
            if (await destination.Graph.ExistsByGuid(graph.TenantGUID, graph.GUID, token).ConfigureAwait(false))
                await destination.Graph.Update(graph, token).ConfigureAwait(false);
            else
                await destination.Graph.Create(graph, token).ConfigureAwait(false);
        }

        private static async Task UpsertNode(GraphRepositoryBase destination, Node node, CancellationToken token)
        {
            if (await destination.Node.ExistsByGuid(node.TenantGUID, node.GUID, token).ConfigureAwait(false))
                await destination.Node.Update(node, token).ConfigureAwait(false);
            else
                await destination.Node.Create(node, token).ConfigureAwait(false);
        }

        private static async Task UpsertEdge(GraphRepositoryBase destination, Edge edge, CancellationToken token)
        {
            if (await destination.Edge.ExistsByGuid(edge.TenantGUID, edge.GUID, token).ConfigureAwait(false))
                await destination.Edge.Update(edge, token).ConfigureAwait(false);
            else
                await destination.Edge.Create(edge, token).ConfigureAwait(false);
        }

        private static async Task UpsertLabel(GraphRepositoryBase destination, LabelMetadata label, CancellationToken token)
        {
            if (await destination.Label.ExistsByGuid(label.TenantGUID, label.GUID, token).ConfigureAwait(false))
                await destination.Label.Update(label, token).ConfigureAwait(false);
            else
                await destination.Label.Create(label, token).ConfigureAwait(false);
        }

        private static async Task UpsertTag(GraphRepositoryBase destination, TagMetadata tag, CancellationToken token)
        {
            if (await destination.Tag.ExistsByGuid(tag.TenantGUID, tag.GUID, token).ConfigureAwait(false))
                await destination.Tag.Update(tag, token).ConfigureAwait(false);
            else
                await destination.Tag.Create(tag, token).ConfigureAwait(false);
        }

        private static async Task UpsertVector(GraphRepositoryBase destination, VectorMetadata vector, CancellationToken token)
        {
            if (await destination.Vector.ExistsByGuid(vector.TenantGUID, vector.GUID, token).ConfigureAwait(false))
                await destination.Vector.Update(vector, token).ConfigureAwait(false);
            else
                await destination.Vector.Create(vector, token).ConfigureAwait(false);
        }

        private static void CompareCount(StorageVerificationResult result, string name, int source, int destination)
        {
            if (source != destination)
                result.Differences.Add(name + " count mismatch: source=" + source + ", destination=" + destination + ".");
        }

        private static async Task VerifySamples(
            GraphRepositoryBase source,
            GraphRepositoryBase destination,
            StorageVerificationResult result,
            int sampleSize,
            CancellationToken token)
        {
            if (sampleSize == 0) return;

            int sampledTenants = 0;
            await foreach (TenantMetadata tenant in source.Tenant.ReadMany(token: token).WithCancellation(token).ConfigureAwait(false))
            {
                if (sampledTenants++ >= sampleSize) break;
                result.SampledGuids.Add(tenant.GUID);
                if (!await destination.Tenant.ExistsByGuid(tenant.GUID, token).ConfigureAwait(false))
                    result.Differences.Add("Missing sampled tenant " + tenant.GUID + ".");

                await VerifyTenantSamples(source, destination, result, tenant.GUID, sampleSize, token).ConfigureAwait(false);
            }
        }

        private static async Task VerifyTenantSamples(
            GraphRepositoryBase source,
            GraphRepositoryBase destination,
            StorageVerificationResult result,
            Guid tenantGuid,
            int sampleSize,
            CancellationToken token)
        {
            foreach (UserMaster user in await TakeAsync(source.User.ReadAllInTenant(tenantGuid, token: token), sampleSize, token).ConfigureAwait(false))
            {
                result.SampledGuids.Add(user.GUID);
                if (!await destination.User.ExistsByGuid(tenantGuid, user.GUID, token).ConfigureAwait(false))
                    result.Differences.Add("Missing sampled user " + user.GUID + ".");
            }

            foreach (Credential credential in await TakeAsync(source.Credential.ReadAllInTenant(tenantGuid, token: token), sampleSize, token).ConfigureAwait(false))
            {
                result.SampledGuids.Add(credential.GUID);
                if (!await destination.Credential.ExistsByGuid(tenantGuid, credential.GUID, token).ConfigureAwait(false))
                    result.Differences.Add("Missing sampled credential " + credential.GUID + ".");
            }

            foreach (Graph graph in await TakeAsync(source.Graph.ReadAllInTenant(tenantGuid, token: token), sampleSize, token).ConfigureAwait(false))
            {
                result.SampledGuids.Add(graph.GUID);
                if (!await destination.Graph.ExistsByGuid(tenantGuid, graph.GUID, token).ConfigureAwait(false))
                    result.Differences.Add("Missing sampled graph " + graph.GUID + ".");

                await VerifyGraphSamples(source, destination, result, tenantGuid, graph.GUID, sampleSize, token).ConfigureAwait(false);
            }
        }

        private static async Task VerifyGraphSamples(
            GraphRepositoryBase source,
            GraphRepositoryBase destination,
            StorageVerificationResult result,
            Guid tenantGuid,
            Guid graphGuid,
            int sampleSize,
            CancellationToken token)
        {
            foreach (Node node in await TakeAsync(source.Node.ReadAllInGraph(tenantGuid, graphGuid, token: token), sampleSize, token).ConfigureAwait(false))
            {
                result.SampledGuids.Add(node.GUID);
                if (!await destination.Node.ExistsByGuid(tenantGuid, node.GUID, token).ConfigureAwait(false))
                    result.Differences.Add("Missing sampled node " + node.GUID + ".");
            }

            foreach (Edge edge in await TakeAsync(source.Edge.ReadAllInGraph(tenantGuid, graphGuid, token: token), sampleSize, token).ConfigureAwait(false))
            {
                result.SampledGuids.Add(edge.GUID);
                if (!await destination.Edge.ExistsByGuid(tenantGuid, edge.GUID, token).ConfigureAwait(false))
                    result.Differences.Add("Missing sampled edge " + edge.GUID + ".");
            }

            foreach (LabelMetadata label in await TakeAsync(source.Label.ReadAllInGraph(tenantGuid, graphGuid, token: token), sampleSize, token).ConfigureAwait(false))
            {
                result.SampledGuids.Add(label.GUID);
                if (!await destination.Label.ExistsByGuid(tenantGuid, label.GUID, token).ConfigureAwait(false))
                    result.Differences.Add("Missing sampled label " + label.GUID + ".");
            }

            foreach (TagMetadata tag in await TakeAsync(source.Tag.ReadAllInGraph(tenantGuid, graphGuid, token: token), sampleSize, token).ConfigureAwait(false))
            {
                result.SampledGuids.Add(tag.GUID);
                if (!await destination.Tag.ExistsByGuid(tenantGuid, tag.GUID, token).ConfigureAwait(false))
                    result.Differences.Add("Missing sampled tag " + tag.GUID + ".");
            }

            foreach (VectorMetadata vector in await TakeAsync(source.Vector.ReadAllInGraph(tenantGuid, graphGuid, token: token), sampleSize, token).ConfigureAwait(false))
            {
                result.SampledGuids.Add(vector.GUID);
                if (!await destination.Vector.ExistsByGuid(tenantGuid, vector.GUID, token).ConfigureAwait(false))
                    result.Differences.Add("Missing sampled vector " + vector.GUID + ".");
            }
        }

        private static async Task<List<T>> TakeAsync<T>(IAsyncEnumerable<T> values, int count, CancellationToken token)
        {
            List<T> result = new List<T>();
            if (values == null || count <= 0) return result;

            await foreach (T value in values.WithCancellation(token).ConfigureAwait(false))
            {
                result.Add(value);
                if (result.Count >= count) break;
            }

            return result;
        }
    }
}
