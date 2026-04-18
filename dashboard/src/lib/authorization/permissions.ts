import {
  AuthorizationEffectivePermissionsResult,
  AuthorizationPermission,
  AuthorizationResourceType,
} from '@/lib/sdk/authorization';

export const hasEffectivePermission = (
  effectivePermissions: AuthorizationEffectivePermissionsResult | null | undefined,
  permission: AuthorizationPermission,
  resourceTypes: AuthorizationResourceType[]
) => {
  if (!effectivePermissions?.Grants?.length) return false;

  return effectivePermissions.Grants.some((grant) => {
    if (grant.AppliesToRequestedGraph === false) return false;
    if (!grant.Permissions?.includes(permission)) return false;
    return grant.ResourceTypes?.some((resourceType) => resourceTypes.includes(resourceType));
  });
};

export const canManageAuthorization = (
  effectivePermissions: AuthorizationEffectivePermissionsResult | null | undefined,
  hasAdminAccessKey: boolean
) => {
  if (hasAdminAccessKey) return true;
  return hasEffectivePermission(effectivePermissions, 'Admin', ['Admin']);
};
