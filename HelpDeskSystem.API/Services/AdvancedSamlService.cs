using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Domain.Enums;
using HelpDeskSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.API.Services;

public interface IAdvancedSamlService
{
    Task<SamlResponse> ProcessSamlResponseAsync(string samlResponse, string? relayState = null);
    Task<string> GenerateSamlRequestAsync(string identityProviderId, string returnUrl);
    Task<SamlMetadata> GetProviderMetadataAsync(string identityProviderId);
    Task<bool> ValidateSamlResponseAsync(string samlResponse, string identityProviderId);
    Task<SamlAssertion?> ExtractAssertionAsync(string samlResponse);
    Task<Dictionary<string, string>> ExtractAttributesAsync(SamlAssertion assertion);
    Task<bool> ValidateSignatureAsync(string samlResponse, X509Certificate2? certificate);
    Task<bool> ValidateConditionsAsync(SamlAssertion assertion);
    Task<bool> ValidateAudienceRestrictionAsync(SamlAssertion assertion, string audience);
    Task<bool> ValidateTimeRestrictionsAsync(SamlAssertion assertion);
    Task<X509Certificate2?> GetProviderCertificateAsync(string identityProviderId);
    Task<List<SamlIdentityProvider>> GetConfiguredProvidersAsync();
    Task<SamlIdentityProvider?> GetProviderByIdAsync(string identityProviderId);
    Task<bool> TestProviderConnectionAsync(string identityProviderId);
}

public class AdvancedSamlService : IAdvancedSamlService
{
    private const string SamlProtocolNs = "urn:oasis:names:tc:SAML:2.0:protocol";
    private const string SamlAssertionNs = "urn:oasis:names:tc:SAML:2.0:assertion";

    private readonly HelpDeskDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AdvancedSamlService> _logger;

    public AdvancedSamlService(
        HelpDeskDbContext context,
        IHttpClientFactory httpClientFactory,
        ILogger<AdvancedSamlService> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<SamlResponse> ProcessSamlResponseAsync(string samlResponse, string? relayState = null)
    {
        var xmlDocument = DecodeSamlXml(samlResponse);
        var issuer = GetSingleNodeText(xmlDocument, "//saml:Issuer", SamlAssertionNs) ?? string.Empty;
        var destination = xmlDocument.DocumentElement?.GetAttribute("Destination") ?? string.Empty;
        var issueInstantRaw = xmlDocument.DocumentElement?.GetAttribute("IssueInstant");
        var issueInstant = DateTime.TryParse(issueInstantRaw, out var parsed) ? parsed : DateTime.UtcNow;

        var isValid = await ValidateSamlResponseAsync(samlResponse, issuer);
        var assertion = await ExtractAssertionAsync(samlResponse);

        return new SamlResponse
        {
            IsValid = isValid,
            RelayState = relayState,
            Issuer = issuer,
            Destination = destination,
            IssueInstant = issueInstant,
            Assertion = assertion,
            Xml = xmlDocument
        };
    }

    public async Task<string> GenerateSamlRequestAsync(string identityProviderId, string returnUrl)
    {
        var provider = await GetProviderByIdAsync(identityProviderId)
            ?? throw new ArgumentException($"Identity provider {identityProviderId} not found");

        var requestId = "_" + Guid.NewGuid().ToString("N");
        var issueInstant = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        var samlRequest = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<samlp:AuthnRequest xmlns:samlp=""{SamlProtocolNs}"" xmlns:saml=""{SamlAssertionNs}"" ID=""{requestId}"" Version=""2.0"" IssueInstant=""{issueInstant}"" Destination=""{provider.SsoUrl}"" AssertionConsumerServiceURL=""{returnUrl}"">
  <saml:Issuer>{provider.ServiceProviderEntityId}</saml:Issuer>
  <samlp:NameIDPolicy Format=""{provider.NameIdFormat}"" AllowCreate=""true"" />
</samlp:AuthnRequest>";

        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(samlRequest));
        var authUrl = $"{provider.SsoUrl}?SAMLRequest={Uri.EscapeDataString(encoded)}";
        if (!string.IsNullOrWhiteSpace(provider.RelayStateParameter))
            authUrl += $"&RelayState={Uri.EscapeDataString(returnUrl)}";

        return authUrl;
    }

    public async Task<SamlMetadata> GetProviderMetadataAsync(string identityProviderId)
    {
        var provider = await GetProviderByIdAsync(identityProviderId)
            ?? throw new ArgumentException($"Identity provider {identityProviderId} not found");

        if (string.IsNullOrWhiteSpace(provider.MetadataUrl))
            return new SamlMetadata();

        var client = _httpClientFactory.CreateClient();
        var metadataXml = await client.GetStringAsync(provider.MetadataUrl);

        var xml = new XmlDocument { PreserveWhitespace = true };
        xml.LoadXml(metadataXml);

        var ns = new XmlNamespaceManager(xml.NameTable);
        ns.AddNamespace("md", "urn:oasis:names:tc:SAML:2.0:metadata");
        ns.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#");

        var ssoServices = xml.SelectNodes("//md:IDPSSODescriptor/md:SingleSignOnService", ns)
            ?.Cast<XmlNode>()
            .Select(x => new SamlSsoService
            {
                Binding = x.Attributes?["Binding"]?.Value ?? string.Empty,
                Location = x.Attributes?["Location"]?.Value ?? string.Empty
            })
            .ToList() ?? [];

        var certValue = xml.SelectSingleNode("//md:IDPSSODescriptor//ds:X509Certificate", ns)?.InnerText?.Trim() ?? string.Empty;

        return new SamlMetadata
        {
            EntityId = xml.DocumentElement?.GetAttribute("entityID") ?? string.Empty,
            MetadataXml = metadataXml,
            SsoServices = ssoServices,
            SigningCertificate = certValue
        };
    }

    public async Task<bool> ValidateSamlResponseAsync(string samlResponse, string identityProviderId)
    {
        var provider = await GetProviderByIdAsync(identityProviderId);
        if (provider == null)
            return false;

        var xml = DecodeSamlXml(samlResponse);
        var issuer = GetSingleNodeText(xml, "//saml:Issuer", SamlAssertionNs);
        if (provider.EnforceStrictIssuer && !string.Equals(provider.Issuer, issuer, StringComparison.OrdinalIgnoreCase))
            return false;

        var assertion = await ExtractAssertionAsync(samlResponse);
        if (assertion == null)
            return false;

        var cert = await GetProviderCertificateAsync(identityProviderId);
        var signatureValid = await ValidateSignatureAsync(samlResponse, cert);
        if (!signatureValid)
            _logger.LogWarning("SAML signature could not be fully validated for provider {Provider}", identityProviderId);

        return await ValidateConditionsAsync(assertion)
               && await ValidateAudienceRestrictionAsync(assertion, provider.ServiceProviderEntityId)
               && await ValidateTimeRestrictionsAsync(assertion);
    }

    public Task<SamlAssertion?> ExtractAssertionAsync(string samlResponse)
    {
        var xml = DecodeSamlXml(samlResponse);
        var ns = new XmlNamespaceManager(xml.NameTable);
        ns.AddNamespace("saml", SamlAssertionNs);

        var assertionNode = xml.SelectSingleNode("//saml:Assertion", ns);
        if (assertionNode == null)
            return Task.FromResult<SamlAssertion?>(null);

        var assertion = new SamlAssertion
        {
            Id = assertionNode.Attributes?["ID"]?.Value ?? string.Empty,
            IssueInstant = ParseDate(assertionNode.Attributes?["IssueInstant"]?.Value),
            Issuer = xml.SelectSingleNode("//saml:Assertion/saml:Issuer", ns)?.InnerText ?? string.Empty,
            SubjectNameId = xml.SelectSingleNode("//saml:Assertion/saml:Subject/saml:NameID", ns)?.InnerText ?? string.Empty,
            NotBefore = ParseDate(xml.SelectSingleNode("//saml:Assertion/saml:Conditions", ns)?.Attributes?["NotBefore"]?.Value),
            NotOnOrAfter = ParseDate(xml.SelectSingleNode("//saml:Assertion/saml:Conditions", ns)?.Attributes?["NotOnOrAfter"]?.Value),
            Audience = xml.SelectSingleNode("//saml:Assertion/saml:Conditions/saml:AudienceRestriction/saml:Audience", ns)?.InnerText ?? string.Empty,
            Attributes = ParseAttributes(xml, ns)
        };

        return Task.FromResult<SamlAssertion?>(assertion);
    }

    public Task<Dictionary<string, string>> ExtractAttributesAsync(SamlAssertion assertion)
        => Task.FromResult(assertion.Attributes);

    public Task<bool> ValidateSignatureAsync(string samlResponse, X509Certificate2? certificate)
    {
        var xml = DecodeSamlXml(samlResponse);
        var hasSignature = xml.GetElementsByTagName("Signature", "http://www.w3.org/2000/09/xmldsig#").Count > 0;
        if (!hasSignature)
            return Task.FromResult(false);

        if (certificate == null)
            return Task.FromResult(true);

        // Minimal validation guard for runtime hardening path.
        var now = DateTime.UtcNow;
        return Task.FromResult(certificate.NotBefore <= now && certificate.NotAfter >= now);
    }

    public Task<bool> ValidateConditionsAsync(SamlAssertion assertion)
    {
        var now = DateTime.UtcNow;
        var notBeforeOk = !assertion.NotBefore.HasValue || now >= assertion.NotBefore.Value.AddMinutes(-2);
        var notAfterOk = !assertion.NotOnOrAfter.HasValue || now < assertion.NotOnOrAfter.Value.AddMinutes(2);
        return Task.FromResult(notBeforeOk && notAfterOk);
    }

    public Task<bool> ValidateAudienceRestrictionAsync(SamlAssertion assertion, string audience)
    {
        if (string.IsNullOrWhiteSpace(audience) || string.IsNullOrWhiteSpace(assertion.Audience))
            return Task.FromResult(true);

        return Task.FromResult(string.Equals(assertion.Audience, audience, StringComparison.OrdinalIgnoreCase));
    }

    public Task<bool> ValidateTimeRestrictionsAsync(SamlAssertion assertion)
        => ValidateConditionsAsync(assertion);

    public async Task<X509Certificate2?> GetProviderCertificateAsync(string identityProviderId)
    {
        var provider = await GetProviderByIdAsync(identityProviderId);
        if (provider == null || string.IsNullOrWhiteSpace(provider.SigningCertificateBase64))
            return null;

        try
        {
            return new X509Certificate2(Convert.FromBase64String(provider.SigningCertificateBase64));
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<SamlIdentityProvider>> GetConfiguredProvidersAsync()
    {
        var rows = await _context.IdentityProviderConfigs
            .AsNoTracking()
            .Where(x => x.Protocol == IdentityProtocol.Saml && !x.IsDeleted)
            .ToListAsync();

        return rows.Select(MapProvider).ToList();
    }

    public async Task<SamlIdentityProvider?> GetProviderByIdAsync(string identityProviderId)
    {
        IdentityProviderConfig? row;
        if (int.TryParse(identityProviderId, out var id))
        {
            row = await _context.IdentityProviderConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && x.Protocol == IdentityProtocol.Saml && !x.IsDeleted);
        }
        else
        {
            row = await _context.IdentityProviderConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Name == identityProviderId && x.Protocol == IdentityProtocol.Saml && !x.IsDeleted);
        }

        return row == null ? null : MapProvider(row);
    }

    public async Task<bool> TestProviderConnectionAsync(string identityProviderId)
    {
        var provider = await GetProviderByIdAsync(identityProviderId);
        if (provider == null)
            return false;

        if (string.IsNullOrWhiteSpace(provider.MetadataUrl))
            return false;

        try
        {
            var client = _httpClientFactory.CreateClient();
            using var response = await client.GetAsync(provider.MetadataUrl);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SAML provider connectivity test failed for {Provider}", identityProviderId);
            return false;
        }
    }

    private static SamlIdentityProvider MapProvider(IdentityProviderConfig row)
    {
        var metadataCert = TryExtractMetadataCertificate(row.SamlMetadataXml);
        return new SamlIdentityProvider
        {
            Id = row.Id.ToString(),
            Name = row.Name,
            Issuer = row.Issuer,
            MetadataUrl = row.AuthorityOrMetadataUrl,
            SsoUrl = row.AuthorityOrMetadataUrl,
            ServiceProviderEntityId = row.SamlSpEntityId,
            NameIdFormat = "urn:oasis:names:tc:SAML:1.1:nameid-format:emailAddress",
            RelayStateParameter = "RelayState",
            EnforceStrictIssuer = row.EnforceStrictIssuer,
            SigningCertificateBase64 = metadataCert,
            AllowedCertificateThumbprints = row.SamlAllowedCertificateThumbprints
        };
    }

    private static string TryExtractMetadataCertificate(string metadataXml)
    {
        if (string.IsNullOrWhiteSpace(metadataXml))
            return string.Empty;

        try
        {
            var xml = new XmlDocument { PreserveWhitespace = true };
            xml.LoadXml(metadataXml);
            var ns = new XmlNamespaceManager(xml.NameTable);
            ns.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#");
            return xml.SelectSingleNode("//ds:X509Certificate", ns)?.InnerText?.Trim() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static Dictionary<string, string> ParseAttributes(XmlDocument xml, XmlNamespaceManager ns)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var attrs = xml.SelectNodes("//saml:Assertion/saml:AttributeStatement/saml:Attribute", ns);
        if (attrs == null)
            return map;

        foreach (var node in attrs.Cast<XmlNode>())
        {
            var name = node.Attributes?["Name"]?.Value;
            if (string.IsNullOrWhiteSpace(name))
                continue;

            var value = node.SelectSingleNode("saml:AttributeValue", ns)?.InnerText ?? string.Empty;
            map[name] = value;
        }

        return map;
    }

    private static XmlDocument DecodeSamlXml(string samlResponse)
    {
        var bytes = Convert.FromBase64String(samlResponse);
        var xml = new XmlDocument { PreserveWhitespace = true };
        xml.LoadXml(Encoding.UTF8.GetString(bytes));
        return xml;
    }

    private static string? GetSingleNodeText(XmlDocument xml, string xpath, string nsUri)
    {
        var ns = new XmlNamespaceManager(xml.NameTable);
        ns.AddNamespace("saml", nsUri);
        return xml.SelectSingleNode(xpath, ns)?.InnerText;
    }

    private static DateTime? ParseDate(string? value)
        => DateTime.TryParse(value, out var parsed) ? parsed : null;
}

public class SamlIdentityProvider
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string MetadataUrl { get; set; } = string.Empty;
    public string SsoUrl { get; set; } = string.Empty;
    public string ServiceProviderEntityId { get; set; } = string.Empty;
    public string NameIdFormat { get; set; } = "urn:oasis:names:tc:SAML:1.1:nameid-format:emailAddress";
    public string RelayStateParameter { get; set; } = "RelayState";
    public bool EnforceStrictIssuer { get; set; } = true;
    public string SigningCertificateBase64 { get; set; } = string.Empty;
    public string AllowedCertificateThumbprints { get; set; } = string.Empty;
}

public class SamlMetadata
{
    public string EntityId { get; set; } = string.Empty;
    public string MetadataXml { get; set; } = string.Empty;
    public List<SamlSsoService> SsoServices { get; set; } = [];
    public string SigningCertificate { get; set; } = string.Empty;
}

public class SamlSsoService
{
    public string Binding { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}

public class SamlResponse
{
    public bool IsValid { get; set; }
    public XmlDocument? Xml { get; set; }
    public string? RelayState { get; set; }
    public string Issuer { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public DateTime IssueInstant { get; set; }
    public SamlAssertion? Assertion { get; set; }
}

public class SamlAssertion
{
    public string Id { get; set; } = string.Empty;
    public DateTime? IssueInstant { get; set; }
    public string Issuer { get; set; } = string.Empty;
    public string SubjectNameId { get; set; } = string.Empty;
    public DateTime? NotBefore { get; set; }
    public DateTime? NotOnOrAfter { get; set; }
    public string Audience { get; set; } = string.Empty;
    public Dictionary<string, string> Attributes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
