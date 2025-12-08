#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace RoomBooking.Infrastructure.Auth
{
    /// <summary>
    /// DI extensions for configuring JWT authentication and authorization policies.
    /// </summary>
    public static class AuthExtensions
    {
        /// <summary>
        /// Adds JWT Bearer authentication using settings from configuration section "Jwt" and optional overrides.
        /// </summary>
        public static IServiceCollection AddJwtAuthentication(
            this IServiceCollection services,
            IConfiguration configuration,
            Action<JwtOptions>? configure = null)
        {
            var jwtOptions = new JwtOptions();
            var section = configuration.GetSection("Jwt");
            if (section.Exists())
            {
                section.Bind(jwtOptions);
                services.Configure<JwtOptions>(section);
            }

            configure?.Invoke(jwtOptions);
            jwtOptions.Validate();

            // Keep claim types as emitted by the token without default remapping
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            var keyBytes = Encoding.UTF8.GetBytes(jwtOptions.Secret);
            var signingKey = new SymmetricSecurityKey(keyBytes);

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = jwtOptions.RequireHttpsMetadata;
                    options.SaveToken = true;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = jwtOptions.ValidateIssuerSigningKey,
                        IssuerSigningKey = signingKey,

                        ValidateIssuer = jwtOptions.ValidateIssuer,
                        ValidIssuer = jwtOptions.Issuer,

                        ValidateAudience = jwtOptions.ValidateAudience,
                        ValidAudience = jwtOptions.Audience,

                        ValidateLifetime = jwtOptions.ValidateLifetime,
                        ClockSkew = TimeSpan.FromSeconds(jwtOptions.ClockSkewInSeconds),

                        NameClaimType = jwtOptions.NameClaimType,
                        RoleClaimType = jwtOptions.RoleClaimType
                    };

                    // Optional diagnostics/events
                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            // You can log context.Exception here if needed.
                            context.NoResult();
                            return Task.CompletedTask;
                        },
                        OnTokenValidated = context =>
                        {
                            // Add custom validations if required.
                            return Task.CompletedTask;
                        }
                    };
                });

            return services;
        }

        /// <summary>
        /// Adds authorization policies for role-based and scope-based access control.
        /// </summary>
        public static IServiceCollection AddAuthorizationPolicies(
            this IServiceCollection services,
            Action<AuthorizationOptions>? configure = null)
        {
            services.AddAuthorization(options =>
            {
                // Default policy: authenticated users only
                options.DefaultPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .Build();

                // Role-based policies
                // Role-based policies with fallback for explicit "role" claim type
                options.AddPolicy(Policies.RequireAdmin, policy =>
                    policy.RequireAssertion(context => 
                        context.User.IsInRole(Roles.Admin) || 
                        context.User.HasClaim(c => (c.Type == "role" || c.Type == ClaimTypes.Role) && c.Value == Roles.Admin)));

                options.AddPolicy(Policies.RequireManager, policy =>
                     policy.RequireAssertion(context => 
                        context.User.IsInRole(Roles.Manager) || 
                        context.User.IsInRole(Roles.Admin) ||
                        context.User.HasClaim(c => (c.Type == "role" || c.Type == ClaimTypes.Role) && (c.Value == Roles.Manager || c.Value == Roles.Admin))));

                options.AddPolicy(Policies.RequireEmployee, policy =>
                     policy.RequireAssertion(context => 
                        context.User.IsInRole(Roles.Employee) || 
                        context.User.IsInRole(Roles.Manager) ||
                        context.User.IsInRole(Roles.Admin) ||
                        context.User.HasClaim(c => (c.Type == "role" || c.Type == ClaimTypes.Role) && (c.Value == Roles.Employee || c.Value == Roles.Manager || c.Value == Roles.Admin))));

                // Scope-based policies (supports both "scope" and "scp" claim types)
                options.AddPolicy(Policies.BookingsRead, policy =>
                    policy.RequireScope("bookings.read"));

                options.AddPolicy(Policies.BookingsWrite, policy =>
                    policy.RequireScope("bookings.write"));

                configure?.Invoke(options);
            });

            return services;
        }

        /// <summary>
        /// Convenience method to add both JWT authentication and authorization policies in one call.
        /// </summary>
        public static IServiceCollection AddApiSecurity(
            this IServiceCollection services,
            IConfiguration configuration,
            Action<JwtOptions>? configureJwt = null,
            Action<AuthorizationOptions>? configureAuthorization = null)
        {
            services.AddJwtAuthentication(configuration, configureJwt);
            services.AddAuthorizationPolicies(configureAuthorization);
            return services;
        }

        /// <summary>
        /// Adds an assertion to require all specified scopes in the "scope" or "scp" claims.
        /// Claim values can be space-delimited (common for OAuth/OpenID providers).
        /// </summary>
        public static AuthorizationPolicyBuilder RequireScope(this AuthorizationPolicyBuilder builder, params string[] requiredScopes)
        {
            return builder.RequireAssertion(context =>
            {
                if (requiredScopes is null || requiredScopes.Length == 0)
                    return true;

                var scopeValues = GetScopeValues(context.User);
                if (scopeValues.Count == 0)
                    return false;

                // All required scopes must be present
                return requiredScopes.All(req => scopeValues.Contains(req, StringComparer.OrdinalIgnoreCase));
            });
        }

        private static HashSet<string> GetScopeValues(ClaimsPrincipal user)
        {
            // Collect values from "scope" and "scp" claims
            var claims = user.FindAll("scope").Select(c => c.Value)
                .Concat(user.FindAll("scp").Select(c => c.Value));

            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var value in claims)
            {
                if (string.IsNullOrWhiteSpace(value)) continue;
                // Scope claims may be space-delimited
                foreach (var token in value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    set.Add(token);
                }
            }
            return set;
        }
    }

    /// <summary>
    /// Strongly-typed JWT configuration options. Bind from configuration section "Jwt".
    /// </summary>
    public sealed class JwtOptions
    {
        /// <summary>
        /// Expected issuer of tokens.
        /// </summary>
        public string? Issuer { get; set; }

        /// <summary>
        /// Expected audience of tokens.
        /// </summary>
        public string? Audience { get; set; }

        /// <summary>
        /// Symmetric secret used for signing token validation.
        /// Store securely (e.g., environment variable, secrets manager).
        /// </summary>
        public string Secret { get; set; } = string.Empty;

        /// <summary>
        /// Whether to validate the token issuer.
        /// </summary>
        public bool ValidateIssuer { get; set; } = true;

        /// <summary>
        /// Whether to validate the token audience.
        /// </summary>
        public bool ValidateAudience { get; set; } = true;

        /// <summary>
        /// Whether to validate token lifetime (exp, nbf).
        /// </summary>
        public bool ValidateLifetime { get; set; } = true;

        /// <summary>
        /// Whether to validate the signing key.
        /// </summary>
        public bool ValidateIssuerSigningKey { get; set; } = true;

        /// <summary>
        /// Acceptable clock skew in seconds when validating exp/nbf.
        /// </summary>
        public int ClockSkewInSeconds { get; set; } = 60;

        /// <summary>
        /// Enforce HTTPS metadata for the JWT bearer handler.
        /// </summary>
        public bool RequireHttpsMetadata { get; set; } = true;

        /// <summary>
        /// Which claim represents the user identity name.
        /// </summary>
        public string NameClaimType { get; set; } = ClaimTypes.Name;

        /// <summary>
        /// Which claim represents user roles.
        /// </summary>
        public string RoleClaimType { get; set; } = ClaimTypes.Role;

        public void Validate()
        {
            if (ValidateIssuer && string.IsNullOrWhiteSpace(Issuer))
                throw new InvalidOperationException("JwtOptions.Issuer must be provided when ValidateIssuer is true.");

            if (ValidateAudience && string.IsNullOrWhiteSpace(Audience))
                throw new InvalidOperationException("JwtOptions.Audience must be provided when ValidateAudience is true.");

            if (ValidateIssuerSigningKey && string.IsNullOrWhiteSpace(Secret))
                throw new InvalidOperationException("JwtOptions.Secret must be provided when ValidateIssuerSigningKey is true.");

            if (ClockSkewInSeconds < 0)
                throw new InvalidOperationException("JwtOptions.ClockSkewInSeconds cannot be negative.");
        }
    }

    /// <summary>
    /// Role names used across the application.
    /// </summary>
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Manager = "Manager";
        public const string Employee = "Employee";
        public const string User = "User";
    }

    /// <summary>
    /// Authorization policy names used by the API.
    /// </summary>
    public static class Policies
    {
        public const string RequireAdmin = "RequireAdmin";
        public const string RequireManager = "RequireManager";
        public const string RequireEmployee = "RequireEmployee";

        public const string BookingsRead = "Bookings.Read";
        public const string BookingsWrite = "Bookings.Write";
    }
}
