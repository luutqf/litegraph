export class AuthorizationRole {
    constructor(role?: any);
    GUID: string | null;
    TenantGUID: string | null;
    Name: string;
    DisplayName: string | null;
    Description: string | null;
    BuiltIn: boolean;
    BuiltInRole: string | null;
    ResourceScope: string;
    Permissions: string[];
    ResourceTypes: string[];
    InheritsToGraphs: boolean;
    CreatedUtc: Date | null;
    LastUpdateUtc: Date | null;
}
export class UserRoleAssignment {
    constructor(assignment?: any);
    GUID: string | null;
    TenantGUID: string | null;
    UserGUID: string | null;
    RoleGUID: string | null;
    RoleName: string | null;
    ResourceScope: string;
    GraphGUID: string | null;
    CreatedUtc: Date | null;
    LastUpdateUtc: Date | null;
}
export class CredentialScopeAssignment {
    constructor(assignment?: any);
    GUID: string | null;
    TenantGUID: string | null;
    CredentialGUID: string | null;
    RoleGUID: string | null;
    RoleName: string | null;
    ResourceScope: string;
    GraphGUID: string | null;
    Permissions: string[];
    ResourceTypes: string[];
    CreatedUtc: Date | null;
    LastUpdateUtc: Date | null;
}
export class AuthorizationEffectiveGrant {
    constructor(grant?: any);
    Source: string;
    AssignmentGUID: string | null;
    RoleGUID: string | null;
    RoleName: string | null;
    ResourceScope: string;
    GraphGUID: string | null;
    Permissions: string[];
    ResourceTypes: string[];
    InheritsToGraphs: boolean;
    AppliesToRequestedGraph: boolean;
}
export class AuthorizationEffectivePermissionsResult {
    constructor(result?: any);
    TenantGUID: string | null;
    UserGUID: string | null;
    CredentialGUID: string | null;
    GraphGUID: string | null;
    Grants: AuthorizationEffectiveGrant[];
    UserRoleAssignments: UserRoleAssignment[];
    CredentialScopeAssignments: CredentialScopeAssignment[];
    Roles: AuthorizationRole[];
}
export class AuthorizationRoleSearchResult {
    constructor(result?: any);
    Objects: AuthorizationRole[];
    Page: number;
    PageSize: number;
    TotalCount: number;
    TotalPages: number;
}
export class UserRoleAssignmentSearchResult {
    constructor(result?: any);
    Objects: UserRoleAssignment[];
    Page: number;
    PageSize: number;
    TotalCount: number;
    TotalPages: number;
}
export class CredentialScopeAssignmentSearchResult {
    constructor(result?: any);
    Objects: CredentialScopeAssignment[];
    Page: number;
    PageSize: number;
    TotalCount: number;
    TotalPages: number;
}
