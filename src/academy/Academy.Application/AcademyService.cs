using Microsoft.Extensions.Options;

namespace Academy.Application;

public sealed class AcademyService
{
    private readonly IAcademyRepository _repository;
    private readonly IInvitationTokenService _tokenService;
    private readonly IInstitutionInvitationEmailSender _emailSender;
    private readonly TimeProvider _timeProvider;
    private readonly AcademyOptions _options;

    public AcademyService(
        IAcademyRepository repository,
        IInvitationTokenService tokenService,
        IInstitutionInvitationEmailSender emailSender,
        TimeProvider timeProvider,
        IOptions<AcademyOptions> options)
    {
        _repository = repository;
        _tokenService = tokenService;
        _emailSender = emailSender;
        _timeProvider = timeProvider;
        _options = options.Value;
    }

    public async Task<AcademyResult<InstitutionCreated>> CreateInstitutionAsync(
        CreateInstitutionCommand command,
        CancellationToken cancellationToken)
    {
        if (IsBlank(command.Code) || IsBlank(command.LegalName) || IsBlank(command.DisplayName))
        {
            return Fail<InstitutionCreated>("invalid_institution", 400, "Institution code, legalName and displayName are required.");
        }

        if (IsBlank(command.ContactEmail) || !command.ContactEmail.Contains('@', StringComparison.Ordinal))
        {
            return Fail<InstitutionCreated>("invalid_invitation_email", 400, "A valid contactEmail is required to invite the institution.");
        }

        var code = command.Code.Trim().ToUpperInvariant();
        if (await _repository.InstitutionCodeExistsAsync(code, cancellationToken))
        {
            return Fail<InstitutionCreated>("institution_code_exists", 409, "An institution with that code already exists.");
        }

        var now = _timeProvider.GetUtcNow();
        var normalizedCommand = command with
        {
            Code = code,
            LegalName = command.LegalName.Trim(),
            DisplayName = command.DisplayName.Trim(),
            ContactEmail = command.ContactEmail.Trim().ToLowerInvariant(),
            CountryCode = NormalizeCountry(command.CountryCode),
            WebsiteUrl = BlankToNull(command.WebsiteUrl)
        };

        var institution = await _repository.CreateInstitutionAsync(normalizedCommand, now, cancellationToken);
        var invitationResult = await CreateInvitationInternalAsync(
            new CreateInstitutionInvitationCommand(institution.Id, normalizedCommand.ContactEmail, InstitutionRoles.Admin),
            now,
            cancellationToken);

        return invitationResult is AcademySuccess<InstitutionInvitationCreated> success
            ? new AcademySuccess<InstitutionCreated>(new InstitutionCreated(institution, success.Value))
            : Fail<InstitutionCreated>(
                ((AcademyFailureResult<InstitutionInvitationCreated>)invitationResult).Failure.ErrorCode,
                ((AcademyFailureResult<InstitutionInvitationCreated>)invitationResult).Failure.StatusCode,
                ((AcademyFailureResult<InstitutionInvitationCreated>)invitationResult).Failure.Detail);
    }

    public async Task<AcademyResult<IReadOnlyList<InstitutionSummary>>> ListInstitutionsAsync(
        CancellationToken cancellationToken)
    {
        var institutions = await _repository.ListInstitutionsAsync(cancellationToken);
        return new AcademySuccess<IReadOnlyList<InstitutionSummary>>(institutions);
    }

    public async Task<AcademyResult<InstitutionSummary>> GetInstitutionAsync(
        Guid institutionId,
        CancellationToken cancellationToken)
    {
        var institution = await _repository.GetInstitutionAsync(institutionId, cancellationToken);
        return institution is null
            ? Fail<InstitutionSummary>("institution_not_found", 404, "Institution was not found.")
            : new AcademySuccess<InstitutionSummary>(institution);
    }

    public async Task<AcademyResult<CareerSummary>> CreateCareerAsync(
        CreateCareerCommand command,
        CancellationToken cancellationToken)
    {
        if (await _repository.GetInstitutionAsync(command.InstitutionId, cancellationToken) is null)
        {
            return Fail<CareerSummary>("institution_not_found", 404, "Institution was not found.");
        }

        if (IsBlank(command.Code) || IsBlank(command.Name))
        {
            return Fail<CareerSummary>("invalid_career", 400, "Career code and name are required.");
        }

        var code = command.Code.Trim().ToUpperInvariant();
        if (await _repository.CareerCodeExistsAsync(command.InstitutionId, code, cancellationToken))
        {
            return Fail<CareerSummary>("career_code_exists", 409, "A career with that code already exists for this institution.");
        }

        var normalized = command with { Code = code, Name = command.Name.Trim() };
        var career = await _repository.CreateCareerAsync(normalized, _timeProvider.GetUtcNow(), cancellationToken);
        return new AcademySuccess<CareerSummary>(career);
    }

    public async Task<AcademyResult<StudentSummary>> CreateStudentAsync(
        CreateStudentCommand command,
        CancellationToken cancellationToken)
    {
        if (await _repository.GetInstitutionAsync(command.InstitutionId, cancellationToken) is null)
        {
            return Fail<StudentSummary>("institution_not_found", 404, "Institution was not found.");
        }

        var externalReference = BlankToNull(command.ExternalReference);
        if (externalReference is not null
            && await _repository.StudentExternalReferenceExistsAsync(command.InstitutionId, externalReference, cancellationToken))
        {
            return Fail<StudentSummary>("student_external_reference_exists", 409, "A student with that externalReference already exists for this institution.");
        }

        var walletAddress = BlockchainIdentity.NormalizeWalletAddress(command.WalletAddress);
        if (!string.IsNullOrWhiteSpace(command.WalletAddress) && walletAddress is null)
        {
            return Fail<StudentSummary>("invalid_wallet_address", 400, "walletAddress must be a valid Ethereum address.");
        }

        var normalized = command with { ExternalReference = externalReference, WalletAddress = walletAddress };
        var did = walletAddress is null ? null : BlockchainIdentity.CreateDid(walletAddress);
        var student = await _repository.CreateStudentAsync(normalized, did, _timeProvider.GetUtcNow(), cancellationToken);
        return new AcademySuccess<StudentSummary>(student);
    }

    public async Task<AcademyResult<IReadOnlyList<StudentSummary>>> ListStudentsAsync(
        Guid institutionId,
        CancellationToken cancellationToken)
    {
        if (await _repository.GetInstitutionAsync(institutionId, cancellationToken) is null)
        {
            return Fail<IReadOnlyList<StudentSummary>>("institution_not_found", 404, "Institution was not found.");
        }

        var students = await _repository.ListStudentsAsync(institutionId, cancellationToken);
        return new AcademySuccess<IReadOnlyList<StudentSummary>>(students);
    }

    public async Task<AcademyResult<StudentSummary>> GetStudentAsync(
        Guid institutionId,
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var student = await _repository.GetStudentAsync(institutionId, studentId, cancellationToken);
        return student is null
            ? Fail<StudentSummary>("student_not_found", 404, "Student was not found.")
            : new AcademySuccess<StudentSummary>(student);
    }

    public async Task<AcademyResult<StudentWalletSummary>> AddStudentWalletAsync(
        AddStudentWalletCommand command,
        CancellationToken cancellationToken)
    {
        if (await _repository.GetInstitutionAsync(command.InstitutionId, cancellationToken) is null)
        {
            return Fail<StudentWalletSummary>("institution_not_found", 404, "Institution was not found.");
        }

        var walletAddress = BlockchainIdentity.NormalizeWalletAddress(command.WalletAddress);
        if (walletAddress is null)
        {
            return Fail<StudentWalletSummary>("invalid_wallet_address", 400, "walletAddress must be a valid Ethereum address.");
        }

        var normalized = command with { WalletAddress = walletAddress };
        var wallet = await _repository.AddStudentWalletAsync(
            normalized,
            BlockchainIdentity.CreateDid(walletAddress),
            _timeProvider.GetUtcNow(),
            cancellationToken);

        return wallet is null
            ? Fail<StudentWalletSummary>("student_not_found", 404, "Student was not found.")
            : new AcademySuccess<StudentWalletSummary>(wallet);
    }

    public async Task<AcademyResult<HolderDashboard>> GetHolderDashboardAsync(
        string? walletAddress,
        string? did,
        CancellationToken cancellationToken)
    {
        var normalizedWallet = BlockchainIdentity.NormalizeWalletAddress(walletAddress);
        if (normalizedWallet is null)
        {
            return Fail<HolderDashboard>("invalid_holder_wallet", 400, "A valid holder wallet address is required.");
        }

        var dashboard = await _repository.GetHolderDashboardAsync(
            normalizedWallet,
            NormalizeDid(did, normalizedWallet),
            cancellationToken);

        return new AcademySuccess<HolderDashboard>(dashboard);
    }

    public async Task<AcademyResult<HolderProfile>> UpdateHolderProfileAsync(
        UpdateHolderProfileCommand command,
        CancellationToken cancellationToken)
    {
        var walletAddress = BlockchainIdentity.NormalizeWalletAddress(command.WalletAddress);
        if (walletAddress is null)
        {
            return Fail<HolderProfile>("invalid_holder_wallet", 400, "A valid holder wallet address is required.");
        }

        if (command.BirthDate is DateOnly birthDate
            && birthDate > DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime))
        {
            return Fail<HolderProfile>("invalid_birth_date", 400, "birthDate cannot be in the future.");
        }

        var contactEmail = BlankToNull(command.ContactEmail);
        if (contactEmail is not null && !contactEmail.Contains('@', StringComparison.Ordinal))
        {
            return Fail<HolderProfile>("invalid_contact_email", 400, "contactEmail must be a valid email address.");
        }

        var countryCode = BlankToNull(command.CountryCode);
        if (countryCode is not null && countryCode.Length != 2)
        {
            return Fail<HolderProfile>("invalid_country_code", 400, "countryCode must use two letters.");
        }

        var normalized = command with
        {
            WalletAddress = walletAddress,
            Did = NormalizeDid(command.Did, walletAddress),
            DisplayName = BlankToNull(command.DisplayName),
            FullName = BlankToNull(command.FullName),
            ContactEmail = contactEmail?.ToLowerInvariant(),
            CountryCode = countryCode?.ToUpperInvariant(),
            PhoneNumber = BlankToNull(command.PhoneNumber)
        };

        var profile = await _repository.UpdateHolderProfileAsync(
            normalized,
            _timeProvider.GetUtcNow(),
            cancellationToken);

        return new AcademySuccess<HolderProfile>(profile);
    }

    public async Task<AcademyResult<IReadOnlyList<InstitutionUserSummary>>> ListInstitutionUsersAsync(
        Guid institutionId,
        CancellationToken cancellationToken)
    {
        if (await _repository.GetInstitutionAsync(institutionId, cancellationToken) is null)
        {
            return Fail<IReadOnlyList<InstitutionUserSummary>>("institution_not_found", 404, "Institution was not found.");
        }

        var users = await _repository.ListInstitutionUsersAsync(institutionId, cancellationToken);
        return new AcademySuccess<IReadOnlyList<InstitutionUserSummary>>(users);
    }

    public async Task<AcademyResult<InstitutionUserSummary>> UpdateInstitutionUserRoleAsync(
        Guid institutionId,
        Guid userId,
        string role,
        CancellationToken cancellationToken)
    {
        if (!InstitutionRoles.IsValid(role) || string.Equals(role, InstitutionRoles.Student, StringComparison.OrdinalIgnoreCase))
        {
            return Fail<InstitutionUserSummary>("invalid_institution_role", 400, "Institution role is not supported.");
        }

        var updated = await _repository.UpdateInstitutionUserRoleAsync(
            institutionId,
            userId,
            InstitutionRoles.Normalize(role),
            _timeProvider.GetUtcNow(),
            cancellationToken);

        return updated is null
            ? Fail<InstitutionUserSummary>("institution_user_not_found", 404, "Institution user was not found.")
            : new AcademySuccess<InstitutionUserSummary>(updated);
    }

    public async Task<AcademyResult<bool>> RevokeInstitutionUserAsync(
        Guid institutionId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var revoked = await _repository.RevokeInstitutionUserAsync(
            institutionId,
            userId,
            _timeProvider.GetUtcNow(),
            cancellationToken);

        return revoked
            ? new AcademySuccess<bool>(true)
            : Fail<bool>("institution_user_not_found", 404, "Institution user was not found.");
    }

    public Task<AcademyResult<InstitutionInvitationCreated>> CreateInvitationAsync(
        CreateInstitutionInvitationCommand command,
        CancellationToken cancellationToken) =>
        CreateInvitationInternalAsync(command, _timeProvider.GetUtcNow(), cancellationToken);

    public async Task<AcademyResult<InstitutionInvitationAccepted>> AcceptInvitationAsync(
        AcceptInstitutionInvitationCommand command,
        CancellationToken cancellationToken)
    {
        if (IsBlank(command.Token))
        {
            return Fail<InstitutionInvitationAccepted>("invalid_invitation_token", 400, "Invitation token is required.");
        }

        var walletAddress = BlockchainIdentity.NormalizeWalletAddress(command.WalletAddress);
        if (walletAddress is null)
        {
            return Fail<InstitutionInvitationAccepted>("invalid_wallet_address", 400, "walletAddress must be a valid Ethereum address.");
        }

        var accepted = await _repository.AcceptInvitationAsync(
            _tokenService.HashToken(command.Token),
            walletAddress,
            BlockchainIdentity.CreateDid(walletAddress),
            BlankToNull(command.DisplayName),
            _timeProvider.GetUtcNow(),
            cancellationToken);

        return accepted is null
            ? Fail<InstitutionInvitationAccepted>("invitation_not_usable", 404, "Invitation was not found, has expired, or was already accepted.")
            : new AcademySuccess<InstitutionInvitationAccepted>(accepted);
    }

    private async Task<AcademyResult<InstitutionInvitationCreated>> CreateInvitationInternalAsync(
        CreateInstitutionInvitationCommand command,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (await _repository.GetInstitutionAsync(command.InstitutionId, cancellationToken) is null)
        {
            return Fail<InstitutionInvitationCreated>("institution_not_found", 404, "Institution was not found.");
        }

        if (IsBlank(command.Email) || !command.Email.Contains('@', StringComparison.Ordinal))
        {
            return Fail<InstitutionInvitationCreated>("invalid_invitation_email", 400, "A valid invitation email is required.");
        }

        if (!InstitutionRoles.IsValid(command.Role) || string.Equals(command.Role, InstitutionRoles.Student, StringComparison.OrdinalIgnoreCase))
        {
            return Fail<InstitutionInvitationCreated>("invalid_institution_role", 400, "Institution role is not supported.");
        }

        var token = _tokenService.CreateToken();
        var invitationUrl = BuildInvitationUrl(token);
        var normalized = command with
        {
            Email = command.Email.Trim().ToLowerInvariant(),
            Role = InstitutionRoles.Normalize(command.Role)
        };

        var invitation = await _repository.CreateInvitationAsync(
            normalized,
            _tokenService.HashToken(token),
            invitationUrl,
            now.AddHours(Math.Max(1, _options.InvitationTtlHours)),
            now,
            cancellationToken);

        await _emailSender.SendInvitationAsync(invitation.Email, invitation.InvitationUrl, invitation.ExpiresAt, cancellationToken);
        return new AcademySuccess<InstitutionInvitationCreated>(invitation);
    }

    private string BuildInvitationUrl(string token)
    {
        var separator = _options.InvitationBaseUrl.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return $"{_options.InvitationBaseUrl}{separator}token={Uri.EscapeDataString(token)}";
    }

    private static AcademyFailureResult<T> Fail<T>(string errorCode, int statusCode, string detail) =>
        new(new AcademyFailure(errorCode, statusCode, detail));

    private static bool IsBlank(string? value) => string.IsNullOrWhiteSpace(value);

    private static string? BlankToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizeDid(string? did, string walletAddress) =>
        string.IsNullOrWhiteSpace(did) ? BlockchainIdentity.CreateDid(walletAddress) : did.Trim().ToLowerInvariant();

    private static string NormalizeCountry(string? countryCode)
    {
        var normalized = string.IsNullOrWhiteSpace(countryCode) ? "CL" : countryCode.Trim().ToUpperInvariant();
        return normalized.Length == 2 ? normalized : "CL";
    }
}

