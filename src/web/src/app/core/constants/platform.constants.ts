export const PLATFORM_DEFAULT_COUNTRY_CODE = 'CL';

export const PLATFORM_INVITATION_ROLES = ['admin', 'issuer'] as const;

export type PlatformInvitationRole = (typeof PLATFORM_INVITATION_ROLES)[number];
