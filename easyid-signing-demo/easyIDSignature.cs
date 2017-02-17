using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace signature_demo
{
    // For the purpose of this demo, the following properties
    // are extracted from the signature, and displayed to the user.
    public class SignatureRepresentation
    {
        public string Ppid { get; set; }
        public string SignText { get; set; }
        public string Evidence { get; set; }
        public string Issuer { get; set; }
        public string EndorsingKeys { get; set; }
    }

    public class TrustedSenderRepresentation
    {
        public string Origin { get; set; }
    }

    public class SignatureRequester
    {
        private readonly string signerAuthority;
        private readonly string realm;
        private readonly ConfigurationManager<OpenIdConnectConfiguration> configManager;

        public SignatureRequester(string signerAuthority, string realm)
        {
            if (signerAuthority == null) throw new ArgumentNullException("signerAuthority");
            if (realm == null) throw new ArgumentNullException("realm");

            this.signerAuthority = signerAuthority;
            this.realm = realm;
            var oidcEndpoint = new UriBuilder(this.signerAuthority);
            oidcEndpoint.Path = "/.well-known/openid-configuration";
            this.configManager =
                    new ConfigurationManager<OpenIdConnectConfiguration>(
                        oidcEndpoint.Uri.AbsoluteUri,
                        new OpenIdConnectConfigurationRetriever());
        }

        public string AsRedirectUrl(
            string textToSign, 
            string signatureMethod, 
            Uri replyTo,
            string responseStrategy)
        {
            if (textToSign == null) throw new ArgumentNullException("textToSign");
            if (string.IsNullOrWhiteSpace(textToSign))
                throw new ArgumentException("Must have some visible text to sign", "textToSign");
            if (signatureMethod == null) throw new ArgumentNullException("signatureMethod");
            if (string.IsNullOrWhiteSpace(signatureMethod))
                throw new ArgumentException("Must have a non-blank signature method", "signatureMethod");
            if (replyTo == null) throw new ArgumentNullException("replyTo");
            if (!replyTo.IsAbsoluteUri)
                throw new ArgumentException("Must specify an absolute URI for the reply URL", "replyTo");

            var encoding = EncodingFor(signatureMethod);
            var signText =
                WebUtility.UrlEncode(
                    Convert.ToBase64String(encoding.GetBytes(textToSign)));
            var ub = new UriBuilder(signerAuthority);
            ub.Path = "/sign/text";
            var query =
                String.Format(
                    CultureInfo.InvariantCulture,
                    "wa=wsignin1.0&wtrealm={0}&wreply={1}&wauth={2}&signtext={3}&responseStrategy={4}",
                    this.realm,
                    replyTo,
                    signatureMethod,
                    signText,
                    responseStrategy);
            ub.Query = query;
            return ub.Uri.AbsoluteUri;
        }

        // Each signature scheme may have a different encoding rules
        private static Encoding EncodingFor(string signatureMethod)
        {
            if (signatureMethod == null) throw new ArgumentNullException("signatureMethod");

            if (signatureMethod.StartsWith("urn:grn:authn:no:bankid"))
            {
                return Encoding.GetEncoding("ISO-8859-1");
            }

            return Encoding.UTF8;
        }

        private async Task<ClaimsPrincipal> ValidateEndorsingSignature(
            string rawSignatureResponse)
        {
            var cfg = await this.configManager.GetConfigurationAsync();

            var tokenHandler = new JwtSecurityTokenHandler();
            tokenHandler.InboundClaimTypeMap = new Dictionary<string, string>();
            var validationParams = new TokenValidationParameters();
            validationParams.ValidIssuer = cfg.Issuer;
            validationParams.ValidAudience = realm;
            validationParams.ClockSkew = TimeSpan.FromMinutes(1);
            validationParams.IssuerSigningKeys = cfg.SigningKeys;
            SecurityToken token = null;
            // ValidateToken throws an exception if anything fails to validate.
            return tokenHandler.ValidateToken(rawSignatureResponse, validationParams, out token);
        }

        private string ValueOrDefault(ClaimsPrincipal p, string claimtype, string def)
        {
            var c = p.FindFirst(claimtype);
            if (c == null) return def;
            return c.Value;
        }

        // Each signature scheme may have a different naming for the PPID identifier
        private string PpidClaimType(string signMethod)
        {
            if (signMethod.StartsWith("urn:grn:authn:no:bankid"))
            {
                return "ssn";
            }
            if (signMethod.StartsWith("urn:grn:authn:se:bankid"))
            {
                return "ssn";
            }
            if (signMethod.StartsWith("urn:grn:authn:dk:nemid"))
            {
                return "cpr";
            }

            return "";
        }

        private async Task<string> SerializeEndorsingKeys()
        {
            var cfg = await this.configManager.GetConfigurationAsync();
            var signingKeys =
                cfg.JsonWebKeySet.Keys
                    .Where(k => k.Use == "sig").ToArray();
            var jsonSerializerSettings = new Newtonsoft.Json.JsonSerializerSettings();
            jsonSerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
            var endorsingKeys =
                Newtonsoft.Json.JsonConvert
                    .SerializeObject(signingKeys, Newtonsoft.Json.Formatting.Indented,
                    jsonSerializerSettings);
            return endorsingKeys;
        }

        public async Task<SignatureRepresentation> ValidateSignature(string signature, string signatureMethod)
        {
            if (signature == null) throw new ArgumentNullException("signature");
            if (signatureMethod == null) throw new ArgumentNullException("signatureMethod");

            var principal = await this.ValidateEndorsingSignature(signature);

            // The evidence property is always UTF-8 encoded
            var evidence =
                Encoding.UTF8.GetString(
                    Convert.FromBase64String(
                        ValueOrDefault(principal, "evidence", "")));
            // Get core properties
            var ppidClaim = this.PpidClaimType(signatureMethod);
            var ppid = ValueOrDefault(principal, ppidClaim, "N/A");
            var issuer = ValueOrDefault(principal, "iss", "N/A");
            var encoding = EncodingFor(signatureMethod);
            var signText =
                encoding.GetString(Convert.FromBase64String(
                    ValueOrDefault(principal, "signtext", "N/A")));

            string endorsingKeys = await this.SerializeEndorsingKeys();

            return new SignatureRepresentation
            {
                Evidence = evidence,
                Ppid = ppid,
                SignText = signText,
                Issuer = issuer,
                EndorsingKeys = endorsingKeys
            };
        }

        public string SignerAuthority { get { return this.signerAuthority; } }
    }
}