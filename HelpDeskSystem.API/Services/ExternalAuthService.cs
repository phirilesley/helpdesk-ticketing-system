using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Web;
using System.Xml;
using HelpDeskSystem.Application.DTOs.Users;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Domain.Enums;
using HelpDeskSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace HelpDeskSystem.API.Services
{
    public interface IExternalAuthService
    {
        Task<AuthResult> AuthenticateWithOidcAsync(
            int providerId,
            string code,
            string redirectUri,
            string deviceId,
            string deviceName,
            string ipAddress,
            string userAgent,
            CancellationToken cancellationToken = default);

        Task<AuthResult> AuthenticateWithSamlAsync(
            int providerId,
            string samlResponse,
            string deviceId,
            string deviceName,
            string ipAddress,
            string userAgent,
            CancellationToken cancellationToken = default);

        Task<string> GenerateAuthUrlAsync(
            int providerId,
            string redirectUri,
            string? codeChallenge = null,
            string? state = null,
            CancellationToken cancellationToken = default);
        Task<IdentityProviderConfig> GetProviderAsync(int providerId, CancellationToken cancellationToken = default);
    }

    public class AuthResult
    {
        public bool Success { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public User? User { get; set; }
        public string? Error { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
    }

    public class ExternalAuthService : IExternalAuthService
    {
        private readonly HelpDeskDbContext _context;
        private readonly ITokenService _tokenService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ExternalAuthService> _logger;

        public ExternalAuthService(
            HelpDeskDbContext context,
            ITokenService tokenService,
            IRefreshTokenService refreshTokenService,
            IHttpClientFactory httpClientFactory,
            ILogger<ExternalAuthService> logger)
        {
            _context = context;
            _tokenService = tokenService;
            _refreshTokenService = refreshTokenService;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<IdentityProviderConfig> GetProviderAsync(int providerId, CancellationToken cancellationToken = default)
        {
            var provider = await _context.IdentityProviderConfigs
                .FirstOrDefaultAsync(p => p.Id == providerId && p.IsEnabled && !p.IsDeleted, cancellationToken);

            if (provider == null)
                throw new ArgumentException("Identity provider not found or inactive");

            return provider;
        }

        public async Task<string> GenerateAuthUrlAsync(
            int providerId,
            string redirectUri,
            string? codeChallenge = null,
            string? state = null,
            CancellationToken cancellationToken = default)
        {
            var provider = await GetProviderAsync(providerId, cancellationToken);
            if (provider.Protocol != IdentityProtocol.Oidc)
                throw new NotSupportedException("Only OIDC providers support authorization URL generation.");

            if (!IsRedirectUriAllowed(provider, redirectUri))
                throw new InvalidOperationException("Redirect URI is not allowed for this provider.");

            var oidcConfig = await FetchOidcConfigurationAsync(provider, cancellationToken);

            var query = HttpUtility.ParseQueryString(string.Empty);
            query["client_id"] = provider.ClientId;
            query["response_type"] = "code";
            query["redirect_uri"] = redirectUri;
            query["scope"] = "openid profile email";
            query["state"] = string.IsNullOrWhiteSpace(state) ? Guid.NewGuid().ToString("N") : state.Trim();
            query["nonce"] = Guid.NewGuid().ToString("N");

            if (provider.OidcRequirePkce)
            {
                if (string.IsNullOrWhiteSpace(codeChallenge))
                    throw new InvalidOperationException("Provider requires PKCE code challenge.");

                query["code_challenge"] = codeChallenge.Trim();
                query["code_challenge_method"] = "S256";
            }

            if (!string.IsNullOrWhiteSpace(provider.Audience))
                query["audience"] = provider.Audience;

            return $"{oidcConfig.AuthorizationEndpoint}?{query}";
        }

        public async Task<AuthResult> AuthenticateWithOidcAsync(
            int providerId,
            string code,
            string redirectUri,
            string deviceId,
            string deviceName,
            string ipAddress,
            string userAgent,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var provider = await GetProviderAsync(providerId, cancellationToken);
                if (provider.Protocol != IdentityProtocol.Oidc)
                    return new AuthResult { Success = false, Error = "Provider is not configured for OIDC." };

                if (!IsRedirectUriAllowed(provider, redirectUri))
                    return new AuthResult { Success = false, Error = "Redirect URI is not allowed." };

                var oidcConfig = await FetchOidcConfigurationAsync(provider, cancellationToken);
                var tokenPayload = await ExchangeCodeForTokensAsync(provider, oidcConfig.TokenEndpoint, code, redirectUri, cancellationToken);

                if (!tokenPayload.TryGetProperty("id_token", out var idTokenElement))
                    return new AuthResult { Success = false, Error = "OIDC provider did not return id_token." };

                var idToken = idTokenElement.GetString();
                if (string.IsNullOrWhiteSpace(idToken))
                    return new AuthResult { Success = false, Error = "OIDC id_token is empty." };

                var principal = await ValidateIdTokenAsync(provider, oidcConfig, idToken, cancellationToken);
                var email = principal.FindFirstValue("email")
                    ?? principal.FindFirstValue("preferred_username")
                    ?? principal.FindFirstValue(ClaimTypes.Email)
                    ?? principal.FindFirstValue("upn");

                if (string.IsNullOrWhiteSpace(email))
                    return new AuthResult { Success = false, Error = "Email claim was not found in id_token." };

                var firstName = principal.FindFirstValue("given_name") ?? string.Empty;
                var lastName = principal.FindFirstValue("family_name") ?? string.Empty;
                var fullName = principal.FindFirstValue("name") ?? string.Empty;

                var user = await FindOrCreateTenantUserAsync(provider.TenantId, email, firstName, lastName, fullName, cancellationToken);
                var internalTokens = await GenerateInternalTokensAsync(user, deviceId, deviceName, ipAddress, userAgent, cancellationToken);

                return new AuthResult
                {
                    Success = true,
                    AccessToken = internalTokens.accessToken,
                    RefreshToken = internalTokens.refreshToken,
                    ExpiresAt = internalTokens.expiresAtUtc,
                    User = user,
                    Metadata = new Dictionary<string, string>
                    {
                        ["provider"] = provider.Name,
                        ["protocol"] = provider.Protocol.ToString(),
                        ["issuer"] = oidcConfig.Issuer ?? string.Empty,
                        ["subject"] = principal.FindFirstValue("sub") ?? string.Empty
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OIDC authentication failed for provider {ProviderId}", providerId);
                return new AuthResult { Success = false, Error = "OIDC authentication failed." };
            }
        }

        public async Task<AuthResult> AuthenticateWithSamlAsync(
            int providerId,
            string samlResponse,
            string deviceId,
            string deviceName,
            string ipAddress,
            string userAgent,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var provider = await GetProviderAsync(providerId, cancellationToken);
                if (provider.Protocol != IdentityProtocol.Saml)
                    return new AuthResult { Success = false, Error = "Provider is not configured for SAML." };

                var xml = DecodeSamlResponse(samlResponse);
                var doc = new XmlDocument { PreserveWhitespace = true };
                doc.LoadXml(xml);

                var issuer = ReadFirstNodeInnerText(doc, "//*[local-name()='Issuer']");
                if (provider.EnforceStrictIssuer
                    && !string.IsNullOrWhiteSpace(provider.Issuer)
                    && !string.Equals(provider.Issuer, issuer, StringComparison.OrdinalIgnoreCase))
                    return new AuthResult { Success = false, Error = "SAML issuer mismatch." };

                if (provider.SamlValidateSignature)
                {
                    var signatureValidation = ValidateSamlSignature(doc, provider);
                    if (!signatureValidation.isValid)
                        return new AuthResult { Success = false, Error = signatureValidation.error };
                }

                if (!provider.SamlAllowIdpInitiated)
                {
                    var inResponseTo = ReadFirstAttributeValue(doc, "//*[local-name()='Response']", "InResponseTo");
                    if (string.IsNullOrWhiteSpace(inResponseTo))
                        return new AuthResult { Success = false, Error = "IdP-initiated SAML response is not allowed for this provider." };
                }

                if (!string.IsNullOrWhiteSpace(provider.SamlAcsUrl))
                {
                    var recipient = ReadFirstAttributeValue(doc, "//*[local-name()='SubjectConfirmationData']", "Recipient");
                    if (!string.IsNullOrWhiteSpace(recipient)
                        && !string.Equals(recipient.Trim(), provider.SamlAcsUrl.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        return new AuthResult { Success = false, Error = "SAML recipient/ACS mismatch." };
                    }
                }

                var email = ReadSamlAttribute(doc, "email")
                    ?? ReadSamlAttribute(doc, "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")
                    ?? ReadFirstNodeInnerText(doc, "//*[local-name()='NameID']");

                if (string.IsNullOrWhiteSpace(email))
                    return new AuthResult { Success = false, Error = "SAML assertion does not include email/NameID." };

                var firstName = ReadSamlAttribute(doc, "given_name")
                    ?? ReadSamlAttribute(doc, "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname")
                    ?? string.Empty;
                var lastName = ReadSamlAttribute(doc, "family_name")
                    ?? ReadSamlAttribute(doc, "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname")
                    ?? string.Empty;

                var user = await FindOrCreateTenantUserAsync(provider.TenantId, email, firstName, lastName, string.Empty, cancellationToken);
                var internalTokens = await GenerateInternalTokensAsync(user, deviceId, deviceName, ipAddress, userAgent, cancellationToken);

                return new AuthResult
                {
                    Success = true,
                    AccessToken = internalTokens.accessToken,
                    RefreshToken = internalTokens.refreshToken,
                    ExpiresAt = internalTokens.expiresAtUtc,
                    User = user,
                    Metadata = new Dictionary<string, string>
                    {
                        ["provider"] = provider.Name,
                        ["protocol"] = provider.Protocol.ToString(),
                        ["issuer"] = issuer ?? string.Empty,
                        ["subject"] = email
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SAML authentication failed for provider {ProviderId}", providerId);
                return new AuthResult { Success = false, Error = "SAML authentication failed." };
            }
        }

        private async Task<(string accessToken, string refreshToken, DateTime expiresAtUtc)> GenerateInternalTokensAsync(
            User user,
            string deviceId,
            string deviceName,
            string ipAddress,
            string userAgent,
            CancellationToken cancellationToken)
        {
            var dto = new UserDto
            {
                Id = user.Id,
                Username = string.IsNullOrWhiteSpace(user.Username) ? user.Email : user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsActive = user.IsActive,
                IsMfaEnabled = user.IsMfaEnabled,
                TenantId = user.TenantId,
                Roles = user.UserRoles
                    .Where(ur => ur.Role != null)
                    .Select(ur => ur.Role!.Name)
                    .Distinct()
                    .ToList()
            };

            var tokenResult = _tokenService.Generate(dto);
            var refresh = await _refreshTokenService.CreateAsync(
                user.Id,
                string.IsNullOrWhiteSpace(deviceId) ? "sso-device" : deviceId,
                string.IsNullOrWhiteSpace(deviceName) ? "SSO Device" : deviceName,
                ipAddress,
                userAgent,
                cancellationToken);

            return (tokenResult.AccessToken, refresh.token, tokenResult.ExpiresAtUtc);
        }

        private async Task<User> FindOrCreateTenantUserAsync(
            int tenantId,
            string email,
            string firstName,
            string lastName,
            string fullName,
            CancellationToken cancellationToken)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();

            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Email.ToLower() == normalizedEmail && !u.IsDeleted, cancellationToken);

            if (user == null)
            {
                var names = SplitName(firstName, lastName, fullName);
                user = new User
                {
                    TenantId = tenantId,
                    Email = normalizedEmail,
                    Username = normalizedEmail,
                    PasswordHash = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
                    FirstName = names.first,
                    LastName = names.last,
                    IsActive = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync(cancellationToken);

                var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Customer" && !r.IsDeleted, cancellationToken);
                if (customerRole != null)
                {
                    _context.UserRoles.Add(new UserRole
                    {
                        UserId = user.Id,
                        RoleId = customerRole.Id
                    });
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }
            else
            {
                var names = SplitName(firstName, lastName, fullName);
                var changed = false;

                if (!string.IsNullOrWhiteSpace(names.first) && !string.Equals(user.FirstName, names.first, StringComparison.Ordinal))
                {
                    user.FirstName = names.first;
                    changed = true;
                }

                if (!string.IsNullOrWhiteSpace(names.last) && !string.Equals(user.LastName, names.last, StringComparison.Ordinal))
                {
                    user.LastName = names.last;
                    changed = true;
                }

                if (!user.IsActive)
                {
                    user.IsActive = true;
                    changed = true;
                }

                if (changed)
                    await _context.SaveChangesAsync(cancellationToken);
            }

            return await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstAsync(u => u.Id == user.Id, cancellationToken);
        }

        private async Task<OpenIdConnectConfiguration> FetchOidcConfigurationAsync(IdentityProviderConfig provider, CancellationToken cancellationToken)
        {
            var metadataAddress = provider.AuthorityOrMetadataUrl.Trim();
            if (string.IsNullOrWhiteSpace(metadataAddress))
                throw new InvalidOperationException("OIDC provider metadata/authority URL is missing.");

            if (!metadataAddress.Contains(".well-known/openid-configuration", StringComparison.OrdinalIgnoreCase))
            {
                metadataAddress = metadataAddress.TrimEnd('/') + "/.well-known/openid-configuration";
            }

            var manager = new ConfigurationManager<OpenIdConnectConfiguration>(
                metadataAddress,
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever(_httpClientFactory.CreateClient())
                {
                    RequireHttps = metadataAddress.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                });

            return await manager.GetConfigurationAsync(cancellationToken);
        }

        private async Task<JsonElement> ExchangeCodeForTokensAsync(
            IdentityProviderConfig provider,
            string tokenEndpoint,
            string code,
            string redirectUri,
            CancellationToken cancellationToken)
        {
            var body = new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["client_id"] = provider.ClientId,
                ["redirect_uri"] = redirectUri
            };

            if (!string.IsNullOrWhiteSpace(provider.ClientSecret))
                body["client_secret"] = provider.ClientSecret;

            var client = _httpClientFactory.CreateClient();
            using var response = await client.PostAsync(tokenEndpoint, new FormUrlEncodedContent(body), cancellationToken);
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OIDC token exchange failed for provider {ProviderId}. Status: {Status}. Body: {Body}", provider.Id, response.StatusCode, raw);
                throw new InvalidOperationException("OIDC token exchange failed.");
            }

            using var doc = JsonDocument.Parse(raw);
            return doc.RootElement.Clone();
        }

        private async Task<ClaimsPrincipal> ValidateIdTokenAsync(
            IdentityProviderConfig provider,
            OpenIdConnectConfiguration oidcConfig,
            string idToken,
            CancellationToken cancellationToken)
        {
            var validation = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = oidcConfig.SigningKeys,
                ValidateIssuer = true,
                ValidIssuers = BuildValidIssuers(provider, oidcConfig),
                ValidateAudience = true,
                ValidAudience = provider.ClientId,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(2)
            };

            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(idToken, validation, out _);
            await Task.CompletedTask;
            return principal;
        }

        private static IEnumerable<string> BuildValidIssuers(IdentityProviderConfig provider, OpenIdConnectConfiguration config)
        {
            var issuers = new List<string>();
            if (!string.IsNullOrWhiteSpace(provider.Issuer))
                issuers.Add(provider.Issuer.Trim());
            if (!string.IsNullOrWhiteSpace(config.Issuer))
                issuers.Add(config.Issuer.Trim());

            if (!provider.EnforceStrictIssuer && issuers.Count == 0)
            {
                issuers.Add(config.Issuer);
            }

            return issuers.Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private static string DecodeSamlResponse(string samlResponse)
        {
            if (string.IsNullOrWhiteSpace(samlResponse))
                throw new InvalidOperationException("SAML response is empty.");

            var trimmed = samlResponse.Trim();
            if (trimmed.StartsWith("<", StringComparison.Ordinal))
                return trimmed;

            var bytes = Convert.FromBase64String(trimmed);
            return Encoding.UTF8.GetString(bytes);
        }

        private static string? ReadSamlAttribute(XmlDocument doc, string attributeName)
        {
            var xpath = $"//*[local-name()='Attribute'][@Name='{attributeName}']/*[local-name()='AttributeValue']";
            return ReadFirstNodeInnerText(doc, xpath);
        }

        private static string? ReadFirstNodeInnerText(XmlDocument doc, string xpath)
        {
            var node = doc.SelectSingleNode(xpath);
            var value = node?.InnerText?.Trim();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private static string? ReadFirstAttributeValue(XmlDocument doc, string xpath, string attributeName)
        {
            var node = doc.SelectSingleNode(xpath);
            var value = node?.Attributes?[attributeName]?.Value?.Trim();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private static (bool isValid, string error) ValidateSamlSignature(XmlDocument doc, IdentityProviderConfig provider)
        {
            var signatureNode = doc.SelectSingleNode("//*[local-name()='Signature']") as XmlElement;
            if (signatureNode == null)
                return (false, "SAML signature is missing.");

            var allowedThumbprints = provider.SamlAllowedCertificateThumbprints
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => x.Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var certNode = doc.SelectSingleNode("//*[local-name()='X509Certificate']");
            if (certNode == null || string.IsNullOrWhiteSpace(certNode.InnerText))
                return (false, "SAML signing certificate is missing from assertion.");

            X509Certificate2 cert;
            try
            {
                cert = new X509Certificate2(Convert.FromBase64String(certNode.InnerText.Trim()));
            }
            catch
            {
                return (false, "SAML signing certificate is invalid.");
            }

            if (allowedThumbprints.Count > 0)
            {
                var certThumbprint = (cert.Thumbprint ?? string.Empty).Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
                if (!allowedThumbprints.Contains(certThumbprint))
                    return (false, "SAML certificate thumbprint is not trusted for this provider.");
            }

            try
            {
                if (doc.DocumentElement == null)
                    return (false, "SAML document root element is missing.");

                var signedXml = new SignedXml(doc.DocumentElement);
                signedXml.LoadXml(signatureNode);
                if (!signedXml.CheckSignature(cert, true))
                    return (false, "SAML signature validation failed.");
            }
            catch (Exception ex)
            {
                return (false, $"SAML signature validation error: {ex.Message}");
            }

            return (true, string.Empty);
        }

        private static bool IsRedirectUriAllowed(IdentityProviderConfig provider, string redirectUri)
        {
            if (string.IsNullOrWhiteSpace(redirectUri))
                return false;

            if (string.IsNullOrWhiteSpace(provider.AllowedRedirectUrisJson))
                return true;

            try
            {
                var allowed = JsonSerializer.Deserialize<List<string>>(provider.AllowedRedirectUrisJson) ?? [];
                if (allowed.Count == 0)
                    return true;

                return allowed.Any(x => string.Equals(x.Trim(), redirectUri.Trim(), StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
        }

        private static (string first, string last) SplitName(string first, string last, string fullName)
        {
            if (!string.IsNullOrWhiteSpace(first) || !string.IsNullOrWhiteSpace(last))
                return (first.Trim(), last.Trim());

            if (string.IsNullOrWhiteSpace(fullName))
                return (string.Empty, string.Empty);

            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
                return (parts[0], string.Empty);

            return (parts[0], string.Join(' ', parts.Skip(1)));
        }
    }
}
