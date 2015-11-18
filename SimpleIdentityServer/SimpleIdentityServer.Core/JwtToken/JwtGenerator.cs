﻿using System;
using System.Collections.Generic;
using System.Linq;

using System.Security.Claims;
using Microsoft.Practices.ObjectBuilder2;

using SimpleIdentityServer.Core.Configuration;
using SimpleIdentityServer.Core.Extensions;
using SimpleIdentityServer.Core.Helpers;
using SimpleIdentityServer.Core.Jwt.Mapping;
using SimpleIdentityServer.Core.Jwt.Signature;
using SimpleIdentityServer.Core.Models;
using SimpleIdentityServer.Core.Parameters;
using SimpleIdentityServer.Core.Repositories;
using SimpleIdentityServer.Core.Validators;
using SimpleIdentityServer.Core.Jwt;
using SimpleIdentityServer.Core.Jwt.Encrypt;

namespace SimpleIdentityServer.Core.JwtToken
{
    public interface IJwtGenerator
    {
        JwsPayload GenerateJwsPayload(
            ClaimsPrincipal claimPrincipal,
            AuthorizationParameter authorizationParameter);

        string Sign(
            JwsPayload jwsPayload,
            AuthorizationParameter authorizationParameter);

        string Encrypt(
            string toEncrypt,
            AuthorizationParameter authorizationParameter);
    }

    public class JwtGenerator : IJwtGenerator
    {
        private readonly ISimpleIdentityServerConfigurator _simpleIdentityServerConfigurator;

        private readonly IClientRepository _clientRepository;

        private readonly IClientValidator _clientValidator;

        private readonly IJsonWebKeyRepository _jsonWebKeyRepository;

        private readonly IScopeRepository _scopeRepository;

        private readonly IClaimsMapping _claimsMapping;

        private readonly IParameterParserHelper _parameterParserHelper;

        private readonly IJwsGenerator _jwsGenerator;

        private readonly IJweGenerator _jweGenerator;

        public JwtGenerator(
            ISimpleIdentityServerConfigurator simpleIdentityServerConfigurator,
            IClientRepository clientRepository,
            IClientValidator clientValidator,
            IJsonWebKeyRepository jsonWebKeyRepository,
            IScopeRepository scopeRepository,
            IClaimsMapping claimsMapping,
            IParameterParserHelper parameterParserHelper,
            IJwsGenerator jwsGenerator,
            IJweGenerator jweGenerator)
        {
            _simpleIdentityServerConfigurator = simpleIdentityServerConfigurator;
            _clientRepository = clientRepository;
            _clientValidator = clientValidator;
            _jsonWebKeyRepository = jsonWebKeyRepository;
            _scopeRepository = scopeRepository;
            _claimsMapping = claimsMapping;
            _parameterParserHelper = parameterParserHelper;
            _jwsGenerator = jwsGenerator;
            _jweGenerator = jweGenerator;
        }

        public JwsPayload GenerateJwsPayload(
            ClaimsPrincipal claimPrincipal,
            AuthorizationParameter authorizationParameter)
        {
            // Get the issuer from the configuration.
            var issuerName = _simpleIdentityServerConfigurator.GetIssuerName();
            // Audience can be used to disambiguate the intended recipients.
            // Fill-in the list with the list of client_id suspected to parse the JWT tokens.
            var audiences = new List<string>();
            var clients = _clientRepository.GetAll();
            clients.ForEach(client =>
            {
                var isClientSupportIdTokenResponseType =
                    _clientValidator.ValidateResponseType(ResponseType.id_token, client);
                if (isClientSupportIdTokenResponseType)
                {
                    audiences.Add(client.ClientId);
                }
            });

            // Calculate the expiration datetime
            var currentDateTime = DateTime.Now;
            var expiredDateTime = currentDateTime.AddSeconds(_simpleIdentityServerConfigurator.GetTokenValidityPeriodInSeconds());
            var expirationInSeconds = expiredDateTime.ConvertToUnixTimestamp();
            // Calculate the time in seconds which the JWT was issued.
            var iatInSeconds = currentDateTime.ConvertToUnixTimestamp();
            // Populate the claims
            var scopes = _parameterParserHelper.ParseScopeParameters(authorizationParameter.Scope);
            var claims = GetClaimsFromRequestedScopes(scopes, claimPrincipal);

            var result = new JwsPayload
            {
                {
                    Jwt.Constants.StandardClaimNames.Issuer, issuerName
                },
                {
                    Jwt.Constants.StandardClaimNames.Audiences, audiences.ToArray()
                },
                {
                    Jwt.Constants.StandardClaimNames.ExpirationTime, expirationInSeconds
                },
                {
                    Jwt.Constants.StandardClaimNames.Iat, iatInSeconds
                }
            };

            foreach (var claim in claims)
            {
                result.Add(claim.Key, claim.Value);
            }

            // If the max_age request is made or when auth_time is requesed as an Essential claim then we calculate the auth_time
            // The auth_time corresponds to the time when the End-User authentication occured. 
            // Its value is a JSON number representing the number of seconds from 1970-01-01
            if (authorizationParameter.MaxAge != 0.0D)
            {
                var authenticationInstant = claimPrincipal.Claims.SingleOrDefault(c => c.Type == ClaimTypes.AuthenticationInstant);
                if (authenticationInstant != null)
                {
                    result.Add(Jwt.Constants.StandardClaimNames.AuthenticationTime, double.Parse(authenticationInstant.Value));
                }
            }

            // Set the nonce value in the id token. The value is coming from the authorization request
            if (!string.IsNullOrWhiteSpace(authorizationParameter.Nonce))
            {
                result.Add(Jwt.Constants.StandardClaimNames.Nonce, authorizationParameter.Nonce);
            }

            // Set the ACR : Authentication Context Class Reference
            // Set the AMR : Authentication Methods Reference
            // For the moment we support a level 1 because only password via HTTPS is supported.
            /*if (!string.IsNullOrWhiteSpace(authorizationParameter.AcrValues))
            {*/
            result.Add(Jwt.Constants.StandardClaimNames.Acr, Core.Constants.StandardArcParameterNames.OpenIdCustomAuthLevel + ".password=1");
            result.Add(Jwt.Constants.StandardClaimNames.Amr, "password");
            //}

            // Set the client_id
            // This claim is only needed when the ID token has a single audience value & that audience is different than the authorized party.
            if (audiences != null && 
                audiences.Count() == 1 && 
                audiences.First() == authorizationParameter.ClientId)
            {
                result.Add(Jwt.Constants.StandardClaimNames.Azp, authorizationParameter.ClientId);
            }

            // TODO : Add another claims in it ...

            return result;
        }

        public string Sign(
            JwsPayload jwsPayload,
            AuthorizationParameter authorizationParameter)
        {
            var client = _clientRepository.GetClientById(authorizationParameter.ClientId);
            var signedAlg = client.IdTokenSignedTResponseAlg;
            JwsAlg signedAlgorithm;
            if (string.IsNullOrWhiteSpace(signedAlg)
                || !Enum.TryParse(signedAlg, out signedAlgorithm))
            {
                signedAlgorithm = JwsAlg.HS256;
            }
            
            // In the "open-id-connect-discovery" there's an endpoint jwks_uri :
            // This url contains the signing key's) the RP uses to validate signatures from the OP
            // The JWS set may also contain the Server's encryption key(s) which are used by the RP to encrypt requests to the server.
            var jsonWebKey = GetJsonWebKey(
                signedAlgorithm.ToAllAlg(), 
                KeyOperations.Sign, 
                Use.Sig);
            return _jwsGenerator.Generate(
                jwsPayload, 
                signedAlgorithm, 
                jsonWebKey);
        }

        public string Encrypt(
            string jwe,
            AuthorizationParameter authorizationParameter)
        {
            var client = _clientRepository.GetClientById(authorizationParameter.ClientId);
            var algName = client.IdTokenEncryptedResponseAlg;
            var encName = client.IdTokenEncryptedResponseEnc;
            
            if (string.IsNullOrWhiteSpace(algName) || 
                !Jwt.Constants.MappingNameToJweAlgEnum.Keys.Contains(algName))
            {
                return jwe;
            }

            JweEnc encEnum;
            if (string.IsNullOrWhiteSpace(encName) ||
                !Jwt.Constants.MappingNameToJweEncEnum.Keys.Contains(encName))
            {
                encEnum = JweEnc.A128CBC_HS256;
            }
            else
            {
                encEnum = Jwt.Constants.MappingNameToJweEncEnum[encName];
            }

            var algEnum = Jwt.Constants.MappingNameToJweAlgEnum[algName];

            var jsonWebKey = GetJsonWebKey(
                algEnum.ToAllAlg(),
                KeyOperations.Encrypt,
                Use.Enc);

            return _jweGenerator.GenerateJwe(
                jwe, 
                algEnum, 
                encEnum, 
                jsonWebKey);
        }

        private Dictionary<string, string> GetClaimsFromRequestedScopes(
            IEnumerable<string> scopes,
            ClaimsPrincipal claimsPrincipal)
        {
            var result = new Dictionary<string, string>
            {
                {
                    Jwt.Constants.StandardResourceOwnerClaimNames.Subject, claimsPrincipal.GetSubject()
                }
            };
            foreach (var scope in scopes)
            {
                var scopeClaims = _scopeRepository.GetScopeByName(scope).Claims;
                if (scopeClaims == null)
                {
                    continue;
                }

                var openIdClaims = _claimsMapping.MapToOpenIdClaims(claimsPrincipal.Claims);
                openIdClaims.Where(oc => scopeClaims.Contains(oc.Key))
                    .Select(oc => new { key = oc.Key, val = oc.Value })
                    .ForEach(r => result.Add(r.key, r.val));
            }

            return result;
        } 

        private JsonWebKey GetJsonWebKey(
            AllAlg alg,
            KeyOperations operation,
            Use use)
        {
            JsonWebKey result = null;
            var jsonWebKeys = _jsonWebKeyRepository.GetByAlgorithm(
                use,
                alg,
                new[] { operation });
            if (jsonWebKeys != null && jsonWebKeys.Any())
            {
                result = jsonWebKeys.First();
            }

            return result;
        }
    }
}
