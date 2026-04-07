using Grpc.Core;
using Medimate.Auth.V1;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Share.Jwt;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MediMate.Grpc
{
    public class AuthGrpcService : AuthService.AuthServiceBase
    {
        private readonly ILogger<AuthGrpcService> _logger;
        private readonly TokenValidationParameters _tokenValidationParameters;
        private readonly JwtSecurityTokenHandler _tokenHandler = new();

        public AuthGrpcService(ILogger<AuthGrpcService> logger, IOptions<JwtSettings> jwtOptions)
        {
            _logger = logger;
            var jwt = jwtOptions.Value;

            _tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwt.Issuer,
                ValidAudience = jwt.Audience,
                IssuerSigningKey = ResolveSecurityKey(jwt),
                ClockSkew = TimeSpan.Zero
            };
        }

        public override Task<ValidateTokenResponse> ValidateToken(ValidateTokenRequest request, ServerCallContext context)
        {
            var requestId = context.RequestHeaders.FirstOrDefault(h => h.Key == "x-request-id")?.Value
                            ?? context.RequestHeaders.FirstOrDefault(h => h.Key == "x-correlation-id")?.Value
                            ?? context.Host;

            if (string.IsNullOrWhiteSpace(request.Token))
            {
                _logger.LogWarning("ValidateToken failed. request_id={RequestId}, reason=empty_token", requestId);
                return Task.FromResult(new ValidateTokenResponse { IsValid = false });
            }

            try
            {
                var principal = _tokenHandler.ValidateToken(request.Token, _tokenValidationParameters, out _);
                var userId = ResolveUserId(principal);
                var roles = ResolveRoles(principal);

                var response = new ValidateTokenResponse
                {
                    IsValid = true,
                    UserId = userId
                };
                response.Roles.AddRange(roles);
                return Task.FromResult(response);
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogInformation(
                    "ValidateToken invalid token. request_id={RequestId}, reason={Reason}",
                    requestId,
                    ex.GetType().Name);
                return Task.FromResult(new ValidateTokenResponse { IsValid = false });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "ValidateToken unexpected error. request_id={RequestId}, reason={Reason}",
                    requestId,
                    ex.GetType().Name);
                return Task.FromResult(new ValidateTokenResponse { IsValid = false });
            }
        }

        private static SecurityKey ResolveSecurityKey(JwtSettings settings)
        {
            if (!string.IsNullOrWhiteSpace(settings.SecretKey))
            {
                return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecretKey));
            }

            throw new InvalidOperationException("JWT:SecretKey must be configured.");
        }

        private static string ResolveUserId(ClaimsPrincipal principal)
        {
            return principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal.FindFirstValue("user_id")
                ?? principal.FindFirstValue("userid")
                ?? string.Empty;
        }

        private static IReadOnlyCollection<string> ResolveRoles(ClaimsPrincipal principal)
        {
            return principal.Claims
                .Where(c =>
                    c.Type == ClaimTypes.Role ||
                    c.Type == "role" ||
                    c.Type == "roles" ||
                    c.Type == "Role")
                .SelectMany(c => c.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Select(r => r.ToLowerInvariant())
                .Distinct()
                .ToArray();
        }
    }
}
