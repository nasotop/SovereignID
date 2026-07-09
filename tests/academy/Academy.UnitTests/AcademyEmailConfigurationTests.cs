using Academy.Application;
using Academy.Infrastructure.Email;
using Microsoft.Extensions.Configuration;

namespace Academy.UnitTests;

public sealed class AcademyEmailConfigurationTests
{
    [Fact]
    public void ValidateAcademyEmailConfiguration_WhenProviderIsLogging_DoesNotThrow()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            [$"{AcademyOptions.SectionName}:Email:Provider"] = EmailProviders.Logging
        });

        var exception = Record.Exception(() =>
            EmailServiceCollectionExtensions.ValidateAcademyEmailConfiguration(configuration));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData("", "noreply@example.com")]
    [InlineData("smtp.example.com", "")]
    [InlineData("smtp.example.com", "invalid-address")]
    public void ValidateAcademyEmailConfiguration_WhenProviderIsSmtpAndRequiredFieldsMissing_Throws(
        string host,
        string fromAddress)
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            [$"{AcademyOptions.SectionName}:Email:Provider"] = EmailProviders.Smtp,
            [$"{AcademyOptions.SectionName}:Email:Smtp:Host"] = host,
            [$"{AcademyOptions.SectionName}:Email:Smtp:FromAddress"] = fromAddress,
            [$"{AcademyOptions.SectionName}:Email:Smtp:Port"] = "587"
        });

        Assert.Throws<InvalidOperationException>(() =>
            EmailServiceCollectionExtensions.ValidateAcademyEmailConfiguration(configuration));
    }

    [Fact]
    public void ValidateAcademyEmailConfiguration_WhenProviderIsSmtpAndUsernameSetWithoutPassword_Throws()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            [$"{AcademyOptions.SectionName}:Email:Provider"] = EmailProviders.Smtp,
            [$"{AcademyOptions.SectionName}:Email:Smtp:Host"] = "smtp.example.com",
            [$"{AcademyOptions.SectionName}:Email:Smtp:FromAddress"] = "noreply@example.com",
            [$"{AcademyOptions.SectionName}:Email:Smtp:Port"] = "587",
            [$"{AcademyOptions.SectionName}:Email:Smtp:Username"] = "noreply@example.com"
        });

        Assert.Throws<InvalidOperationException>(() =>
            EmailServiceCollectionExtensions.ValidateAcademyEmailConfiguration(configuration));
    }

    [Fact]
    public void ValidateAcademyEmailConfiguration_WhenProviderIsSmtpAndConfigurationIsValid_DoesNotThrow()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            [$"{AcademyOptions.SectionName}:Email:Provider"] = EmailProviders.Smtp,
            [$"{AcademyOptions.SectionName}:Email:Smtp:Host"] = "smtp.example.com",
            [$"{AcademyOptions.SectionName}:Email:Smtp:FromAddress"] = "noreply@example.com",
            [$"{AcademyOptions.SectionName}:Email:Smtp:Port"] = "587",
            [$"{AcademyOptions.SectionName}:Email:Smtp:Username"] = "noreply@example.com",
            [$"{AcademyOptions.SectionName}:Email:Smtp:Password"] = "secret"
        });

        var exception = Record.Exception(() =>
            EmailServiceCollectionExtensions.ValidateAcademyEmailConfiguration(configuration));

        Assert.Null(exception);
    }

    [Fact]
    public void UsesSmtpEmail_WhenProviderIsSmtp_ReturnsTrue()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            [$"{AcademyOptions.SectionName}:Email:Provider"] = EmailProviders.Smtp
        });

        Assert.True(EmailServiceCollectionExtensions.UsesSmtpEmail(configuration));
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
