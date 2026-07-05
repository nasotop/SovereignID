#nullable enable
using System;

namespace Auth.Infrastructure.Persistence.Generated.Entities;

/// <summary>
/// Roles globales de plataforma. Para MVP se usa platform_admin seeded por wallet.
/// </summary>
public partial class UserGlobalRole
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public GlobalUserRole Role { get; set; }

    public DateTime GrantedAt { get; set; }

    /// <summary>
    /// Si tiene valor, el rol global esta inactivo
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
