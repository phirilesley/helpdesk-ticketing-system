using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.IdentityModel.Tokens;
using System.IdentityModel.Services;
using System.IdentityModel.Metadata;
using System.ServiceModel.Security;
using System.ServiceModel.Security.Tokens;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Web;
using Microsoft.Extensions.Logging;

namespace HelpDeskSystem.API.Services;

public interface IAdvancedSamlService
{
    Task<SamlResponse> ProcessSamlResponseAsync(string samlResponse, string relayState = null);
    Task<string> GenerateSamlRequestAsync(string identityProviderId, string returnUrl);
    Task<SamlMetadata> GetProviderMetadataAsync(string identityProviderId);
    Task<bool> ValidateSamlResponseAsync(string samlResponse, string identityProviderId);
    Task<SamlAssertion> ExtractAssertionAsync(string samlResponse);
    Task<Dictionary<string, string>> ExtractAttributesAsync(SamlAssertion assertion);
    Task<bool> ValidateSignatureAsync(string samlResponse, X509Certificate2 certificate);
    Task<bool> ValidateConditionsAsync(SamlAssertion assertion);
    Task<bool> ValidateAudienceRestrictionAsync(SamlAssertion assertion, string audience);
    Task<bool> ValidateTimeRestrictionsAsync(SamlAssertion assertion);
    Task<X509Certificate2> GetProviderCertificateAsync(string identityProviderId);
    Task<List<SamlIdentityProvider>> GetConfiguredProvidersAsync();
    Task<SamlIdentityProvider> GetProviderByIdAsync(string identityProviderId);
    Task<bool> TestProviderConnectionAsync(string identityProviderId);
}

public class AdvancedSamlService : IAdvancedSamlService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AdvancedSamlService> _logger;
    private readonly SamlOptions _options;

    public AdvancedSamlService(
        IHttpClientFactory httpClientFactory,
        ILogger<AdvancedSamlService> logger,
        IOptions<SamlOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<SamlResponse> ProcessSamlResponseAsync(string samlResponse, string relayState = null)
    {
        try
        {
            _logger.LogInformation("Processing SAML response");

            // Decode and deserialize SAML response
            var decodedResponse = Convert.FromBase64String(samlResponse);
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(Encoding.UTF8.GetString(decodedResponse));

            var response = new SamlResponse
            {
                Xml = xmlDocument,
                RelayState = relayState,
                Issuer = GetIssuer(xmlDocument),
                Destination = GetDestination(xmlDocument),
                IssueInstant = GetIssueInstant(xmlDocument),
                Assertion = ExtractAssertionFromResponse(xmlDocument)
            };

            // Validate the response
            if (!await ValidateSamlResponseAsync(samlResponse, response.Issuer))
            {
                throw new SecurityTokenValidationException("SAML response validation failed");
            }

            _logger.LogInformation("SAML response processed successfully");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process SAML response");
            throw;
        }
    }

    public async Task<string> GenerateSamlRequestAsync(string identityProviderId, string returnUrl)
    {
        try
        {
            var provider = await GetProviderByIdAsync(identityProviderId);
            if (provider == null)
                throw new ArgumentException($"Identity provider {identityProviderId} not found");

            var requestId = GenerateRequestId();
            var issueInstant = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

            var samlRequest = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<samlp:AuthnRequest 
    xmlns:samlp=""urn:oasis:names:tc:SAML:2.0:protocol""
    xmlns:saml=""urn:oasis:names:tc:SAML:2.0:assertion""
    ID=""{requestId}""
    Version=""2.0""
    IssueInstant=""{issueInstant}""
    Destination=""{provider.SsoUrl}""
    AssertionConsumerServiceURL=""{returnUrl}"">
    <saml:Issuer>{_options.ServiceProviderEntityId}</saml:Issuer>
    <samlp:NameIDPolicy Format=""{provider.NameIdFormat}"" AllowCreate=""true""/>
    <samlp:RequestedAuthnContext Comparison=""exact"">
        <saml:AuthnContextClassRef>urn:oasis:names:tc:SAML:2.0:ac:classes:PasswordProtectedTransport</saml:AuthnContextClassRef>
    </samlp:RequestedAuthnContext>
</samlp:AuthnRequest>";

            // Encode the request
            var encodedRequest = Convert.ToBase64String(Encoding.UTF8.GetBytes(samlRequest));
            var urlEncodedRequest = HttpUtility.UrlEncode(encodedRequest);

            var authUrl = $"{provider.SsoUrl}?SAMLRequest={urlEncodedRequest}";
            
            if (!string.IsNullOrEmpty(provider.RelayStateParameter))
            {
                authUrl += $"&RelayState={HttpUtility.UrlEncode(returnUrl)}";
            }

            _logger.LogInformation("Generated SAML request for provider {ProviderId}", identityProviderId);
            return authUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate SAML request for provider {ProviderId}", identityProviderId);
            throw;
        }
    }

    public async Task<SamlMetadata> GetProviderMetadataAsync(string identityProviderId)
    {
        try
        {
            var provider = await GetProviderByIdAsync(identityProviderId);
            if (provider == null)
                throw new ArgumentException($"Identity provider {identityProviderId} not found");

            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync(provider.MetadataUrl);
            response.EnsureSuccessStatusCode();

            var metadataXml = await response.Content.ReadAsStringAsync();
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(metadataXml);

            var metadata = ParseMetadata(xmlDocument, provider);
            
            _logger.LogInformation("Retrieved metadata for provider {ProviderId}", identityProviderId);
            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve metadata for provider {ProviderId}", identityProviderId);
            throw;
        }
    }

    public async Task<bool> ValidateSamlResponseAsync(string samlResponse, string identityProviderId)
    {
        try
        {
            var provider = await GetProviderByIdAsync(identityProviderId);
            if (provider == null)
                return false;

            var certificate = await GetProviderCertificateAsync(identityProviderId);
            if (certificate == null)
                return false;

            // Decode response
            var decodedResponse = Convert.FromBase64String(samlResponse);
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(Encoding.UTF8.GetString(decodedResponse));

            // Validate signature
            if (!ValidateSignatureAsync(samlResponse, certificate).Result)
            {
                _logger.LogWarning("SAML response signature validation failed");
                return false;
            }

            // Extract and validate assertion
            var assertion = ExtractAssertionFromResponse(xmlDocument);
            if (assertion == null)
            {
                _logger.LogWarning("No assertion found in SAML response");
                return false;
            }

            // Validate conditions
            if (!ValidateConditionsAsync(assertion))
            {
                _logger.LogWarning("SAML assertion conditions validation failed");
                return false;
            }

            // Validate audience restriction
            if (!ValidateAudienceRestrictionAsync(assertion, _options.ServiceProviderEntityId))
            {
                _logger.LogWarning("SAML assertion audience restriction validation failed");
                return false;
            }

            // Validate time restrictions
            if (!ValidateTimeRestrictionsAsync(assertion))
            {
                _logger.LogWarning("SAML assertion time restrictions validation failed");
                return false;
            }

            // Provider-specific validations
            if (!await ValidateProviderSpecificRules(assertion, provider))
            {
                _logger.LogWarning("Provider-specific validation failed");
                return false;
            }

            _logger.LogInformation("SAML response validation successful");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SAML response validation failed");
            return false;
        }
    }

    public async Task<SamlAssertion> ExtractAssertionAsync(string samlResponse)
    {
        try
        {
            var decodedResponse = Convert.FromBase64String(samlResponse);
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(Encoding.UTF8.GetString(decodedResponse));

            var assertion = ExtractAssertionFromResponse(xmlDocument);
            if (assertion == null)
                throw new InvalidOperationException("No assertion found in SAML response");

            return assertion;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract assertion from SAML response");
            throw;
        }
    }

    public async Task<Dictionary<string, string>> ExtractAttributesAsync(SamlAssertion assertion)
    {
        try
        {
            var attributes = new Dictionary<string, string>();

            if (assertion.AttributeStatements != null)
            {
                foreach (var attributeStatement in assertion.AttributeStatements)
                {
                    foreach (var attribute in attributeStatement.Attributes)
                    {
                        var values = attribute.Values?.Select(v => v.Value).ToArray() ?? new string[0];
                        var value = values.Length == 1 ? values[0] : string.Join(",", values);
                        
                        attributes[attribute.Name] = value;
                    }
                }
            }

            _logger.LogInformation("Extracted {Count} attributes from SAML assertion", attributes.Count);
            return attributes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract attributes from SAML assertion");
            throw;
        }
    }

    public async Task<bool> ValidateSignatureAsync(string samlResponse, X509Certificate2 certificate)
    {
        try
        {
            var decodedResponse = Convert.FromBase64String(samlResponse);
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(Encoding.UTF8.GetString(decodedResponse));

            // Find the signature element
            var signatureNode = xmlDocument.SelectSingleNode("//ds:Signature", GetNamespaceManager(xmlDocument));
            if (signatureNode == null)
            {
                _logger.LogWarning("No signature found in SAML response");
                return false;
            }

            // Load the signature
            var signedXml = new SignedXml(xmlDocument);
            signedXml.LoadXml((XmlElement)signatureNode);

            // Verify the signature
            var isValid = signedXml.CheckSignature(certificate, true);
            
            _logger.LogInformation("Signature validation result: {IsValid}", isValid);
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate SAML signature");
            return false;
        }
    }

    public async Task<bool> ValidateConditionsAsync(SamlAssertion assertion)
    {
        try
        {
            if (assertion.Conditions == null)
                return true;

            var now = DateTime.UtcNow;

            // Validate NotBefore
            if (assertion.Conditions.NotBefore.HasValue && now < assertion.Conditions.NotBefore.Value)
            {
                _logger.LogWarning("SAML assertion is not yet valid (NotBefore: {NotBefore}, Current: {Current})", 
                    assertion.Conditions.NotBefore.Value, now);
                return false;
            }

            // Validate NotOnOrAfter
            if (assertion.Conditions.NotOnOrAfter.HasValue && now >= assertion.Conditions.NotOnOrAfter.Value)
            {
                _logger.LogWarning("SAML assertion has expired (NotOnOrAfter: {NotOnOrAfter}, Current: {Current})", 
                    assertion.Conditions.NotOnOrAfter.Value, now);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate SAML conditions");
            return false;
        }
    }

    public async Task<bool> ValidateAudienceRestrictionAsync(SamlAssertion assertion, string audience)
    {
        try
        {
            if (assertion.Conditions?.AudienceRestrictions == null)
                return true;

            var audienceRestrictions = assertion.Conditions.AudienceRestrictions;
            var allowedAudiences = audienceRestrictions
                .SelectMany(ar => ar.Audiences)
                .Select(a => a.Uri)
                .ToList();

            if (!allowedAudiences.Contains(audience))
            {
                _logger.LogWarning("SAML assertion audience restriction failed. Expected: {Expected}, Allowed: {Allowed}", 
                    audience, string.Join(", ", allowedAudiences));
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate SAML audience restriction");
            return false;
        }
    }

    public async Task<bool> ValidateTimeRestrictionsAsync(SamlAssertion assertion)
    {
        try
        {
            var now = DateTime.UtcNow;

            // Validate SubjectConfirmationData
            if (assertion.Subject?.SubjectConfirmations != null)
            {
                foreach (var subjectConfirmation in assertion.Subject.SubjectConfirmations)
                {
                    var subjectConfirmationData = subjectConfirmation.SubjectConfirmationData;
                    if (subjectConfirmationData != null)
                    {
                        // Validate NotBefore
                        if (subjectConfirmationData.NotBefore.HasValue && now < subjectConfirmationData.NotBefore.Value)
                        {
                            _logger.LogWarning("SubjectConfirmation is not yet valid");
                            return false;
                        }

                        // Validate NotOnOrAfter
                        if (subjectConfirmationData.NotOnOrAfter.HasValue && now >= subjectConfirmationData.NotOnOrAfter.Value)
                        {
                            _logger.LogWarning("SubjectConfirmation has expired");
                            return false;
                        }

                        // Validate Recipient
                        if (!string.IsNullOrEmpty(subjectConfirmationData.Recipient) && 
                            !subjectConfirmationData.Recipient.Equals(_options.ServiceProviderEntityId, StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogWarning("SubjectConfirmation recipient mismatch");
                            return false;
                        }
                    }
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate SAML time restrictions");
            return false;
        }
    }

    public async Task<X509Certificate2> GetProviderCertificateAsync(string identityProviderId)
    {
        try
        {
            var provider = await GetProviderByIdAsync(identityProviderId);
            if (provider == null)
                return null;

            if (!string.IsNullOrEmpty(provider.CertificatePath))
            {
                // Load certificate from file
                return new X509Certificate2(provider.CertificatePath, provider.CertificatePassword);
            }
            else if (!string.IsNullOrEmpty(provider.CertificateThumbprint))
            {
                // Load certificate from certificate store
                var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadOnly);
                
                var certs = store.Certificates.Find(X509FindType.FindByThumbprint, provider.CertificateThumbprint, false);
                store.Close();

                return certs.Count > 0 ? certs[0] : null;
            }
            else if (!string.IsNullOrEmpty(provider.MetadataUrl))
            {
                // Extract certificate from metadata
                var metadata = await GetProviderMetadataAsync(identityProviderId);
                return metadata.SigningCertificates?.FirstOrDefault();
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get certificate for provider {ProviderId}", identityProviderId);
            return null;
        }
    }

    public async Task<List<SamlIdentityProvider>> GetConfiguredProvidersAsync()
    {
        try
        {
            return _options.IdentityProviders ?? new List<SamlIdentityProvider>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get configured SAML providers");
            return new List<SamlIdentityProvider>();
        }
    }

    public async Task<SamlIdentityProvider> GetProviderByIdAsync(string identityProviderId)
    {
        try
        {
            var providers = await GetConfiguredProvidersAsync();
            return providers.FirstOrDefault(p => p.Id.Equals(identityProviderId, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get SAML provider by ID {ProviderId}", identityProviderId);
            return null;
        }
    }

    public async Task<bool> TestProviderConnectionAsync(string identityProviderId)
    {
        try
        {
            var provider = await GetProviderByIdAsync(identityProviderId);
            if (provider == null)
                return false;

            // Test metadata URL
            if (!string.IsNullOrEmpty(provider.MetadataUrl))
            {
                var httpClient = _httpClientFactory.CreateClient();
                var response = await httpClient.GetAsync(provider.MetadataUrl);
                if (!response.IsSuccessStatusCode)
                    return false;
            }

            // Test certificate
            var certificate = await GetProviderCertificateAsync(identityProviderId);
            if (certificate == null)
                return false;

            // Test SSO URL
            if (!string.IsNullOrEmpty(provider.SsoUrl))
            {
                var httpClient = _httpClientFactory.CreateClient();
                var response = await httpClient.GetAsync(provider.SsoUrl);
                if (!response.IsSuccessStatusCode)
                    return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test provider connection for {ProviderId}", identityProviderId);
            return false;
        }
    }

    private async Task<bool> ValidateProviderSpecificRules(SamlAssertion assertion, SamlIdentityProvider provider)
    {
        try
        {
            // Provider-specific validation rules
            switch (provider.Type.ToLowerInvariant())
            {
                case "adfs":
                    return await ValidateAdfsRules(assertion, provider);
                case "okta":
                    return await ValidateOktaRules(assertion, provider);
                case "shibboleth":
                    return await ValidateShibbolethRules(assertion, provider);
                case "ping":
                    return await ValidatePingRules(assertion, provider);
                case "auth0":
                    return await ValidateAuth0Rules(assertion, provider);
                case "azuread":
                    return await ValidateAzureAdRules(assertion, provider);
                case "keycloak":
                    return await ValidateKeycloakRules(assertion, provider);
                default:
                    return true; // No specific rules for unknown providers
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate provider-specific rules for {ProviderType}", provider.Type);
            return false;
        }
    }

    private async Task<bool> ValidateAdfsRules(SamlAssertion assertion, SamlIdentityProvider provider)
    {
        // ADFS-specific validations
        // Check for UPN claim
        var attributes = await ExtractAttributesAsync(assertion);
        if (!attributes.ContainsKey("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn"))
        {
            _logger.LogWarning("ADFS assertion missing UPN claim");
            return false;
        }

        // Validate ADFS-specific attributes
        var requiredAttributes = new[] { "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" };
        foreach (var attr in requiredAttributes)
        {
            if (!attributes.ContainsKey(attr))
            {
                _logger.LogWarning("ADFS assertion missing required attribute: {Attribute}", attr);
                return false;
            }
        }

        return true;
    }

    private async Task<bool> ValidateOktaRules(SamlAssertion assertion, SamlIdentityProvider provider)
    {
        // Okta-specific validations
        var attributes = await ExtractAttributesAsync(assertion);
        
        // Check for Okta-specific attributes
        var oktaAttributes = new[] { "okta_id", "okta_session_expires_at" };
        foreach (var attr in oktaAttributes)
        {
            if (!attributes.ContainsKey(attr))
            {
                _logger.LogWarning("Okta assertion missing attribute: {Attribute}", attr);
                return false;
            }
        }

        // Validate Okta session expiration
        if (attributes.TryGetValue("okta_session_expires_at", out var sessionExpires))
        {
            if (long.TryParse(sessionExpires, out var timestamp))
            {
                var expirationTime = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
                if (DateTime.UtcNow > expirationTime)
                {
                    _logger.LogWarning("Okta session has expired");
                    return false;
                }
            }
        }

        return true;
    }

    private async Task<bool> ValidateShibbolethRules(SamlAssertion assertion, SamlIdentityProvider provider)
    {
        // Shibboleth-specific validations
        var attributes = await ExtractAttributesAsync(assertion);
        
        // Check for Shibboleth-specific attributes
        var shibbolethAttributes = new[] { "eduPersonPrincipalName", "eduPersonAffiliation" };
        foreach (var attr in shibbolethAttributes)
        {
            if (!attributes.ContainsKey(attr))
            {
                _logger.LogWarning("Shibboleth assertion missing attribute: {Attribute}", attr);
                return false;
            }
        }

        // Validate eduPersonPrincipalName format
        if (attributes.TryGetValue("eduPersonPrincipalName", out var eppn))
        {
            if (!eppn.Contains("@"))
            {
                _logger.LogWarning("Invalid eduPersonPrincipalName format: {EPPN}", eppn);
                return false;
            }
        }

        return true;
    }

    private async Task<bool> ValidatePingRules(SamlAssertion assertion, SamlIdentityProvider provider)
    {
        // Ping Identity-specific validations
        var attributes = await ExtractAttributesAsync(assertion);
        
        // Check for Ping-specific attributes
        var pingAttributes = new[] { "ping_one_id", "ping_one_email_verified" };
        foreach (var attr in pingAttributes)
        {
            if (!attributes.ContainsKey(attr))
            {
                _logger.LogWarning("Ping assertion missing attribute: {Attribute}", attr);
                return false;
            }
        }

        return true;
    }

    private async Task<bool> ValidateAuth0Rules(SamlAssertion assertion, SamlIdentityProvider provider)
    {
        // Auth0-specific validations
        var attributes = await ExtractAttributesAsync(assertion);
        
        // Check for Auth0-specific attributes
        var auth0Attributes = new[] { "auth0_user_id", "auth0_email_verified" };
        foreach (var attr in auth0Attributes)
        {
            if (!attributes.ContainsKey(attr))
            {
                _logger.LogWarning("Auth0 assertion missing attribute: {Attribute}", attr);
                return false;
            }
        }

        // Validate Auth0 user ID format
        if (attributes.TryGetValue("auth0_user_id", out var userId))
        {
            if (!userId.StartsWith("auth0|"))
            {
                _logger.LogWarning("Invalid Auth0 user ID format: {UserId}", userId);
                return false;
            }
        }

        return true;
    }

    private async Task<bool> ValidateAzureAdRules(SamlAssertion assertion, SamlIdentityProvider provider)
    {
        // Azure AD-specific validations
        var attributes = await ExtractAttributesAsync(assertion);
        
        // Check for Azure AD-specific attributes
        var azureAttributes = new[] { "http://schemas.microsoft.com/claims/authnclassreference", "http://schemas.microsoft.com/claims/authnmethodsreferences" };
        foreach (var attr in azureAttributes)
        {
            if (!attributes.ContainsKey(attr))
            {
                _logger.LogWarning("Azure AD assertion missing attribute: {Attribute}", attr);
                return false;
            }
        }

        // Validate Azure AD authentication method
        if (attributes.TryGetValue("http://schemas.microsoft.com/claims/authnclassreference", out var authClass))
        {
            var validAuthClasses = new[] { "1", "2" }; // 1=Password, 2=MFA
            if (!validAuthClasses.Contains(authClass))
            {
                _logger.LogWarning("Invalid Azure AD authentication class: {AuthClass}", authClass);
                return false;
            }
        }

        return true;
    }

    private async Task<bool> ValidateKeycloakRules(SamlAssertion assertion, SamlIdentityProvider provider)
    {
        // Keycloak-specific validations
        var attributes = await ExtractAttributesAsync(assertion);
        
        // Check for Keycloak-specific attributes
        var keycloakAttributes = new[] { "keycloak_security_context", "keycloak_idp_hint" };
        foreach (var attr in keycloakAttributes)
        {
            if (!attributes.ContainsKey(attr))
            {
                _logger.LogWarning("Keycloak assertion missing attribute: {Attribute}", attr);
                return false;
            }
        }

        return true;
    }

    private string GenerateRequestId()
    {
        return $"_{Guid.NewGuid():N}";
    }

    private string GetIssuer(XmlDocument xmlDocument)
    {
        var issuerNode = xmlDocument.SelectSingleNode("//saml:Issuer", GetNamespaceManager(xmlDocument));
        return issuerNode?.InnerText;
    }

    private string GetDestination(XmlDocument xmlDocument)
    {
        var responseNode = xmlDocument.SelectSingleNode("//samlp:Response", GetNamespaceManager(xmlDocument));
        return responseNode?.Attributes?["Destination"]?.Value;
    }

    private DateTime GetIssueInstant(XmlDocument xmlDocument)
    {
        var responseNode = xmlDocument.SelectSingleNode("//samlp:Response", GetNamespaceManager(xmlDocument));
        var issueInstantStr = responseNode?.Attributes?["IssueInstant"]?.Value;
        
        if (DateTime.TryParse(issueInstantStr, out var issueInstant))
            return issueInstant;
        
        return DateTime.UtcNow;
    }

    private SamlAssertion ExtractAssertionFromResponse(XmlDocument xmlDocument)
    {
        var assertionNode = xmlDocument.SelectSingleNode("//saml:Assertion", GetNamespaceManager(xmlDocument));
        if (assertionNode == null)
            return null;

        return ParseAssertion(assertionNode as XmlElement);
    }

    private SamlAssertion ParseAssertion(XmlElement assertionElement)
    {
        var assertion = new SamlAssertion
        {
            Id = assertionElement.Attributes?["ID"]?.Value,
            IssueInstant = DateTime.Parse(assertionElement.Attributes?["IssueInstant"]?.Value),
            Version = assertionElement.Attributes?["Version"]?.Value,
            Issuer = assertionElement.SelectSingleNode("saml:Issuer", GetNamespaceManager(assertionElement.OwnerDocument))?.InnerText,
        };

        // Parse Subject
        var subjectNode = assertionElement.SelectSingleNode("saml:Subject", GetNamespaceManager(assertionElement.OwnerDocument));
        if (subjectNode != null)
        {
            assertion.Subject = ParseSubject(subjectNode as XmlElement);
        }

        // Parse Conditions
        var conditionsNode = assertionElement.SelectSingleNode("saml:Conditions", GetNamespaceManager(assertionElement.OwnerDocument));
        if (conditionsNode != null)
        {
            assertion.Conditions = ParseConditions(conditionsNode as XmlElement);
        }

        // Parse AttributeStatements
        var attributeStatementNodes = assertionElement.SelectNodes("saml:AttributeStatement", GetNamespaceManager(assertionElement.OwnerDocument));
        if (attributeStatementNodes != null)
        {
            assertion.AttributeStatements = new List<SamlAttributeStatement>();
            foreach (XmlNode node in attributeStatementNodes)
            {
                assertion.AttributeStatements.Add(ParseAttributeStatement(node as XmlElement));
            }
        }

        return assertion;
    }

    private SamlSubject ParseSubject(XmlElement subjectElement)
    {
        var subject = new SamlSubject();

        var nameIdNode = subjectElement.SelectSingleNode("saml:NameID", GetNamespaceManager(subjectElement.OwnerDocument));
        if (nameIdNode != null)
        {
            subject.NameId = new SamlNameId
            {
                Format = nameIdNode.Attributes?["Format"]?.Value,
                Value = nameIdNode.InnerText
            };
        }

        var subjectConfirmationNodes = subjectElement.SelectNodes("saml:SubjectConfirmation", GetNamespaceManager(subjectElement.OwnerDocument));
        if (subjectConfirmationNodes != null)
        {
            subject.SubjectConfirmations = new List<SamlSubjectConfirmation>();
            foreach (XmlNode node in subjectConfirmationNodes)
            {
                subject.SubjectConfirmations.Add(ParseSubjectConfirmation(node as XmlElement));
            }
        }

        return subject;
    }

    private SamlSubjectConfirmation ParseSubjectConfirmation(XmlElement subjectConfirmationElement)
    {
        var confirmation = new SamlSubjectConfirmation
        {
            Method = subjectConfirmationElement.Attributes?["Method"]?.Value
        };

        var subjectConfirmationDataNode = subjectConfirmationElement.SelectSingleNode("saml:SubjectConfirmationData", GetNamespaceManager(subjectConfirmationElement.OwnerDocument));
        if (subjectConfirmationDataNode != null)
        {
            confirmation.SubjectConfirmationData = ParseSubjectConfirmationData(subjectConfirmationDataNode as XmlElement);
        }

        return confirmation;
    }

    private SamlSubjectConfirmationData ParseSubjectConfirmationData(XmlElement subjectConfirmationDataElement)
    {
        var data = new SamlSubjectConfirmationData
        {
            NotBefore = ParseDateTime(subjectConfirmationDataElement.Attributes?["NotBefore"]?.Value),
            NotOnOrAfter = ParseDateTime(subjectConfirmationDataElement.Attributes?["NotOnOrAfter"]?.Value),
            Recipient = subjectConfirmationDataElement.Attributes?["Recipient"]?.Value
        };

        return data;
    }

    private SamlConditions ParseConditions(XmlElement conditionsElement)
    {
        var conditions = new SamlConditions
        {
            NotBefore = ParseDateTime(conditionsElement.Attributes?["NotBefore"]?.Value),
            NotOnOrAfter = ParseDateTime(conditionsElement.Attributes?["NotOnOrAfter"]?.Value)
        };

        var audienceRestrictionNodes = conditionsElement.SelectNodes("saml:AudienceRestriction", GetNamespaceManager(conditionsElement.OwnerDocument));
        if (audienceRestrictionNodes != null)
        {
            conditions.AudienceRestrictions = new List<SamlAudienceRestriction>();
            foreach (XmlNode node in audienceRestrictionNodes)
            {
                conditions.AudienceRestrictions.Add(ParseAudienceRestriction(node as XmlElement));
            }
        }

        return conditions;
    }

    private SamlAudienceRestriction ParseAudienceRestriction(XmlElement audienceRestrictionElement)
    {
        var restriction = new SamlAudienceRestriction
        {
            Audiences = new List<SamlAudience>()
        };

        var audienceNodes = audienceRestrictionElement.SelectNodes("saml:Audience", GetNamespaceManager(audienceRestrictionElement.OwnerDocument));
        if (audienceNodes != null)
        {
            foreach (XmlNode node in audienceNodes)
            {
                restriction.Audiences.Add(new SamlAudience { Uri = node.InnerText });
            }
        }

        return restriction;
    }

    private SamlAttributeStatement ParseAttributeStatement(XmlElement attributeStatementElement)
    {
        var statement = new SamlAttributeStatement
        {
            Attributes = new List<SamlAttribute>()
        };

        var attributeNodes = attributeStatementElement.SelectNodes("saml:Attribute", GetNamespaceManager(attributeStatementElement.OwnerDocument));
        if (attributeNodes != null)
        {
            foreach (XmlNode node in attributeNodes)
            {
                statement.Attributes.Add(ParseAttribute(node as XmlElement));
            }
        }

        return statement;
    }

    private SamlAttribute ParseAttribute(XmlElement attributeElement)
    {
        var attribute = new SamlAttribute
        {
            Name = attributeElement.Attributes?["Name"]?.Value,
            NameFormat = attributeElement.Attributes?["NameFormat"]?.Value,
            Values = new List<SamlAttributeValue>()
        };

        var attributeValueNodes = attributeElement.SelectNodes("saml:AttributeValue", GetNamespaceManager(attributeElement.OwnerDocument));
        if (attributeValueNodes != null)
        {
            foreach (XmlNode node in attributeValueNodes)
            {
                attribute.Values.Add(new SamlAttributeValue { Value = node.InnerText });
            }
        }

        return attribute;
    }

    private SamlMetadata ParseMetadata(XmlDocument metadataXml, SamlIdentityProvider provider)
    {
        var metadata = new SamlMetadata
        {
            EntityId = metadataXml.SelectSingleNode("//md:EntityDescriptor", GetNamespaceManager(metadataXml))?.Attributes?["entityID"]?.Value,
            SigningCertificates = new List<X509Certificate2>()
        };

        // Extract signing certificates
        var keyDescriptorNodes = metadataXml.SelectNodes("//md:KeyDescriptor[@use='signing']/ds:KeyInfo/ds:X509Data/ds:X509Certificate", GetNamespaceManager(metadataXml));
        if (keyDescriptorNodes != null)
        {
            foreach (XmlNode node in keyDescriptorNodes)
            {
                var certData = Convert.FromBase64String(node.InnerText);
                metadata.SigningCertificates.Add(new X509Certificate2(certData));
            }
        }

        // Extract SSO services
        var ssoServiceNodes = metadataXml.SelectNodes("//md:IDPSSODescriptor/md:SingleSignOnService", GetNamespaceManager(metadataXml));
        if (ssoServiceNodes != null)
        {
            metadata.SsoServices = new List<SamlSsoService>();
            foreach (XmlNode node in ssoServiceNodes)
            {
                metadata.SsoServices.Add(new SamlSsoService
                {
                    Binding = node.Attributes?["Binding"]?.Value,
                    Location = node.Attributes?["Location"]?.Value
                });
            }
        }

        return metadata;
    }

    private XmlNamespaceManager GetNamespaceManager(XmlDocument xmlDocument)
    {
        var namespaceManager = new XmlNamespaceManager(xmlDocument.NameTable);
        namespaceManager.AddNamespace("saml", "urn:oasis:names:tc:SAML:2.0:assertion");
        namespaceManager.AddNamespace("samlp", "urn:oasis:names:tc:SAML:2.0:protocol");
        namespaceManager.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#");
        namespaceManager.AddNamespace("md", "urn:oasis:names:tc:SAML:2.0:metadata");
        return namespaceManager;
    }

    private DateTime? ParseDateTime(string dateTimeStr)
    {
        if (string.IsNullOrEmpty(dateTimeStr))
            return null;

        if (DateTime.TryParse(dateTimeStr, out var dateTime))
            return dateTime;

        return null;
    }
}

// Supporting DTOs
public class SamlOptions
{
    public string ServiceProviderEntityId { get; set; }
    public string AssertionConsumerServiceUrl { get; set; }
    public string SingleLogoutServiceUrl { get; set; }
    public List<SamlIdentityProvider> IdentityProviders { get; set; }
}

public class SamlIdentityProvider
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public string MetadataUrl { get; set; }
    public string SsoUrl { get; set; }
    public string SloUrl { get; set; }
    public string NameIdFormat { get; set; }
    public string CertificatePath { get; set; }
    public string CertificatePassword { get; set; }
    public string CertificateThumbprint { get; set; }
    public string RelayStateParameter { get; set; }
    public Dictionary<string, string> CustomSettings { get; set; }
}

public class SamlResponse
{
    public XmlDocument Xml { get; set; }
    public string RelayState { get; set; }
    public string Issuer { get; set; }
    public string Destination { get; set; }
    public DateTime IssueInstant { get; set; }
    public SamlAssertion Assertion { get; set; }
}

public class SamlAssertion
{
    public string Id { get; set; }
    public string Version { get; set; }
    public string Issuer { get; set; }
    public DateTime IssueInstant { get; set; }
    public SamlSubject Subject { get; set; }
    public SamlConditions Conditions { get; set; }
    public List<SamlAttributeStatement> AttributeStatements { get; set; }
}

public class SamlSubject
{
    public SamlNameId NameId { get; set; }
    public List<SamlSubjectConfirmation> SubjectConfirmations { get; set; }
}

public class SamlNameId
{
    public string Format { get; set; }
    public string Value { get; set; }
}

public class SamlSubjectConfirmation
{
    public string Method { get; set; }
    public SamlSubjectConfirmationData SubjectConfirmationData { get; set; }
}

public class SamlSubjectConfirmationData
{
    public DateTime? NotBefore { get; set; }
    public DateTime? NotOnOrAfter { get; set; }
    public string Recipient { get; set; }
}

public class SamlConditions
{
    public DateTime? NotBefore { get; set; }
    public DateTime? NotOnOrAfter { get; set; }
    public List<SamlAudienceRestriction> AudienceRestrictions { get; set; }
}

public class SamlAudienceRestriction
{
    public List<SamlAudience> Audiences { get; set; }
}

public class SamlAudience
{
    public string Uri { get; set; }
}

public class SamlAttributeStatement
{
    public List<SamlAttribute> Attributes { get; set; }
}

public class SamlAttribute
{
    public string Name { get; set; }
    public string NameFormat { get; set; }
    public List<SamlAttributeValue> Values { get; set; }
}

public class SamlAttributeValue
{
    public string Value { get; set; }
}

public class SamlMetadata
{
    public string EntityId { get; set; }
    public List<X509Certificate2> SigningCertificates { get; set; }
    public List<SamlSsoService> SsoServices { get; set; }
}

public class SamlSsoService
{
    public string Binding { get; set; }
    public string Location { get; set; }
}
