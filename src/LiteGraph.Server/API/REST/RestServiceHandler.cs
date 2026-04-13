namespace LiteGraph.Server.API.REST
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.Indexing.Vector;
    using LiteGraph.Serialization;
    using LiteGraph.Server.API.Agnostic;
    using LiteGraph.Server.Classes;
    using LiteGraph.Server.Services;
    using ApiErrorResponse = LiteGraph.Server.Classes.ApiErrorResponse;
    using AuthenticationResultEnum = LiteGraph.Server.Classes.AuthenticationResultEnum;
    using AuthorizationResultEnum = LiteGraph.Server.Classes.AuthorizationResultEnum;
    using SyslogLogging;
    using WatsonWebserver;
    using WatsonWebserver.Core;
    using WatsonWebserver.Core.OpenApi;

    internal class RestServiceHandler : IDisposable
    {
        #region Internal-Members

        #endregion

        #region Private-Members

        private readonly string _Header = "[RestServiceHandler] ";
        static string _Hostname = Dns.GetHostName();
        private Settings _Settings = null;
        private LoggingModule _Logging = null;
        private LiteGraphClient _LiteGraph = null;
        private Serializer _Serializer = null;
        private AuthenticationService _Authentication = null;
        private ServiceHandler _ServiceHandler = null;

        private Webserver _Webserver = null;
        private bool _Disposed = false;

        private List<string> _Localhost = new List<string>
        {
            "127.0.0.1",
            "localhost",
            "::1"
        };

        #endregion

        #region Constructors-and-Factories

        internal RestServiceHandler(
            Settings settings,
            LoggingModule logging,
            LiteGraphClient litegraph,
            Serializer serializer,
            AuthenticationService auth,
            ServiceHandler service)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _LiteGraph = litegraph ?? throw new ArgumentNullException(nameof(litegraph));
            _Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _Authentication = auth ?? throw new ArgumentNullException(nameof(auth));
            _ServiceHandler = service ?? throw new ArgumentNullException(nameof(service));

            _Webserver = new Webserver(_Settings.Rest, DefaultRoute);
            _Webserver.Routes.PreRouting = PreRoutingHandler;
            _Webserver.Routes.AuthenticateRequest = AuthenticateRequest;
            _Webserver.Routes.PostRouting = PostRoutingHandler;
            _Webserver.Routes.Preflight = OptionsHandler;
            _Webserver.Routes.Exception = ExceptionRoute;

            InitializeRoutes();

            _Webserver.UseOpenApi(openApi =>
            {
                openApi.Info.Title = "LiteGraph API";
                openApi.Info.Version = "v5.0";
                openApi.Info.Description = "LiteGraph is a lightweight graph database with vector search, multi-tenancy, and AI agent integration. This API provides full CRUD operations for graphs, nodes, edges, labels, tags, and vectors with built-in HNSW vector indexing.";
                openApi.Info.Contact = new OpenApiContact
                {
                    Name = "LiteGraph",
                    Url = "https://github.com/jchristn/LiteGraph"
                };
                openApi.Info.License = new OpenApiLicense
                {
                    Name = "MIT",
                    Url = "https://github.com/jchristn/LiteGraph/blob/main/LICENSE.md"
                };

                openApi.Tags.Add(new OpenApiTag { Name = "System", Description = "Health checks and system information" });
                openApi.Tags.Add(new OpenApiTag { Name = "Tokens", Description = "Authentication token management" });
                openApi.Tags.Add(new OpenApiTag { Name = "Admin", Description = "Administrative operations including backups, restore, and database flush" });
                openApi.Tags.Add(new OpenApiTag { Name = "Tenants", Description = "Multi-tenant management and statistics" });
                openApi.Tags.Add(new OpenApiTag { Name = "Users", Description = "User account management" });
                openApi.Tags.Add(new OpenApiTag { Name = "Credentials", Description = "Bearer token credential management" });
                openApi.Tags.Add(new OpenApiTag { Name = "Graphs", Description = "Graph CRUD, search, statistics, and GEXF export" });
                openApi.Tags.Add(new OpenApiTag { Name = "VectorIndex", Description = "HNSW vector index configuration, rebuild, and statistics" });
                openApi.Tags.Add(new OpenApiTag { Name = "Nodes", Description = "Node CRUD, search, and batch operations" });
                openApi.Tags.Add(new OpenApiTag { Name = "NodeTraversal", Description = "Node relationship traversal: parents, children, neighbors, and edges" });
                openApi.Tags.Add(new OpenApiTag { Name = "Edges", Description = "Edge CRUD, search, and batch operations" });
                openApi.Tags.Add(new OpenApiTag { Name = "Routes", Description = "Route finding and graph traversal between nodes" });
                openApi.Tags.Add(new OpenApiTag { Name = "Labels", Description = "Label management for graphs, nodes, and edges" });
                openApi.Tags.Add(new OpenApiTag { Name = "Tags", Description = "Key-value tag management for graphs, nodes, and edges" });
                openApi.Tags.Add(new OpenApiTag { Name = "Vectors", Description = "Vector embedding storage, search, and management" });

                openApi.SecuritySchemes["BearerToken"] = new OpenApiSecurityScheme
                {
                    Type = "http",
                    Scheme = "bearer",
                    Description = "Bearer token authentication. Use the admin token or a credential bearer token."
                };
                openApi.SecuritySchemes["EmailPassword"] = new OpenApiSecurityScheme
                {
                    Type = "apiKey",
                    Name = "x-email",
                    In = "header",
                    Description = "Email-based authentication. Requires x-email, x-password, and x-tenant-guid headers."
                };
                openApi.SecuritySchemes["SecurityToken"] = new OpenApiSecurityScheme
                {
                    Type = "apiKey",
                    Name = "x-token",
                    In = "header",
                    Description = "Temporal security token (24-hour expiry). Obtain via GET /v1.0/token."
                };

                openApi.IncludePreAuthRoutes = true;
                openApi.IncludePostAuthRoutes = true;
            });

            _Logging.Info(_Header + "starting REST server on " + _Settings.Rest.Prefix);
            _Webserver.Start();

            if (_Localhost.Contains(_Settings.Rest.Hostname))
            {
                _Logging.Alert(_Header + Environment.NewLine + Environment.NewLine
                    + "NOTICE" + Environment.NewLine
                    + "------" + Environment.NewLine
                    + "LiteGraph is configured to listen on localhost and will not be externally accessible." + Environment.NewLine
                    + "Modify " + Constants.SettingsFile + " to change the REST listener hostname to make externally accessible." + Environment.NewLine);
            }
        }

        #endregion

        #region Internal-Methods

        internal void Stop()
        {
            if (_Disposed) return;
            if (_Webserver == null) return;

            _Logging.Info(_Header + "stopping REST server");
            _Webserver.Stop();
        }

        public void Dispose()
        {
            if (_Disposed) return;

            try
            {
                Stop();
            }
            catch (Exception e)
            {
                _Logging?.Warn(_Header + "error while stopping REST server: " + e.Message);
            }
            finally
            {
                if (_Webserver is IDisposable disposableWebserver) disposableWebserver.Dispose();

                _Webserver = null;
                _Disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        internal void InitializeRoutes()
        {
            _Webserver.Routes.PreAuthentication.Static.Add(HttpMethod.HEAD, "/", LoopbackRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Health check", "System"));
            _Webserver.Routes.PreAuthentication.Static.Add(HttpMethod.GET, "/", RootRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Server information", "System"));
            _Webserver.Routes.PreAuthentication.Static.Add(HttpMethod.GET, "/favicon.ico", FaviconRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Favicon", "System"));
            _Webserver.Routes.PreAuthentication.Static.Add(HttpMethod.GET, "/v1.0/token/tenants", TokenTenantsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("List tenants for email", "Tokens"));

            #region Tokens

            _Webserver.Routes.PostAuthentication.Static.Add(HttpMethod.GET, "/v1.0/token", TokenCreateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Generate authentication token", "Tokens"));
            _Webserver.Routes.PostAuthentication.Static.Add(HttpMethod.GET, "/v1.0/token/details", TokenDetailsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Get token details", "Tokens"));

            #endregion

            #region Admin

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/backups", BackupReadAllRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("List all backups", "Admin"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/backups/{backupFilename}", BackupReadRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read backup file", "Admin"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.HEAD, "/v1.0/backups/{backupFilename}", BackupExistsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Check if backup exists", "Admin"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/backups", BackupRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Create backup", "Admin"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/backups/{backupFilename}", BackupDeleteRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete backup", "Admin"));

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/flush", FlushRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Flush database to disk", "Admin"));

            #endregion

            #region Tenants

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants", TenantCreateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Create tenant", "Tenants"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants", TenantReadManyRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("List tenants", "Tenants"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v2.0/tenants", TenantEnumerateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Enumerate tenants", "Tenants"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v2.0/tenants", TenantEnumerateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Enumerate tenants (POST)", "Tenants"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/stats", TenantStatisticsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Get all tenant statistics", "Tenants"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}", TenantReadRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read tenant", "Tenants"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/stats", TenantStatisticsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Get tenant statistics", "Tenants"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.HEAD, "/v1.0/tenants/{tenantGuid}", TenantExistsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Check if tenant exists", "Tenants"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}", TenantUpdateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Update tenant", "Tenants"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}", TenantDeleteRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete tenant", "Tenants"));

            #endregion

            #region Users

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/users", UserCreateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Create user", "Users"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/users", UserReadManyRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("List users", "Users"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v2.0/tenants/{tenantGuid}/users", UserEnumerateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Enumerate users", "Users"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v2.0/tenants/{tenantGuid}/users", UserEnumerateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Enumerate users (POST)", "Users"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/users/{userGuid}", UserReadRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read user", "Users"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.HEAD, "/v1.0/tenants/{tenantGuid}/users/{userGuid}", UserExistsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Check if user exists", "Users"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/users/{userGuid}", UserUpdateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Update user", "Users"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/users/{userGuid}", UserDeleteRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete user", "Users"));

            #endregion

            #region Credentials

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/credentials", CredentialCreateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Create credential", "Credentials"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/credentials", CredentialReadManyRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("List credentials", "Credentials"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v2.0/tenants/{tenantGuid}/credentials", CredentialEnumerateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Enumerate credentials", "Credentials"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/credentials/bearer/{bearerToken}", CredentialReadByBearerTokenRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Lookup credential by bearer token", "Credentials"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v2.0/tenants/{tenantGuid}/credentials", CredentialEnumerateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Enumerate credentials (POST)", "Credentials"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/credentials/{credentialGuid}", CredentialReadRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read credential", "Credentials"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.HEAD, "/v1.0/tenants/{tenantGuid}/credentials/{credentialGuid}", CredentialExistsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Check if credential exists", "Credentials"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/credentials/{credentialGuid}", CredentialUpdateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Update credential", "Credentials"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/users/{userGuid}/credentials", CredentialDeleteByUserRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete credentials by user", "Credentials"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/credentials", CredentialDeleteAllInTenantRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete all credentials in tenant", "Credentials"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/credentials/{credentialGuid}", CredentialDeleteRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete credential", "Credentials"));

            #endregion

            #region Labels

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/labels", LabelCreateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Create label", "Labels"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/labels/bulk", LabelCreateManyRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Create multiple labels", "Labels"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/labels", LabelReadManyRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("List labels", "Labels"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v2.0/tenants/{tenantGuid}/labels", LabelEnumerateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Enumerate labels", "Labels"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v2.0/tenants/{tenantGuid}/labels", LabelEnumerateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Enumerate labels (POST)", "Labels"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/labels/all", LabelReadAllInTenantRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read all labels in tenant", "Labels"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/labels/all", LabelReadAllInGraphRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read all labels in graph", "Labels"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/labels", LabelReadManyGraphRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read graph labels", "Labels"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/labels", LabelReadManyNodeRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read node labels", "Labels"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/{edgeGuid}/labels", LabelReadManyEdgeRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read edge labels", "Labels"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/labels/{labelGuid}", LabelReadRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read label", "Labels"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.HEAD, "/v1.0/tenants/{tenantGuid}/labels/{labelGuid}", LabelExistsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Check if label exists", "Labels"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/labels/{labelGuid}", LabelUpdateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Update label", "Labels"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/labels/all", LabelDeleteAllInTenantRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete all labels in tenant", "Labels"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/labels/all", LabelDeleteAllInGraphRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete all labels in graph", "Labels"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/labels", LabelDeleteGraphLabelsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete graph labels", "Labels"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/labels", LabelDeleteNodeLabelsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete node labels", "Labels"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/{edgeGuid}/labels", LabelDeleteEdgeLabelsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete edge labels", "Labels"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/labels/bulk", LabelDeleteManyRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete multiple labels", "Labels"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/labels/{labelGuid}", LabelDeleteRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete label", "Labels"));

            #endregion

            #region Tags

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/tags", TagCreateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Create tag", "Tags"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/tags/bulk", TagCreateManyRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Create multiple tags", "Tags"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/tags/all", TagReadAllInTenantRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read all tags in tenant", "Tags"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/tags", TagReadManyRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("List tags", "Tags"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v2.0/tenants/{tenantGuid}/tags", TagEnumerateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Enumerate tags", "Tags"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/tags/all", TagReadAllInGraphRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read all tags in graph", "Tags"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/tags", TagReadManyGraphRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read graph tags", "Tags"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/tags", TagReadManyNodeRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read node tags", "Tags"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/{edgeGuid}/tags", TagReadManyEdgeRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read edge tags", "Tags"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v2.0/tenants/{tenantGuid}/tags", TagEnumerateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Enumerate tags (POST)", "Tags"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/tags/{tagGuid}", TagReadRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read tag", "Tags"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.HEAD, "/v1.0/tenants/{tenantGuid}/tags/{tagGuid}", TagExistsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Check if tag exists", "Tags"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/tags/{tagGuid}", TagUpdateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Update tag", "Tags"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/tags/bulk", TagDeleteManyRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete multiple tags", "Tags"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/tags/all", TagDeleteAllInTenantRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete all tags in tenant", "Tags"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/tags/all", TagDeleteAllInGraphRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete all tags in graph", "Tags"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/tags", TagDeleteGraphTagsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete graph tags", "Tags"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/tags", TagDeleteNodeTagsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete node tags", "Tags"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/{edgeGuid}/tags", TagDeleteEdgeTagsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete edge tags", "Tags"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/tags/{tagGuid}", TagDeleteRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete tag", "Tags"));

            #endregion

            #region Vectors

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/vectors", VectorCreateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Create vector", "Vectors"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/vectors/bulk", VectorCreateManyRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Create multiple vectors", "Vectors"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/tenants/{tenantGuid}/vectors", VectorSearchRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Search vectors", "Vectors"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/vectors", VectorReadManyRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("List vectors", "Vectors"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v2.0/tenants/{tenantGuid}/vectors", VectorEnumerateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Enumerate vectors", "Vectors"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v2.0/tenants/{tenantGuid}/vectors", VectorEnumerateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Enumerate vectors (POST)", "Vectors"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/vectors/all", VectorReadAllInTenantRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read all vectors in tenant", "Vectors"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectors/all", VectorReadAllInGraphRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read all vectors in graph", "Vectors"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectors", VectorReadManyGraphRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read graph vectors", "Vectors"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/vectors", VectorReadManyNodeRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read node vectors", "Vectors"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/{edgeGuid}/vectors", VectorReadManyEdgeRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read edge vectors", "Vectors"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/vectors/{vectorGuid}", VectorReadRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read vector", "Vectors"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.HEAD, "/v1.0/tenants/{tenantGuid}/vectors/{vectorGuid}", VectorExistsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Check if vector exists", "Vectors"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/vectors/{vectorGuid}", VectorUpdateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Update vector", "Vectors"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/vectors/bulk", VectorDeleteManyRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete multiple vectors", "Vectors"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/vectors/all", VectorDeleteAllInTenantRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete all vectors in tenant", "Vectors"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectors/all", VectorDeleteAllInGraphRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete all vectors in graph", "Vectors"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectors", VectorDeleteGraphVectorsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete graph vectors", "Vectors"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/vectors", VectorDeleteNodeVectorsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete node vectors", "Vectors"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/{edgeGuid}/vectors", VectorDeleteEdgeVectorsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete edge vectors", "Vectors"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/vectors/{vectorGuid}", VectorDeleteRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete vector", "Vectors"));

            #endregion

            #region Graphs

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/graphs", GraphCreateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Create graph", "Graphs"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/tenants/{tenantGuid}/graphs/first", GraphReadFirstRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read first matching graph", "Graphs"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/tenants/{tenantGuid}/graphs/search", GraphSearchRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Search graphs", "Graphs"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/stats", GraphStatisticsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Get all graph statistics", "Graphs"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/all", GraphReadAllInTenantRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read all graphs in tenant", "Graphs"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}", GraphReadRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read graph", "Graphs"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/stats", GraphStatisticsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Get graph statistics", "Graphs"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.HEAD, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}", GraphExistsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Check if graph exists", "Graphs"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs", GraphReadManyRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("List graphs", "Graphs"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v2.0/tenants/{tenantGuid}/graphs", GraphEnumerateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Enumerate graphs", "Graphs"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v2.0/tenants/{tenantGuid}/graphs", GraphEnumerateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Enumerate graphs (POST)", "Graphs"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}", GraphUpdateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Update graph", "Graphs"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/all", GraphDeleteAllInTenantRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete all graphs in tenant", "Graphs"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}", GraphDeleteRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete graph", "Graphs"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/existence", GraphExistenceRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Check batch existence", "Graphs"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/subgraph", GraphSubgraphRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Get subgraph from node", "Graphs"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/subgraph/stats", GraphSubgraphStatisticsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Get subgraph statistics", "Graphs"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/export/gexf", GraphGexfExportRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Export graph as GEXF", "Graphs"));

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectorindex/enable", GraphEnableVectorIndexRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Enable vector indexing", "VectorIndex"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectorindex/config", GraphGetVectorIndexConfigRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Get vector index configuration", "VectorIndex"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectorindex", GraphDisableVectorIndexRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Disable vector indexing", "VectorIndex"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectorindex/rebuild", GraphRebuildVectorIndexRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Rebuild vector index", "VectorIndex"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectorindex/stats", GraphVectorIndexStatsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Get vector index statistics", "VectorIndex"));

            // v2.0 versions for consistency
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v2.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectorindex/enable", GraphEnableVectorIndexRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Enable vector indexing", "VectorIndex"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v2.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectorindex/config", GraphGetVectorIndexConfigRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Get vector index configuration", "VectorIndex"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v2.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectorindex", GraphDisableVectorIndexRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Disable vector indexing", "VectorIndex"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v2.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectorindex/rebuild", GraphRebuildVectorIndexRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Rebuild vector index", "VectorIndex"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v2.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectorindex/stats", GraphVectorIndexStatsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Get vector index statistics", "VectorIndex"));

            #endregion

            #region Nodes

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes", NodeCreateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Create node", "Nodes"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/bulk", NodeCreateManyRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Create multiple nodes", "Nodes"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/first", NodeReadFirstRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read first matching node", "Nodes"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/search", NodeSearchRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Search nodes", "Nodes"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/all", NodeReadAllInGraphRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read all nodes in graph", "Nodes"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/mostconnected", NodeReadMostConnectedRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read most connected nodes", "Nodes"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/leastconnected", NodeReadLeastConnectedRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read least connected nodes", "Nodes"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}", NodeReadRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read node", "Nodes"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.HEAD, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}", NodeExistsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Check if node exists", "Nodes"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes", NodeReadManyRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("List nodes", "Nodes"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v2.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes", NodeEnumerateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Enumerate nodes", "Nodes"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v2.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes", NodeEnumerateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Enumerate nodes (POST)", "Nodes"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}", NodeUpdateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Update node", "Nodes"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/all", NodeDeleteAllRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete all nodes in graph", "Nodes"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/bulk", NodeDeleteManyRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete multiple nodes", "Nodes"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}", NodeDeleteRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete node", "Nodes"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/nodes/all", NodeReadAllInTenantRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read all nodes in tenant", "Nodes"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/nodes/all", NodeDeleteAllInTenantRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete all nodes in tenant", "Nodes"));

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/edges/from", EdgesFromNodeRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Get outgoing edges from node", "NodeTraversal"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/edges/to", EdgesToNodeRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Get incoming edges to node", "NodeTraversal"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/edges", AllEdgesToNodeRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Get all edges for node", "NodeTraversal"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/neighbors", NodeNeighborsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Get node neighbors", "NodeTraversal"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/parents", NodeParentsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Get parent nodes", "NodeTraversal"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/children", NodeChildrenRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Get child nodes", "NodeTraversal"));

            #endregion

            #region Edges

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges", EdgeCreateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Create edge", "Edges"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/bulk", EdgeCreateManyRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Create multiple edges", "Edges"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/between", EdgesBetweenRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Get edges between nodes", "Edges"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/first", EdgeReadFirstRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read first matching edge", "Edges"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/search", EdgeSearchRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Search edges", "Edges"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/edges/all", EdgeReadAllInTenantRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read all edges in tenant", "Edges"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/all", EdgeReadAllInGraphRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read all edges in graph", "Edges"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/{edgeGuid}", EdgeReadRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read edge", "Edges"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.HEAD, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/{edgeGuid}", EdgeExistsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Check if edge exists", "Edges"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges", EdgeReadManyRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("List edges", "Edges"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v2.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges", EdgeEnumerateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Enumerate edges", "Edges"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v2.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges", EdgeEnumerateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Enumerate edges (POST)", "Edges"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/{edgeGuid}", EdgeUpdateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Update edge", "Edges"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/all", EdgeDeleteAllRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete all edges in graph", "Edges"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/bulk", EdgeDeleteManyRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete multiple edges", "Edges"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/{edgeGuid}", EdgeDeleteRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete edge", "Edges"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/edges/all", EdgeDeleteAllInTenantRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete all edges in tenant", "Edges"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/edges", EdgeDeleteNodeEdgesRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete all edges for node", "Edges"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/edges/bulk", EdgeDeleteNodeEdgesManyRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete edges for multiple nodes", "Edges"));

            #endregion

            #region Routes-and-Traversal

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/routes", GetRoutesRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Find routes between nodes", "Routes"));

            #endregion
        }

        internal async Task OptionsHandler(HttpContextBase ctx)
        {
            NameValueCollection responseHeaders = new NameValueCollection(StringComparer.InvariantCultureIgnoreCase);

            string[] requestedHeaders = null;
            string headers = "";

            if (ctx.Request.Headers != null)
            {
                for (int i = 0; i < ctx.Request.Headers.Count; i++)
                {
                    string key = ctx.Request.Headers.GetKey(i);
                    string value = ctx.Request.Headers.Get(i);
                    if (String.IsNullOrEmpty(key)) continue;
                    if (String.IsNullOrEmpty(value)) continue;
                    if (String.Compare(key.ToLower(), "access-control-request-headers") == 0)
                    {
                        requestedHeaders = value.Split(',');
                        break;
                    }
                }
            }

            if (requestedHeaders != null)
            {
                foreach (string curr in requestedHeaders)
                {
                    headers += ", " + curr;
                }
            }

            responseHeaders.Add("Access-Control-Allow-Methods", "OPTIONS, HEAD, GET, PUT, POST, DELETE");
            responseHeaders.Add("Access-Control-Allow-Headers", "*, Content-Type, X-Requested-With, " + headers);
            responseHeaders.Add("Access-Control-Expose-Headers", "Content-Type, X-Requested-With, " + headers);
            responseHeaders.Add("Access-Control-Allow-Origin", "*");
            responseHeaders.Add("Accept", "*/*");
            responseHeaders.Add("Accept-Language", "en-US, en");
            responseHeaders.Add("Accept-Charset", "ISO-8859-1, utf-8");
            responseHeaders.Add("Connection", "keep-alive");

            ctx.Response.StatusCode = 200;
            ctx.Response.Headers = responseHeaders;
            await ctx.Response.Send();
            return;
        }

        internal async Task PreRoutingHandler(HttpContextBase ctx)
        {
            RequestContext req = null;

            ctx.Response.Headers.Add(Constants.HostnameHeader, _Hostname);
            ctx.Response.ContentType = Constants.JsonContentType;

            try
            {
                req = new RequestContext(ctx);
            }
            catch (FormatException fe)
            {
                _Logging.Warn(_Header + "format exception building request context" + Environment.NewLine + fe.ToString());
                ctx.Response.StatusCode = 400;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest, null, fe.Message), true));
                return;
            }
            catch (ArgumentOutOfRangeException aore)
            {
                _Logging.Warn(_Header + ctx.Request.Source.IpAddress + " argument out of range exception building request context" + Environment.NewLine + aore.ToString());
                ctx.Response.StatusCode = 400;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest, null, aore.Message), true));
                return;
            }
            catch (ArgumentNullException ane)
            {
                _Logging.Warn(_Header + ctx.Request.Source.IpAddress + " argument null exception building request context" + Environment.NewLine + ane.ToString());
                ctx.Response.StatusCode = 400;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest, null, ane.Message), true));
                return;
            }
            catch (ArgumentException ae)
            {
                _Logging.Warn(_Header + ctx.Request.Source.IpAddress + " argument exception building request context" + Environment.NewLine + ae.ToString());
                ctx.Response.StatusCode = 400;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest, null, ae.Message), true));
                return;
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + ctx.Request.Source.IpAddress + " exception building request context" + Environment.NewLine + e.ToString());
                ctx.Response.StatusCode = 500;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.InternalError, null, e.Message), true));
                return;
            }

            ctx.Metadata = req;
            if (_Settings.Debug.Requests)
                _Logging.Debug(_Serializer.SerializeJson(ctx.Request, true));
        }

        internal async Task AuthenticateRequest(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            await _Authentication.AuthenticateAndAuthorize(req, CancellationToken.None).ConfigureAwait(false);

            switch (req.Authentication.Result)
            {
                case AuthenticationResultEnum.Success:
                    break;

                case AuthenticationResultEnum.NotFound:
                    ctx.Response.StatusCode = 401;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.AuthenticationFailed), true));
                    return;

                case AuthenticationResultEnum.Inactive:
                    ctx.Response.StatusCode = 401;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.Inactive), true));
                    return;

                case AuthenticationResultEnum.Invalid:
                    ctx.Response.StatusCode = 400;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest), true));
                    return;
            }

            switch (req.Authorization.Result)
            {
                case AuthorizationResultEnum.Conflict:
                    ctx.Response.StatusCode = 409;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.Conflict), true));
                    return;

                case AuthorizationResultEnum.Denied:
                    ctx.Response.StatusCode = 401;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.AuthorizationFailed), true));
                    return;

                case AuthorizationResultEnum.NotFound:
                    ctx.Response.StatusCode = 404;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.NotFound), true));
                    return;

                case AuthorizationResultEnum.Permitted:
                    break;
            }
        }

        internal async Task DefaultRoute(HttpContextBase ctx)
        {
            _Logging.Warn(_Header + "unknown verb or endpoint: " + ctx.Request.Method + " " + ctx.Request.Url.RawWithQuery);
            ctx.Response.StatusCode = 400;
            await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest), true));
        }

        internal async Task PostRoutingHandler(HttpContextBase ctx)
        {
            string msg =
                _Header
                + ctx.Request.Method.ToString() + " " + ctx.Request.Url.RawWithQuery + " "
                + ctx.Response.StatusCode + " "
                + ctx.Timestamp.TotalMs + "ms";

            if (ctx.Response.StatusCode > 299 && _Settings.Debug.Requests)
                msg += Environment.NewLine + ctx.Response.DataAsString;

            ctx.Timestamp.End = DateTime.UtcNow;
            _Logging.Debug(msg);
        }

        internal async Task ExceptionHandler(HttpContextBase @base, Exception exception)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Private-Route-Implementations

        #region Token

        private async Task TokenCreateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            AuthenticationToken token = _Authentication.GenerateToken(req.Authentication.TenantGUID.Value, req.Authentication.UserGUID.Value);
            ctx.Response.StatusCode = 200;
            await ctx.Response.Send(_Serializer.SerializeJson(token, true));
            return;
        }

        private async Task TokenDetailsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(req.Authentication.SecurityToken))
            {
                _Logging.Warn(_Header + "no authentication token supplied from which to read details");
                ctx.Response.StatusCode = 400;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest, null, "No authentication token supplied."), true));
                return;
            }

            AuthenticationToken token = await _Authentication.ReadToken(req.Authentication.SecurityToken, CancellationToken.None).ConfigureAwait(false);
            ctx.Response.StatusCode = 200;
            await ctx.Response.Send(_Serializer.SerializeJson(token, true));
            return;
        }

        private async Task TokenTenantsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (String.IsNullOrEmpty(req.Authentication.Email))
            {
                _Logging.Warn(_Header + "no email supplied in headers for tenant retrieval");
                ctx.Response.StatusCode = 400;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest, null, "No email supplied in the request headers."), true));
                return;
            }

            ResponseContext resp = await _ServiceHandler.UserTenants(req);
            if (resp.Success)
            {
                ctx.Response.StatusCode = 200;
                await ctx.Response.Send(_Serializer.SerializeJson(resp.Data, true));
                return;
            }
            else
            {
                ctx.Response.StatusCode = 404;
                await ctx.Response.Send(_Serializer.SerializeJson(resp.Error, true));
            }
        }

        #endregion

        #region Admin

        private async Task BackupRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.BackupRequest = _Serializer.DeserializeJson<BackupRequest>(ctx.Request.DataAsString);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.BackupExecute);
        }

        private async Task BackupReadAllRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }

            await WrappedRequestHandler(ctx, req, _ServiceHandler.BackupReadAll);
        }

        private async Task BackupReadRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }

            await WrappedRequestHandler(ctx, req, _ServiceHandler.BackupRead);
        }

        private async Task BackupExistsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }

            await WrappedRequestHandler(ctx, req, _ServiceHandler.BackupExists);
        }

        private async Task BackupDeleteRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }

            await WrappedRequestHandler(ctx, req, _ServiceHandler.BackupDelete);
        }

        private async Task FlushRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }

            await WrappedRequestHandler(ctx, req, _ServiceHandler.FlushDatabase);
        }

        #endregion

        #region General

        private async Task ExceptionRoute(HttpContextBase ctx, Exception e)
        {
            if (_Settings.Debug.Exceptions) _Logging.Warn(_Header + "exception of type " + e.GetType() + ": " + e.ToString());

            if (e is InvalidOperationException)
            {
                ctx.Response.StatusCode = 409;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.Conflict, null, e.Message), true));
            }
            else if (
                e is ArgumentNullException
                || e is ArgumentOutOfRangeException
                || e is ArgumentException
                || e is FormatException
                || e is JsonException)
            {
                ctx.Response.StatusCode = 400;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest, null, e.Message), true));
            }
            else
            {
                ctx.Response.StatusCode = 500;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.InternalError, null, e.Message), true));
                _Logging.Warn(_Header + "exception encountered for " + ctx.Request.Method.ToString() + " " + ctx.Request.Url.RawWithQuery + Environment.NewLine + e.ToString());
            }
        }

        private async Task LoopbackRoute(HttpContextBase ctx)
        {
            ctx.Response.StatusCode = 200;
            await ctx.Response.Send();
        }

        private async Task RootRoute(HttpContextBase ctx)
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = Constants.HtmlContentType;
            await ctx.Response.Send(Constants.DefaultHomepage);
        }

        private async Task FaviconRoute(HttpContextBase ctx)
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = Constants.FaviconContentType;
            await ctx.Response.Send(File.ReadAllBytes(Constants.FaviconFile));
        }

        private async Task NoRequestBody(HttpContextBase ctx)
        {
            ctx.Response.StatusCode = 400;
            await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest), true));
        }

        private async Task NotAdmin(HttpContextBase ctx)
        {
            ctx.Response.StatusCode = 401;
            await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.AuthorizationFailed), true));
        }

        #endregion

        #region Tenant-Routes

        private async Task TenantCreateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.Tenant = _Serializer.DeserializeJson<TenantMetadata>(ctx.Request.DataAsString);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TenantCreate);
        }

        private async Task TenantReadManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TenantReadMany);
        }

        private async Task TenantEnumerateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }

            if (req.Data != null) req.EnumerationQuery = _Serializer.DeserializeJson<EnumerationRequest>(Encoding.UTF8.GetString(req.Data));
            else req.EnumerationQuery = BuildEnumerationQuery(req);

            await WrappedRequestHandler(ctx, req, _ServiceHandler.TenantEnumerate);
        }

        private async Task TenantReadRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TenantRead);
        }

        private async Task TenantStatisticsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin && req.TenantGUID == null)
            {
                await NotAdmin(ctx);
            }
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TenantStatistics);
        }

        private async Task TenantExistsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TenantExists);
        }

        private async Task TenantUpdateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.Tenant = _Serializer.DeserializeJson<TenantMetadata>(ctx.Request.DataAsString);
            req.Tenant.GUID = req.TenantGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TenantUpdate);
        }

        private async Task TenantDeleteRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TenantDelete);
        }

        #endregion

        #region User-Routes

        private async Task UserCreateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.User = _Serializer.DeserializeJson<UserMaster>(ctx.Request.DataAsString);
            req.User.TenantGUID = req.TenantGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.UserCreate);
        }

        private async Task UserReadManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }
            await WrappedRequestHandler(ctx, req, _ServiceHandler.UserReadMany);
        }

        private async Task UserEnumerateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }

            if (req.Data != null) req.EnumerationQuery = _Serializer.DeserializeJson<EnumerationRequest>(Encoding.UTF8.GetString(req.Data));
            else req.EnumerationQuery = BuildEnumerationQuery(req);

            await WrappedRequestHandler(ctx, req, _ServiceHandler.UserEnumerate);
        }

        private async Task UserReadRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }
            await WrappedRequestHandler(ctx, req, _ServiceHandler.UserRead);
        }

        private async Task UserExistsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }
            await WrappedRequestHandler(ctx, req, _ServiceHandler.UserExists);
        }

        private async Task UserUpdateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.User = _Serializer.DeserializeJson<UserMaster>(ctx.Request.DataAsString);
            req.User.TenantGUID = req.TenantGUID.Value;
            req.User.GUID = req.UserGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.UserUpdate);
        }

        private async Task UserDeleteRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }
            await WrappedRequestHandler(ctx, req, _ServiceHandler.UserDelete);
        }

        #endregion

        #region Credential-Routes

        private async Task CredentialCreateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.Credential = _Serializer.DeserializeJson<Credential>(ctx.Request.DataAsString);
            req.Credential.TenantGUID = req.TenantGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.CredentialCreate);
        }

        private async Task CredentialReadManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }
            await WrappedRequestHandler(ctx, req, _ServiceHandler.CredentialReadMany);
        }

        private async Task CredentialEnumerateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }

            if (req.Data != null) req.EnumerationQuery = _Serializer.DeserializeJson<EnumerationRequest>(Encoding.UTF8.GetString(req.Data));
            else req.EnumerationQuery = BuildEnumerationQuery(req);

            await WrappedRequestHandler(ctx, req, _ServiceHandler.CredentialEnumerate);
        }

        private async Task CredentialReadRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }
            await WrappedRequestHandler(ctx, req, _ServiceHandler.CredentialRead);
        }

        private async Task CredentialExistsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }
            await WrappedRequestHandler(ctx, req, _ServiceHandler.CredentialExists);
        }

        private async Task CredentialUpdateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.Credential = _Serializer.DeserializeJson<Credential>(ctx.Request.DataAsString);
            req.Credential.TenantGUID = req.TenantGUID.Value;
            req.Credential.GUID = req.CredentialGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.CredentialUpdate);
        }

        private async Task CredentialDeleteRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }
            await WrappedRequestHandler(ctx, req, _ServiceHandler.CredentialDelete);
        }

        private async Task CredentialReadByBearerTokenRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }
            await WrappedRequestHandler(ctx, req, _ServiceHandler.CredentialReadByBearerToken);
        }

        private async Task CredentialDeleteAllInTenantRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }
            await WrappedRequestHandler(ctx, req, _ServiceHandler.CredentialDeleteAllInTenant);
        }

        private async Task CredentialDeleteByUserRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }
            await WrappedRequestHandler(ctx, req, _ServiceHandler.CredentialDeleteByUser);
        }

        #endregion

        #region Label-Routes

        private async Task LabelCreateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.Label = _Serializer.DeserializeJson<LabelMetadata>(ctx.Request.DataAsString);
            req.Label.TenantGUID = req.TenantGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.LabelCreate);
        }

        private async Task LabelCreateManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.Labels = _Serializer.DeserializeJson<List<LabelMetadata>>(ctx.Request.DataAsString);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.LabelCreateMany);
        }

        private async Task LabelReadManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.LabelReadMany);
        }

        private async Task LabelEnumerateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (req.Data != null) req.EnumerationQuery = _Serializer.DeserializeJson<EnumerationRequest>(Encoding.UTF8.GetString(req.Data));
            else req.EnumerationQuery = BuildEnumerationQuery(req);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.LabelEnumerate);
        }

        private async Task LabelReadRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.LabelRead);
        }

        private async Task LabelExistsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.LabelExists);
        }

        private async Task LabelUpdateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.Label = _Serializer.DeserializeJson<LabelMetadata>(ctx.Request.DataAsString);
            req.Label.TenantGUID = req.TenantGUID.Value;
            req.Label.GUID = req.LabelGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.LabelUpdate);
        }

        private async Task LabelDeleteRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.LabelDelete);
        }

        private async Task LabelDeleteManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.GUIDs = _Serializer.DeserializeJson<List<Guid>>(ctx.Request.DataAsString);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.LabelDeleteMany);
        }

        private async Task LabelReadAllInTenantRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.LabelReadAllInTenant);
        }

        private async Task LabelReadAllInGraphRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.LabelReadAllInGraph);
        }

        private async Task LabelReadManyGraphRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.LabelReadManyGraph);
        }

        private async Task LabelReadManyNodeRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.LabelReadManyNode);
        }

        private async Task LabelReadManyEdgeRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.LabelReadManyEdge);
        }

        private async Task LabelDeleteAllInTenantRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.LabelDeleteAllInTenant);
        }

        private async Task LabelDeleteAllInGraphRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.LabelDeleteAllInGraph);
        }

        private async Task LabelDeleteGraphLabelsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.LabelDeleteGraphLabels);
        }

        private async Task LabelDeleteNodeLabelsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.LabelDeleteNodeLabels);
        }

        private async Task LabelDeleteEdgeLabelsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.LabelDeleteEdgeLabels);
        }

        #endregion

        #region Tag-Routes

        private async Task TagCreateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.Tag = _Serializer.DeserializeJson<TagMetadata>(ctx.Request.DataAsString);
            req.Tag.TenantGUID = req.TenantGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TagCreate);
        }

        private async Task TagCreateManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.Tags = _Serializer.DeserializeJson<List<TagMetadata>>(ctx.Request.DataAsString);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TagCreateMany);
        }

        private async Task TagReadManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TagReadMany);
        }

        private async Task TagEnumerateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (req.Data != null) req.EnumerationQuery = _Serializer.DeserializeJson<EnumerationRequest>(Encoding.UTF8.GetString(req.Data));
            else req.EnumerationQuery = BuildEnumerationQuery(req);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TagEnumerate);
        }

        private async Task TagReadRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TagRead);
        }

        private async Task TagExistsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TagExists);
        }

        private async Task TagUpdateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.Tag = _Serializer.DeserializeJson<TagMetadata>(ctx.Request.DataAsString);
            req.Tag.TenantGUID = req.TenantGUID.Value;
            req.Tag.GUID = req.TagGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TagUpdate);
        }

        private async Task TagDeleteRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TagDelete);
        }

        private async Task TagDeleteManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.GUIDs = _Serializer.DeserializeJson<List<Guid>>(ctx.Request.DataAsString);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TagDeleteMany);
        }

        private async Task TagReadAllInTenantRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TagReadAllInTenant);
        }

        private async Task TagReadAllInGraphRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TagReadAllInGraph);
        }

        private async Task TagReadManyGraphRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TagReadManyGraph);
        }

        private async Task TagReadManyNodeRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TagReadManyNode);
        }

        private async Task TagReadManyEdgeRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TagReadManyEdge);
        }

        private async Task TagDeleteAllInTenantRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TagDeleteAllInTenant);
        }

        private async Task TagDeleteAllInGraphRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TagDeleteAllInGraph);
        }

        private async Task TagDeleteGraphTagsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TagDeleteGraphTags);
        }

        private async Task TagDeleteNodeTagsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TagDeleteNodeTags);
        }

        private async Task TagDeleteEdgeTagsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TagDeleteEdgeTags);
        }

        #endregion

        #region Vector-Routes

        private async Task VectorCreateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.Vector = _Serializer.DeserializeJson<VectorMetadata>(ctx.Request.DataAsString);
            req.Vector.TenantGUID = req.TenantGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.VectorCreate);
        }

        private async Task VectorCreateManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.Vectors = _Serializer.DeserializeJson<List<VectorMetadata>>(ctx.Request.DataAsString);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.VectorCreateMany);
        }

        private async Task VectorReadManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.VectorReadMany);
        }

        private async Task VectorEnumerateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (req.Data != null) req.EnumerationQuery = _Serializer.DeserializeJson<EnumerationRequest>(Encoding.UTF8.GetString(req.Data));
            else req.EnumerationQuery = BuildEnumerationQuery(req);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.VectorEnumerate);
        }

        private async Task VectorReadRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.VectorRead);
        }

        private async Task VectorExistsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.VectorExists);
        }

        private async Task VectorUpdateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.Vector = _Serializer.DeserializeJson<VectorMetadata>(ctx.Request.DataAsString);
            req.Vector.TenantGUID = req.TenantGUID.Value;
            req.Vector.GUID = req.VectorGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.VectorUpdate);
        }

        private async Task VectorDeleteRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.VectorDelete);
        }

        private async Task VectorDeleteManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.GUIDs = _Serializer.DeserializeJson<List<Guid>>(ctx.Request.DataAsString);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.VectorDeleteMany);
        }

        private async Task VectorReadAllInTenantRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.VectorReadAllInTenant);
        }

        private async Task VectorReadAllInGraphRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.VectorReadAllInGraph);
        }

        private async Task VectorReadManyGraphRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.VectorReadManyGraph);
        }

        private async Task VectorReadManyNodeRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.VectorReadManyNode);
        }

        private async Task VectorReadManyEdgeRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.VectorReadManyEdge);
        }

        private async Task VectorDeleteAllInTenantRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.VectorDeleteAllInTenant);
        }

        private async Task VectorDeleteAllInGraphRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.VectorDeleteAllInGraph);
        }

        private async Task VectorDeleteGraphVectorsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.VectorDeleteGraphVectors);
        }

        private async Task VectorDeleteNodeVectorsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.VectorDeleteNodeVectors);
        }

        private async Task VectorDeleteEdgeVectorsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.VectorDeleteEdgeVectors);
        }

        private async Task VectorSearchRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.VectorSearchRequest = _Serializer.DeserializeJson<VectorSearchRequest>(ctx.Request.DataAsString);
            req.VectorSearchRequest.TenantGUID = req.TenantGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.VectorSearch);
        }

        #endregion

        #region Graph-Routes

        private async Task GraphCreateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.Graph = _Serializer.DeserializeJson<Graph>(ctx.Request.DataAsString);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.GraphCreate);
        }

        private async Task GraphReadManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.GraphReadMany);
        }

        private async Task GraphEnumerateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (req.Data != null) req.EnumerationQuery = _Serializer.DeserializeJson<EnumerationRequest>(Encoding.UTF8.GetString(req.Data));
            else req.EnumerationQuery = BuildEnumerationQuery(req);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.GraphEnumerate);
        }

        private async Task GraphExistenceRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.ExistenceRequest = _Serializer.DeserializeJson<ExistenceRequest>(ctx.Request.DataAsString);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.GraphExistence);
        }

        private async Task GraphSubgraphRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.GraphSubgraph);
        }

        private async Task GraphSubgraphStatisticsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.GraphSubgraphStatistics);
        }

        private async Task GraphReadFirstRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await GraphReadManyRoute(ctx);
                return;
            }

            req.SearchRequest = _Serializer.DeserializeJson<SearchRequest>(ctx.Request.DataAsString);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.GraphReadFirst);
        }

        private async Task GraphSearchRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await GraphReadManyRoute(ctx);
                return;
            }

            req.SearchRequest = _Serializer.DeserializeJson<SearchRequest>(ctx.Request.DataAsString);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.GraphSearch);
        }

        private async Task GraphReadRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.GraphRead);
        }

        private async Task GraphStatisticsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin && req.TenantGUID == null)
            {
                await NotAdmin(ctx);
            }
            await WrappedRequestHandler(ctx, req, _ServiceHandler.GraphStatistics);
        }

        private async Task GraphExistsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.GraphExists);
        }

        private async Task GraphUpdateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.Graph = _Serializer.DeserializeJson<Graph>(ctx.Request.DataAsString);
            req.Graph.TenantGUID = req.TenantGUID.Value;
            req.Graph.GUID = req.GraphGUID.Value;

            // Get current graph to preserve vector index properties
            Graph currentGraph = await _LiteGraph.Graph.ReadByGuid(req.TenantGUID.Value, req.GraphGUID.Value, token: CancellationToken.None).ConfigureAwait(false);
            if (currentGraph != null)
            {
                // Preserve existing vector index properties - these should only be changed through vector index APIs
                req.Graph.VectorIndexType = currentGraph.VectorIndexType;
                req.Graph.VectorIndexFile = currentGraph.VectorIndexFile;
                req.Graph.VectorIndexThreshold = currentGraph.VectorIndexThreshold;
                req.Graph.VectorDimensionality = currentGraph.VectorDimensionality;
                req.Graph.VectorIndexM = currentGraph.VectorIndexM;
                req.Graph.VectorIndexEf = currentGraph.VectorIndexEf;
                req.Graph.VectorIndexEfConstruction = currentGraph.VectorIndexEfConstruction;
            }

            await WrappedRequestHandler(ctx, req, _ServiceHandler.GraphUpdate);
        }

        private async Task GraphDeleteRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.GraphDelete);
        }

        private async Task GraphReadAllInTenantRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.GraphReadAllInTenant);
        }

        private async Task GraphDeleteAllInTenantRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.GraphDeleteAllInTenant);
        }

        private async Task GraphGexfExportRoute(HttpContextBase ctx)
        {
            try
            {
                RequestContext req = (RequestContext)ctx.Metadata;
                ResponseContext resp = await _ServiceHandler.GraphGexfExport(req);
                if (!resp.Success)
                {
                    ctx.Response.StatusCode = 500;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.InternalError), true));
                }
                else
                {
                    ctx.Response.ContentType = Constants.XmlContentType;
                    ctx.Response.StatusCode = 200;
                    await ctx.Response.Send(resp.Data.ToString());
                }
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "GEXF export error:" + Environment.NewLine + e.ToString());
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = Constants.JsonContentType;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.InternalError, null, e.Message), true));
            }
        }

        private async Task GraphEnableVectorIndexRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            try
            {
                // Parse request body for index configuration
                string requestData = ctx.Request.DataAsString;
                VectorIndexConfiguration config;

                if (string.IsNullOrWhiteSpace(requestData))
                {
                    // Use default configuration if no body provided
                    config = new VectorIndexConfiguration();
                }
                else
                {
                    config = _Serializer.DeserializeJson<VectorIndexConfiguration>(requestData);
                    if (config == null)
                    {
                        ctx.Response.StatusCode = 400;
                        ctx.Response.ContentType = Constants.JsonContentType;
                        await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest, null, "Invalid configuration format"), true));
                        return;
                    }
                }

                // Validate configuration
                if (!config.IsValid(out string errorMessage))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = Constants.JsonContentType;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest, null, errorMessage), true));
                    return;
                }

                await _LiteGraph.Graph.EnableVectorIndexing(
                    req.TenantGUID.Value,
                    req.GraphGUID.Value,
                    config,
                    CancellationToken.None).ConfigureAwait(false);

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = Constants.JsonContentType;
                await ctx.Response.Send(_Serializer.SerializeJson(config, true));
            }
            catch (Exception e)
            {
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = Constants.JsonContentType;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.InternalError, null, e.Message), true));
            }
        }

        private async Task GraphDisableVectorIndexRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            bool deleteFile = false;
            if (!string.IsNullOrEmpty(req.Query["deleteFile"]))
            {
                bool.TryParse(req.Query["deleteFile"], out deleteFile);
            }

            await _LiteGraph.Graph.DisableVectorIndexing(
                req.TenantGUID.Value,
                req.GraphGUID.Value,
                deleteFile,
                CancellationToken.None).ConfigureAwait(false);

            ctx.Response.StatusCode = 200;
            await ctx.Response.Send();
        }

        private async Task GraphRebuildVectorIndexRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            await _LiteGraph.Graph.RebuildVectorIndex(
                req.TenantGUID.Value,
                req.GraphGUID.Value,
                CancellationToken.None).ConfigureAwait(false);

            ctx.Response.StatusCode = 200;
            await ctx.Response.Send();
        }

        private async Task GraphVectorIndexStatsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            VectorIndexStatistics stats = await _LiteGraph.Graph.GetVectorIndexStatistics(
                req.TenantGUID.Value,
                req.GraphGUID.Value,
                CancellationToken.None).ConfigureAwait(false);

            if (stats == null)
            {
                ctx.Response.StatusCode = 404;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.NotFound, null, "The specified graph has no configured vector index."), true));
                return;
            }

            ctx.Response.StatusCode = 200;
            await ctx.Response.Send(_Serializer.SerializeJson(stats, true));
        }

        private async Task GraphGetVectorIndexConfigRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            try
            {
                Graph graph = await _LiteGraph.Graph.ReadByGuid(req.TenantGUID.Value, req.GraphGUID.Value, token: CancellationToken.None).ConfigureAwait(false);
                if (graph == null)
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = Constants.JsonContentType;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.NotFound, null, "Graph not found"), true));
                    return;
                }

                // Check if vector indexing is enabled
                if (!graph.VectorIndexType.HasValue || graph.VectorIndexType == VectorIndexTypeEnum.None)
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = Constants.JsonContentType;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.NotFound, null, "The specified graph has no configured vector index."), true));
                    return;
                }

                VectorIndexConfiguration config = new VectorIndexConfiguration(graph);

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = Constants.JsonContentType;
                await ctx.Response.Send(_Serializer.SerializeJson(config, true));
            }
            catch (Exception e)
            {
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = Constants.JsonContentType;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.InternalError, null, e.Message), true));
            }
        }


        #endregion

        #region Node-Routes

        private async Task NodeCreateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.Node = _Serializer.DeserializeJson<Node>(ctx.Request.DataAsString);
            req.Node.TenantGUID = req.TenantGUID.Value;
            req.Node.GraphGUID = req.GraphGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.NodeCreate);
        }

        private async Task NodeCreateManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.Nodes = _Serializer.DeserializeJson<List<Node>>(ctx.Request.DataAsString);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.NodeCreateMany);
        }

        private async Task NodeReadManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.NodeReadMany);
        }

        private async Task NodeEnumerateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (req.Data != null) req.EnumerationQuery = _Serializer.DeserializeJson<EnumerationRequest>(Encoding.UTF8.GetString(req.Data));
            else req.EnumerationQuery = BuildEnumerationQuery(req);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.NodeEnumerate);
        }

        private async Task NodeSearchRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NodeReadManyRoute(ctx);
                return;
            }

            req.SearchRequest = _Serializer.DeserializeJson<SearchRequest>(ctx.Request.DataAsString);
            req.SearchRequest.TenantGUID = req.TenantGUID.Value;
            req.SearchRequest.GraphGUID = req.GraphGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.NodeSearch);
        }

        private async Task NodeReadFirstRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NodeReadManyRoute(ctx);
                return;
            }

            req.SearchRequest = _Serializer.DeserializeJson<SearchRequest>(ctx.Request.DataAsString);
            req.SearchRequest.TenantGUID = req.TenantGUID.Value;
            req.SearchRequest.GraphGUID = req.GraphGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.NodeReadFirst);
        }

        private async Task NodeReadRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.NodeRead);
        }

        private async Task NodeExistsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.NodeExists);
        }

        private async Task NodeUpdateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.Node = _Serializer.DeserializeJson<Node>(ctx.Request.DataAsString);
            req.Node.TenantGUID = req.TenantGUID.Value;
            req.Node.GraphGUID = req.GraphGUID.Value;
            req.Node.GUID = req.NodeGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.NodeUpdate);
        }

        private async Task NodeDeleteRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.NodeDelete);
        }

        private async Task NodeDeleteAllRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.NodeDeleteAll);
        }

        private async Task NodeDeleteManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.GUIDs = _Serializer.DeserializeJson<List<Guid>>(ctx.Request.DataAsString);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.NodeDeleteMany);
        }

        private async Task NodeReadAllInTenantRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.NodeReadAllInTenant);
        }

        private async Task NodeReadAllInGraphRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.NodeReadAllInGraph);
        }

        private async Task NodeReadMostConnectedRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.NodeReadMostConnected);
        }

        private async Task NodeReadLeastConnectedRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.NodeReadLeastConnected);
        }

        private async Task NodeDeleteAllInTenantRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.NodeDeleteAllInTenant);
        }

        #endregion

        #region Edge-Routes

        private async Task EdgeCreateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.Edge = _Serializer.DeserializeJson<Edge>(ctx.Request.DataAsString);
            req.Edge.TenantGUID = req.TenantGUID.Value;
            req.Edge.GraphGUID = req.GraphGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.EdgeCreate);
        }

        private async Task EdgeCreateManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.Edges = _Serializer.DeserializeJson<List<Edge>>(ctx.Request.DataAsString);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.EdgeCreateMany);
        }

        private async Task EdgeReadManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.EdgeReadMany);
        }

        private async Task EdgeEnumerateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (req.Data != null) req.EnumerationQuery = _Serializer.DeserializeJson<EnumerationRequest>(Encoding.UTF8.GetString(req.Data));
            else req.EnumerationQuery = BuildEnumerationQuery(req);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.EdgeEnumerate);
        }

        private async Task EdgesBetweenRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.EdgesBetween);
        }

        private async Task EdgeSearchRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await EdgeReadManyRoute(ctx);
                return;
            }

            req.SearchRequest = _Serializer.DeserializeJson<SearchRequest>(ctx.Request.DataAsString);
            req.SearchRequest.TenantGUID = req.TenantGUID.Value;
            req.SearchRequest.GraphGUID = req.GraphGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.EdgeSearch);
        }

        private async Task EdgeReadFirstRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await EdgeReadManyRoute(ctx);
                return;
            }

            req.SearchRequest = _Serializer.DeserializeJson<SearchRequest>(ctx.Request.DataAsString);
            req.SearchRequest.TenantGUID = req.TenantGUID.Value;
            req.SearchRequest.GraphGUID = req.GraphGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.EdgeReadFirst);
        }

        private async Task EdgeReadRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.EdgeRead);
        }

        private async Task EdgeExistsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.EdgeExists);
        }

        private async Task EdgeUpdateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.Edge = _Serializer.DeserializeJson<Edge>(ctx.Request.DataAsString);
            req.Edge.TenantGUID = req.TenantGUID.Value;
            req.Edge.GraphGUID = req.GraphGUID.Value;
            req.Edge.GUID = req.EdgeGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.EdgeUpdate);
        }

        private async Task EdgeDeleteRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.EdgeDelete);
        }

        private async Task EdgeDeleteAllRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.EdgeDeleteAll);
        }

        private async Task EdgeDeleteManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.GUIDs = _Serializer.DeserializeJson<List<Guid>>(ctx.Request.DataAsString);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.EdgeDeleteMany);
        }

        private async Task EdgeReadAllInTenantRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.EdgeReadAllInTenant);
        }

        private async Task EdgeReadAllInGraphRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.EdgeReadAllInGraph);
        }

        private async Task EdgeDeleteAllInTenantRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.EdgeDeleteAllInTenant);
        }

        private async Task EdgeDeleteNodeEdgesRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.EdgeDeleteNodeEdges);
        }

        private async Task EdgeDeleteNodeEdgesManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.GUIDs = _Serializer.DeserializeJson<List<Guid>>(ctx.Request.DataAsString);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.EdgeDeleteNodeEdgesMany);
        }

        #endregion

        #region Routes-and-Traversal

        private async Task EdgesFromNodeRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.EdgesFromNode);
        }

        private async Task EdgesToNodeRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.EdgesToNode);
        }

        private async Task AllEdgesToNodeRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.AllEdgesToNode);
        }

        private async Task NodeChildrenRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.NodeChildren);
        }

        private async Task NodeParentsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.NodeParents);
        }

        private async Task NodeNeighborsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.NodeNeighbors);
        }

        private async Task GetRoutesRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.RouteRequest = _Serializer.DeserializeJson<RouteRequest>(ctx.Request.DataAsString);
            req.RouteRequest.TenantGUID = req.TenantGUID.Value;
            req.RouteRequest.GraphGUID = req.GraphGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.GetRoutes);
        }

        #endregion

        #endregion

        #region Private-Methods

        private async Task WrappedRequestHandler(HttpContextBase ctx, RequestContext req, Func<RequestContext, CancellationToken, Task<ResponseContext>> func)
        {
            try
            {
                ResponseContext resp = await func(req, CancellationToken.None).ConfigureAwait(false);
                if (resp == null)
                {
                    _Logging.Warn(_Header + "no response from agnostic handler");
                    ctx.Response.StatusCode = 500;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.InternalError), true));
                    return;
                }
                else if (resp.Success)
                {
                    ctx.Response.StatusCode = resp.StatusCode;
                    if (resp.Data != null) await ctx.Response.Send(_Serializer.SerializeJson(resp.Data, true));
                    else await ctx.Response.Send();
                    return;
                }
                else
                {
                    if (resp.Error != null) ctx.Response.StatusCode = resp.Error.StatusCode;
                    else ctx.Response.StatusCode = 418;
                    if (resp.Error != null && ctx.Request.Method != HttpMethod.HEAD) await ctx.Response.Send(_Serializer.SerializeJson(resp.Error, true));
                    else await ctx.Response.Send();
                    return;
                }
            }
            catch (JsonException je)
            {
                _Logging.Warn(_Header + "JSON exception: " + Environment.NewLine + je.ToString());
                ctx.Response.StatusCode = 400;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.DeserializationError, null, je.Message), true));
            }
            catch (FormatException fe)
            {
                _Logging.Warn(_Header + "format exception: " + Environment.NewLine + fe.ToString());
                ctx.Response.StatusCode = 400;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest, null, fe.Message), true));
            }
            catch (InvalidOperationException ioe)
            {
                _Logging.Warn(_Header + "invalid operation exception: " + Environment.NewLine + ioe.ToString());
                ctx.Response.StatusCode = 409;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.Conflict, null, ioe.Message), true));
            }
            catch (FileNotFoundException fnfe)
            {
                _Logging.Warn(_Header + "file not found exception: " + Environment.NewLine + fnfe.ToString());
                ctx.Response.StatusCode = 404;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.NotFound, null, fnfe.Message), true));
                return;
            }
            catch (KeyNotFoundException knfe)
            {
                _Logging.Warn(_Header + "invalid operation exception: " + Environment.NewLine + knfe.ToString());
                ctx.Response.StatusCode = 404;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.NotFound, null, knfe.Message), true));
                return;
            }
            catch (ArgumentException ae)
            {
                _Logging.Warn(_Header + "argument exception: " + Environment.NewLine + ae.ToString());
                ctx.Response.StatusCode = 400;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest, null, ae.Message), true));
                return;
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "exception: " + Environment.NewLine + e.ToString());
                ctx.Response.StatusCode = 400;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest, null, e.Message), true));
                return;
            }
        }

        private EnumerationRequest BuildEnumerationQuery(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            return new EnumerationRequest
            {
                TenantGUID = req.TenantGUID,
                GraphGUID = req.GraphGUID,
                MaxResults = req.MaxKeys,
                Skip = req.Skip,
                IncludeData = req.IncludeData,
                IncludeSubordinates = req.IncludeSubordinates,
                ContinuationToken = (!String.IsNullOrEmpty(req.ContinuationToken) ? Guid.Parse(req.ContinuationToken) : null)
            };
        }

        #endregion

    }
}
