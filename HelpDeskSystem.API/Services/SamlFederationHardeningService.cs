using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Domain.Enums;
using HelpDeskSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.API.Services;

public interface ISamlFederationHardeningService
{
    Task<SamlHardeningAssessment> AssessProviderAsync(int providerId, string? samlResponse = null, CancellationToken cancellationToken = default);
}

public class SamlHardeningAssessment
{
    public int ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public List<SamlHardeningCheck> Checks { get; set; } = [];
}

public class SamlHardeningCheck
{
    public string Name { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string Detail { get; set; } = string.Empty;
}

public class SamlFederationHardeningService : ISamlFederationHardeningService
{
    private readonly HelpDeskDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;

    public SamlFederationHardeningService(HelpDeskDbContext context, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<SamlHardeningAssessment> AssessProviderAsync(int providerId, string? samlResponse = null, CancellationToken cancellationToken = default)
    {
        var provider = await _context.IdentityProviderConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == providerId && !x.IsDeleted, cancellationToken);
        if (provider == null)
        {
            return new SamlHardeningAssessment
            {
                ProviderId = providerId,
                ProviderName = string.Empty,
                Passed = false,
                Checks =
                [
                    new SamlHardeningCheck { Name = "provider_exists", Passed = false, Detail = "Provider not found." }
                ]
            };
        }

        var assessment = new SamlHardeningAssessment
        {
            ProviderId = provider.Id,
            ProviderName = provider.Name
        };

        assessment.Checks.Add(new SamlHardeningCheck
        {
            Name = "protocol_is_saml",
            Passed = provider.Protocol == IdentityProtocol.Saml,
            Detail = provider.Protocol.ToString()
        });

        assessment.Checks.Add(new SamlHardeningCheck
        {
            Name = "strict_issuer_enabled",
            Passed = provider.EnforceStrictIssuer,
            Detail = provider.EnforceStrictIssuer ? "Enabled" : "Disabled"
        });

        assessment.Checks.Add(new SamlHardeningCheck
        {
            Name = "signature_validation_enabled",
            Passed = provider.SamlValidateSignature,
            Detail = provider.SamlValidateSignature ? "Enabled" : "Disabled"
        });

        assessment.Checks.Add(new SamlHardeningCheck
        {
            Name = "idp_initiated_policy",
            Passed = !provider.SamlAllowIdpInitiated,
            Detail = provider.SamlAllowIdpInitiated ? "IdP-initiated enabled" : "SP-initiated only"
        });

        var metadataXml = await ResolveMetadataXmlAsync(provider, cancellationToken);
        assessment.Checks.Add(new SamlHardeningCheck
        {
            Name = "metadata_present",
            Passed = !string.IsNullOrWhiteSpace(metadataXml),
            Detail = string.IsNullOrWhiteSpace(metadataXml) ? "Metadata not available." : "Metadata loaded."
        });

        if (!string.IsNullOrWhiteSpace(metadataXml))
        {
            EvaluateMetadata(provider, metadataXml, assessment.Checks);
        }

        if (!string.IsNullOrWhiteSpace(samlResponse))
        {
            EvaluateSampleResponse(provider, samlResponse, assessment.Checks);
        }

        assessment.Passed = assessment.Checks.All(x => x.Passed);
        return assessment;
    }

    private static void EvaluateMetadata(IdentityProviderConfig provider, string xml, List<SamlHardeningCheck> checks)
    {
        var doc = new XmlDocument { PreserveWhitespace = true };
        doc.LoadXml(xml);

        var issuer = doc.SelectSingleNode("//*[local-name()='EntityDescriptor']")?.Attributes?["entityID"]?.Value ?? string.Empty;
        checks.Add(new SamlHardeningCheck
        {
            Name = "metadata_entity_id",
            Passed = !string.IsNullOrWhiteSpace(issuer),
            Detail = issuer
        });

        if (!string.IsNullOrWhiteSpace(provider.Issuer))
        {
            checks.Add(new SamlHardeningCheck
            {
                Name = "metadata_matches_configured_issuer",
                Passed = string.Equals(provider.Issuer.Trim(), issuer.Trim(), StringComparison.OrdinalIgnoreCase),
                Detail = $"Configured={provider.Issuer}, Metadata={issuer}"
            });
        }

        var ssoUrl = doc.SelectSingleNode("//*[local-name()='SingleSignOnService']")?.Attributes?["Location"]?.Value ?? string.Empty;
        checks.Add(new SamlHardeningCheck
        {
            Name = "metadata_sso_endpoint",
            Passed = !string.IsNullOrWhiteSpace(ssoUrl),
            Detail = ssoUrl
        });

        var certNodes = doc.SelectNodes("//*[local-name()='X509Certificate']");
        var certThumbprints = new List<string>();
        if (certNodes != null)
        {
            foreach (XmlNode node in certNodes)
            {
                var raw = node.InnerText?.Trim();
                if (string.IsNullOrWhiteSpace(raw))
                    continue;
                try
                {
                    var cert = new X509Certificate2(Convert.FromBase64String(raw));
                    certThumbprints.Add((cert.Thumbprint ?? string.Empty).Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant());
                }
                catch
                {
                    // Skip invalid cert nodes
                }
            }
        }

        checks.Add(new SamlHardeningCheck
        {
            Name = "metadata_signing_certificates",
            Passed = certThumbprints.Count > 0,
            Detail = $"Found {certThumbprints.Count} cert(s)."
        });

        var configuredThumbprints = provider.SamlAllowedCertificateThumbprints
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (configuredThumbprints.Count > 0)
        {
            var matched = certThumbprints.Any(t => configuredThumbprints.Contains(t));
            checks.Add(new SamlHardeningCheck
            {
                Name = "configured_thumbprint_matches_metadata",
                Passed = matched,
                Detail = matched ? "At least one configured thumbprint matches metadata certs." : "No configured thumbprint matched."
            });
        }

        checks.Add(new SamlHardeningCheck
        {
            Name = "acs_url_configured",
            Passed = !string.IsNullOrWhiteSpace(provider.SamlAcsUrl),
            Detail = provider.SamlAcsUrl
        });

        checks.Add(new SamlHardeningCheck
        {
            Name = "sp_entity_id_configured",
            Passed = !string.IsNullOrWhiteSpace(provider.SamlSpEntityId),
            Detail = provider.SamlSpEntityId
        });
    }

    private static void EvaluateSampleResponse(IdentityProviderConfig provider, string samlResponse, List<SamlHardeningCheck> checks)
    {
        var xml = DecodeSamlResponse(samlResponse);
        var doc = new XmlDocument { PreserveWhitespace = true };
        doc.LoadXml(xml);

        var responseIssuer = doc.SelectSingleNode("//*[local-name()='Issuer']")?.InnerText?.Trim() ?? string.Empty;
        checks.Add(new SamlHardeningCheck
        {
            Name = "sample_response_issuer_match",
            Passed = string.IsNullOrWhiteSpace(provider.Issuer) || string.Equals(provider.Issuer.Trim(), responseIssuer, StringComparison.OrdinalIgnoreCase),
            Detail = $"Configured={provider.Issuer}, Sample={responseIssuer}"
        });

        var recipient = doc.SelectSingleNode("//*[local-name()='SubjectConfirmationData']")?.Attributes?["Recipient"]?.Value ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(provider.SamlAcsUrl))
        {
            checks.Add(new SamlHardeningCheck
            {
                Name = "sample_response_acs_match",
                Passed = string.IsNullOrWhiteSpace(recipient) || string.Equals(provider.SamlAcsUrl.Trim(), recipient.Trim(), StringComparison.OrdinalIgnoreCase),
                Detail = $"Configured={provider.SamlAcsUrl}, Sample={recipient}"
            });
        }

        var inResponseTo = doc.SelectSingleNode("//*[local-name()='Response']")?.Attributes?["InResponseTo"]?.Value ?? string.Empty;
        if (!provider.SamlAllowIdpInitiated)
        {
            checks.Add(new SamlHardeningCheck
            {
                Name = "sample_response_requires_in_response_to",
                Passed = !string.IsNullOrWhiteSpace(inResponseTo),
                Detail = string.IsNullOrWhiteSpace(inResponseTo) ? "Missing InResponseTo." : inResponseTo
            });
        }
    }

    private async Task<string> ResolveMetadataXmlAsync(IdentityProviderConfig provider, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(provider.SamlMetadataXml))
            return provider.SamlMetadataXml;

        if (string.IsNullOrWhiteSpace(provider.AuthorityOrMetadataUrl))
            return string.Empty;

        try
        {
            var client = _httpClientFactory.CreateClient();
            using var response = await client.GetAsync(provider.AuthorityOrMetadataUrl.Trim(), cancellationToken);
            if (!response.IsSuccessStatusCode)
                return string.Empty;
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string DecodeSamlResponse(string samlResponse)
    {
        if (string.IsNullOrWhiteSpace(samlResponse))
            return string.Empty;

        var trimmed = samlResponse.Trim();
        if (trimmed.StartsWith("<", StringComparison.Ordinal))
            return trimmed;

        var bytes = Convert.FromBase64String(trimmed);
        return Encoding.UTF8.GetString(bytes);
    }
}
