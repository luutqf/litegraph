import { canManageAuthorization, hasEffectivePermission } from '@/lib/authorization/permissions';
import { AuthorizationEffectivePermissionsResult } from '@/lib/sdk/authorization';

const effectivePermissions: AuthorizationEffectivePermissionsResult = {
  TenantGUID: 'tenant',
  UserGUID: 'user',
  CredentialGUID: null,
  GraphGUID: null,
  Grants: [
    {
      Source: 'UserRole',
      AssignmentGUID: 'assignment',
      RoleGUID: null,
      RoleName: 'TenantAdmin',
      ResourceScope: 'Tenant',
      GraphGUID: null,
      Permissions: ['Read', 'Admin'],
      ResourceTypes: ['Admin', 'Graph'],
      InheritsToGraphs: true,
      AppliesToRequestedGraph: true,
    },
  ],
  UserRoleAssignments: [],
  CredentialScopeAssignments: [],
  Roles: [],
};

describe('authorization permission helpers', () => {
  it('matches effective grants by permission and resource type', () => {
    expect(hasEffectivePermission(effectivePermissions, 'Admin', ['Admin'])).toBe(true);
    expect(hasEffectivePermission(effectivePermissions, 'Delete', ['Admin'])).toBe(false);
  });

  it('treats admin access keys as authorization administrators', () => {
    expect(canManageAuthorization(null, true)).toBe(true);
  });

  it('requires an admin grant when no admin access key is present', () => {
    expect(canManageAuthorization(effectivePermissions, false)).toBe(true);
    expect(canManageAuthorization({ ...effectivePermissions, Grants: [] }, false)).toBe(false);
  });
});
