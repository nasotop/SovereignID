using Npgsql;

namespace Verifier.IntegrationTests;

/// <summary>
/// Identificadores fijos y siembra determinista de las filas mínimas para ejercitar
/// la verificación: una institución, un tipo, un estudiante + wallet (FKs) y una credencial
/// por escenario (válida, revocada, expirada-por-fecha, revocada+expirada).
/// </summary>
internal static class SeedData
{
    /// <summary>Instante de referencia para los tests; el <see cref="ControllableTimeProvider"/> se fija aquí.</summary>
    public static readonly DateTimeOffset Now = new(2026, 06, 25, 12, 00, 00, TimeSpan.Zero);

    public static readonly Guid InstitutionId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid StudentId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid WalletId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public static readonly Guid ValidCredentialId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid RevokedCredentialId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public static readonly Guid ExpiredCredentialId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    public static readonly Guid RevokedAndExpiredCredentialId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    /// <summary>UUID válido que no corresponde a ninguna fila (escenario not_found).</summary>
    public static readonly Guid MissingCredentialId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

    public const string IssuerDid = "did:ethr:sepolia:0xissuer";
    public const string IssuerDisplayName = "Universidad de Ejemplo";
    public const string IssuerCode = "UDE";

    public static async Task SeedAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = Sql;
        await command.ExecuteNonQueryAsync();
    }

    private const string Sql = """
        INSERT INTO institutions (id, code, legal_name, display_name, did, issuer_wallet_address, public_key)
        VALUES ('11111111-1111-1111-1111-111111111111', 'UDE', 'Universidad de Ejemplo S.A.', 'Universidad de Ejemplo',
                'did:ethr:sepolia:0xissuer', '0x1111111111111111111111111111111111111111', '0xpublickey');

        INSERT INTO credential_types (id, code, name, jsonld_context_url, allows_expiration)
        VALUES (1, 'TITULO', 'Título profesional', 'https://schemas.sovereignid/titulo/v1', true);

        INSERT INTO students (id, institution_id, external_reference, enrollment_year)
        VALUES ('22222222-2222-2222-2222-222222222222', '11111111-1111-1111-1111-111111111111', 'EXT-001', 2020);

        INSERT INTO student_wallets (id, student_id, wallet_address, did)
        VALUES ('33333333-3333-3333-3333-333333333333', '22222222-2222-2222-2222-222222222222',
                '0x2222222222222222222222222222222222222222', 'did:ethr:sepolia:0xsubject');

        -- Válida: activa, sin expiración, no revocada
        INSERT INTO credentials (id, institution_id, credential_type_id, student_id, issued_to_wallet_id,
            subject_did, issuer_did, ipfs_cid, ipfs_gateway_url, content_hash, transaction_hash, block_number,
            eip712_signature, status, issued_at, expires_at, revoked_at)
        VALUES ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '11111111-1111-1111-1111-111111111111', 1,
            '22222222-2222-2222-2222-222222222222', '33333333-3333-3333-3333-333333333333',
            'did:ethr:sepolia:0xsubject', 'did:ethr:sepolia:0xissuer', 'bafyvalid',
            'https://ipfs.example/bafyvalid', '0xaaaa000000000000000000000000000000000000000000000000000000000001',
            '0xaaaa000000000000000000000000000000000000000000000000000000000002', 100,
            '0xsig_valid', 'active', TIMESTAMP '2025-01-01 00:00:00', NULL, NULL);

        -- Revocada: status revoked + revoked_at
        INSERT INTO credentials (id, institution_id, credential_type_id, student_id, issued_to_wallet_id,
            subject_did, issuer_did, ipfs_cid, ipfs_gateway_url, content_hash, transaction_hash, block_number,
            eip712_signature, status, issued_at, expires_at, revoked_at)
        VALUES ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', '11111111-1111-1111-1111-111111111111', 1,
            '22222222-2222-2222-2222-222222222222', '33333333-3333-3333-3333-333333333333',
            'did:ethr:sepolia:0xsubject', 'did:ethr:sepolia:0xissuer', 'bafyrevoked',
            'https://ipfs.example/bafyrevoked', '0xbbbb000000000000000000000000000000000000000000000000000000000001',
            '0xbbbb000000000000000000000000000000000000000000000000000000000002', 101,
            '0xsig_revoked', 'revoked', TIMESTAMP '2025-01-01 00:00:00', NULL, TIMESTAMP '2026-06-01 00:00:00');

        -- Expirada por fecha: status active pero expires_at en el pasado
        INSERT INTO credentials (id, institution_id, credential_type_id, student_id, issued_to_wallet_id,
            subject_did, issuer_did, ipfs_cid, ipfs_gateway_url, content_hash, transaction_hash, block_number,
            eip712_signature, status, issued_at, expires_at, revoked_at)
        VALUES ('cccccccc-cccc-cccc-cccc-cccccccccccc', '11111111-1111-1111-1111-111111111111', 1,
            '22222222-2222-2222-2222-222222222222', '33333333-3333-3333-3333-333333333333',
            'did:ethr:sepolia:0xsubject', 'did:ethr:sepolia:0xissuer', 'bafyexpired',
            'https://ipfs.example/bafyexpired', '0xcccc000000000000000000000000000000000000000000000000000000000001',
            '0xcccc000000000000000000000000000000000000000000000000000000000002', 102,
            '0xsig_expired', 'active', TIMESTAMP '2024-01-01 00:00:00', TIMESTAMP '2026-01-01 00:00:00', NULL);

        -- Revocada y expirada simultáneamente (precedencia → revoked)
        INSERT INTO credentials (id, institution_id, credential_type_id, student_id, issued_to_wallet_id,
            subject_did, issuer_did, ipfs_cid, ipfs_gateway_url, content_hash, transaction_hash, block_number,
            eip712_signature, status, issued_at, expires_at, revoked_at)
        VALUES ('dddddddd-dddd-dddd-dddd-dddddddddddd', '11111111-1111-1111-1111-111111111111', 1,
            '22222222-2222-2222-2222-222222222222', '33333333-3333-3333-3333-333333333333',
            'did:ethr:sepolia:0xsubject', 'did:ethr:sepolia:0xissuer', 'bafyboth',
            'https://ipfs.example/bafyboth', '0xdddd000000000000000000000000000000000000000000000000000000000001',
            '0xdddd000000000000000000000000000000000000000000000000000000000002', 103,
            '0xsig_both', 'active', TIMESTAMP '2024-01-01 00:00:00', TIMESTAMP '2026-01-01 00:00:00',
            TIMESTAMP '2026-06-01 00:00:00');
        """;

    public static async Task<int> CountVerificationLogsAsync(string connectionString, string credentialIdQuery)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM verification_logs WHERE credential_id_query = @q;";
        command.Parameters.AddWithValue("q", credentialIdQuery);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }
}
