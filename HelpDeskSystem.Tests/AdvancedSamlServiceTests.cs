using System.Text;
using HelpDeskSystem.API.Services;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Domain.Enums;
using HelpDeskSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace HelpDeskSystem.Tests;

public class AdvancedSamlServiceTests
{
    [Fact]
    public async Task GenerateSamlRequestAsync_BuildsRedirectUrl()
    {
        await using var context = CreateDbContext();
        context.IdentityProviderConfigs.Add(new IdentityProviderConfig
        {
            TenantId = 1,
            Name = "saml-idp",
            Protocol = IdentityProtocol.Saml,
            Issuer = "https://idp.example.com",
            AuthorityOrMetadataUrl = "https://idp.example.com/sso",
            SamlSpEntityId = "https://sp.example.com",
            EnforceStrictIssuer = true,
            IsEnabled = true
        });
        await context.SaveChangesAsync();

        var service = new AdvancedSamlService(context, new StaticHttpClientFactory(), NullLogger<AdvancedSamlService>.Instance);

        var url = await service.GenerateSamlRequestAsync("saml-idp", "https://app.example.com/acs");

        Assert.StartsWith("https://idp.example.com/sso?SAMLRequest=", url);
        Assert.Contains("RelayState=", url);
    }

    [Fact]
    public async Task ExtractAssertionAsync_ReturnsParsedAssertion()
    {
        await using var context = CreateDbContext();
        context.IdentityProviderConfigs.Add(new IdentityProviderConfig
        {
            TenantId = 1,
            Name = "idp-strict",
            Protocol = IdentityProtocol.Saml,
            Issuer = "https://idp.example.com",
            AuthorityOrMetadataUrl = "https://idp.example.com/sso",
            SamlSpEntityId = "https://sp.example.com",
            EnforceStrictIssuer = true,
            IsEnabled = true
        });
        await context.SaveChangesAsync();

        var service = new AdvancedSamlService(context, new StaticHttpClientFactory(), NullLogger<AdvancedSamlService>.Instance);
        var samlResponse = BuildSamlResponse(
            issuer: "https://idp.example.com",
            audience: "https://sp.example.com");

        var assertion = await service.ExtractAssertionAsync(samlResponse);

        Assert.NotNull(assertion);
        Assert.Equal("https://idp.example.com", assertion!.Issuer);
        Assert.Equal("https://sp.example.com", assertion.Audience);
        Assert.Equal("user@example.com", assertion.SubjectNameId);
    }

    [Fact]
    public async Task ValidateSamlResponseAsync_ReturnsFalse_ForIssuerMismatch()
    {
        await using var context = CreateDbContext();
        context.IdentityProviderConfigs.Add(new IdentityProviderConfig
        {
            TenantId = 1,
            Name = "idp-strict",
            Protocol = IdentityProtocol.Saml,
            Issuer = "https://idp.example.com",
            AuthorityOrMetadataUrl = "https://idp.example.com/sso",
            SamlSpEntityId = "https://sp.example.com",
            EnforceStrictIssuer = true,
            IsEnabled = true
        });
        await context.SaveChangesAsync();

        var service = new AdvancedSamlService(context, new StaticHttpClientFactory(), NullLogger<AdvancedSamlService>.Instance);
        var samlResponse = BuildSamlResponse(
            issuer: "https://evil.example.com",
            audience: "https://sp.example.com");

        var result = await service.ValidateSamlResponseAsync(samlResponse, "idp-strict");

        Assert.False(result);
    }

    private static HelpDeskDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<HelpDeskDbContext>()
            .UseInMemoryDatabase($"saml-tests-{Guid.NewGuid():N}")
            .Options;
        return new HelpDeskDbContext(options);
    }

    private static string BuildSamlResponse(string issuer, string audience)
    {
        var now = DateTime.UtcNow;
        var notBefore = now.AddMinutes(-1).ToString("o");
        var notOnOrAfter = now.AddMinutes(5).ToString("o");
        var issueInstant = now.ToString("o");

        var xml = $@"<samlp:Response xmlns:samlp='urn:oasis:names:tc:SAML:2.0:protocol' xmlns:saml='urn:oasis:names:tc:SAML:2.0:assertion' xmlns:ds='http://www.w3.org/2000/09/xmldsig#' Destination='https://app.example.com/acs' IssueInstant='{issueInstant}'>
  <saml:Issuer>{issuer}</saml:Issuer>
  <ds:Signature><ds:SignedInfo /></ds:Signature>
  <saml:Assertion ID='_a1' IssueInstant='{issueInstant}'>
    <saml:Issuer>{issuer}</saml:Issuer>
    <saml:Subject><saml:NameID>user@example.com</saml:NameID></saml:Subject>
    <saml:Conditions NotBefore='{notBefore}' NotOnOrAfter='{notOnOrAfter}'>
      <saml:AudienceRestriction><saml:Audience>{audience}</saml:Audience></saml:AudienceRestriction>
    </saml:Conditions>
    <saml:AttributeStatement>
      <saml:Attribute Name='email'><saml:AttributeValue>user@example.com</saml:AttributeValue></saml:Attribute>
    </saml:AttributeStatement>
  </saml:Assertion>
</samlp:Response>";

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(xml));
    }

    private sealed class StaticHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name = "") => new();
    }
}
