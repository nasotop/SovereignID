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

        if (!InstitutionRoles.IsValid(command.Role))
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

    private static string NormalizeCountry(string? countryCode)
    {
        var normalized = string.IsNullOrWhiteSpace(countryCode) ? "CL" : countryCode.Trim().ToUpperInvariant();
        return normalized.Length == 2 ? normalized : "CL";
    }
}

