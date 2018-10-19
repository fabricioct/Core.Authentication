// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Test;
using System.Collections.Generic;
using System.Security.Claims;
using static IdentityServer4.IdentityServerConstants;

namespace Core.Authentication.Infrastructure
{
    public static class InMemoryConfig
    {
        // scopes define the resources in your system
        public static IEnumerable<IdentityResource> GetIdentityResources()
        {
            return new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email(),
                new IdentityResource
                {
                    Name = "role",
                    DisplayName = "Permitir roles",
                    Required = true,
                    UserClaims = new[] { JwtClaimTypes.Role}
                }
            };
        }

        public static IEnumerable<ApiResource> GetApiResources()
        {
            return new List<ApiResource>
            {
                new ApiResource {
                Name = "api1",
                DisplayName = "Análisede Crédito API",
                Description = "Custom API Access",
                UserClaims = new List<string> {"role"},
                ApiSecrets = new List<Secret> {new Secret("scopeSecret".Sha256())},
                Scopes = new List<Scope> {
                    new Scope("customAPI.read"),
                    new Scope("customAPI.write")
                }
            }
            };
        }

        // clients want to access resources (aka scopes)
        public static IEnumerable<Client> GetClients()
        {
            // client credentials client
            return new List<Client>
            {
                // OpenID Connect hybrid flow and client credentials client (MVC)
                new Client
                {
                    ClientId = "mvc",
                    ClientName = "MVC Client",
                    AllowedGrantTypes = GrantTypes.HybridAndClientCredentials,

                    ClientSecrets =
                    {
                        new Secret("secret".Sha256())
                    },

                    RedirectUris = { "http://localhost:5002/signin-oidc" },
                    PostLogoutRedirectUris = { "http://localhost:5002/signout-callback-oidc" },
                    AllowedScopes =
                    {
                        StandardScopes.OpenId,
                        StandardScopes.Profile,
                        StandardScopes.Email,
                        JwtClaimTypes.Role,
                        "customAPI.write"
                    },
                    AllowOfflineAccess = true
                  //  AlwaysSendClientClaims = true

                }
            };
        }

        public static List<TestUser> GetUsers()
        {
            return new List<TestUser>
            {
                new TestUser
                {
                    SubjectId = "5BE86359-073C-434B-AD2D-A3932222DABE",
                    Username = "admin",
                    Password = "Pass123$",
                     Claims = new List<Claim> {
                        new Claim(JwtClaimTypes.Email, "bob@bob.com"),
                        new Claim(JwtClaimTypes.Role, "admin"),
                        new Claim(JwtClaimTypes.EmailVerified, "true", System.Security.Claims.ClaimValueTypes.Boolean)
                    }
                },
                new TestUser
                {
                    SubjectId = "0EA50EBE-BAE6-46C1-9F7B-A427F3C7A129",
                    Username = "bob",
                    Password = "Pass123$",
                    Claims = new List<Claim>
                    {
                        new Claim(JwtClaimTypes.Email, "bob@bob.com"),
                        new Claim(JwtClaimTypes.Role, "usuario"),
                        new Claim(JwtClaimTypes.EmailVerified, "true", System.Security.Claims.ClaimValueTypes.Boolean)
                    }
                }
            };
        }
    }
}