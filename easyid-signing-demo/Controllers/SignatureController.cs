using iframe_demo.Models;
using Microsoft.IdentityModel.Protocols;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace iframe_demo.Controllers
{
    public class SignatureController : Controller
    {
        private readonly string signerAuthority;
        private readonly string realm;

        public SignatureController()
        {
            this.signerAuthority = ConfigurationManager.AppSettings["easyid:signerAuthority"];
            this.realm = ConfigurationManager.AppSettings["easyid:clientid"];
        }

        private SignMethod[] SignMethods()
        {
            return new[] {
                new SignMethod {
                    Id = "urn:grn:authn:no:bankid:central",
                    DisplayName = "NO BankID kodebrikke" },
                new SignMethod {
                    Id = "urn:grn:authn:no:bankid:mobile",
                    DisplayName = "NO BankID mobil" },
                new SignMethod {
                    Id = "urn:grn:authn:se:bankid:same-device",
                    DisplayName = "SE BankID denna enhet" },
                new SignMethod {
                    Id = "urn:grn:authn:se:bankid:another-device",
                    DisplayName = "SE BankID annan enhet" },
                new SignMethod {
                    Id = "urn:grn:authn:dk:nemid:poces",
                    DisplayName = "DK NemID privat" },
                new SignMethod {
                    Id = "urn:grn:authn:dk:nemid:moces",
                    DisplayName = "DK NemID erhverv" },
                new SignMethod {
                    Id = "urn:grn:authn:dk:nemid:moces:codefile",
                    DisplayName = "DK NemID nøglefil (erhverv)" },
            };
        }

        // Set up the landing page with a default signing method selection
        public ActionResult Text()
        {
            var model = new SignModel { SignMethods = this.SignMethods() };
            model.SignMethods.First().SetChecked();
            return View(model);
        }

        private Encoding GetEncoding(string signMethod)
        {
            // Norwegian BankId does not work with UTF-8
            if (signMethod.StartsWith("urn:grn:authn:no:bankid"))
            {
                return Encoding.GetEncoding("ISO-8859-1");
            }

            return Encoding.UTF8;
        }

        // Each identity scheme may have a different naming for the PPID identifier
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

        // The text-to-sign is post'ed here
        [HttpPost]
        public ActionResult Text(SignModel model, string selectedSignMethod)
        {
            var currentAuthority = this.Request.Url.GetLeftPart(UriPartial.Authority);
            var replyTo = 
                string.Format(CultureInfo.InvariantCulture,
                    "{0}/Signature/Done?selectedSignMethod={1}",
                    currentAuthority,
                    selectedSignMethod);
            var encoding = GetEncoding(selectedSignMethod);
            var signText = Convert.ToBase64String(encoding.GetBytes(model.TextToSign));
            var signerUrl =
                String.Format(
                    CultureInfo.InvariantCulture,
                    "{0}/sign/text/?wa=wsignin1.0&wtrealm={1}&wreply={2}&wauth={3}&signtext={4}",
                    signerAuthority,
                    realm,
                    replyTo,
                    selectedSignMethod,
                    signText);
            return this.Redirect(signerUrl);
        }

        private string ValueOrDefault(ClaimsPrincipal p, string claimtype, string def)
        {
            var c = p.FindFirst(claimtype);
            if (c == null) return def;
            return c.Value;
        }

        private ClaimsPrincipal ValidateEndorsingSignature(
            OpenIdConnectConfiguration cfg,
            string rawSignatureResponse)
        {
            JwtSecurityTokenHandler.InboundClaimTypeMap = new Dictionary<string, string>();
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParams = new TokenValidationParameters();
            validationParams.ValidIssuer = cfg.Issuer;
            validationParams.ValidAudience = realm;
            validationParams.ClockSkew = TimeSpan.FromMinutes(1);
            validationParams.IssuerSigningTokens = cfg.SigningTokens;
            SecurityToken token = null;
            // ValidateToken throws an exception if anything fails to validate.
            return tokenHandler.ValidateToken(rawSignatureResponse, validationParams, out token);
        }

        // The response from easyID is post'ed here, because the wreply in the 
        // HTTP POST action Text says so.
        [HttpPost]
        public Task<ViewResult> Done(SignedModel model, string selectedSignMethod)
        {
            // Get the OIDC metadata from easyID - in a real-life scenario,
            // you would want to cache the response from GetConfigurationAsync 
            // for an hour or so.
            var client = new System.Net.Http.HttpClient();
            var oidcEndpoint = new UriBuilder(signerAuthority);
            oidcEndpoint.Path = "/.well_known/openid-configuration";
            var oidcConfigMgr =
                    new ConfigurationManager<OpenIdConnectConfiguration>(
                        oidcEndpoint.Uri.AbsoluteUri, client);

            return oidcConfigMgr.GetConfigurationAsync().ContinueWith(
                r =>
                {
                    var oidcConfig = r.Result;

                    // This demo implementation builds a view model with some select properties.
                    // In real-life scenarios, you would want to store
                    //  - the raw signature 
                    //  - the Json Web Key(s) used for validating the JWT signature
                    // in your data store for compliance purposes.
                    var rawSignature = model.Signature;

                    var principal =
                        this.ValidateEndorsingSignature(oidcConfig, rawSignature);

                    // The evidence property is always UTF-8 encoded
                    var evidence =
                        Encoding.UTF8.GetString(
                            Convert.FromBase64String(
                                ValueOrDefault(principal, "evidence", "")));
                    // Get core properties
                    var ppidClaim = this.PpidClaimType(selectedSignMethod);
                    var ppid = ValueOrDefault(principal, ppidClaim, "N/A");
                    var issuer = ValueOrDefault(principal, "iss", "N/A");
                    var encoding = GetEncoding(selectedSignMethod);
                    var signText =
                        encoding.GetString(Convert.FromBase64String(
                            ValueOrDefault(principal, "signtext", "N/A")));

                    string endorsingKeys = SerializeEndorsingKeys(oidcConfig);
                    var displayModel = new SignatureModel
                    {
                        Evidence = evidence,
                        Ppid = ppid,
                        SignText = signText,
                        Issuer = issuer,
                        EndorsingKeys = endorsingKeys
                    };
                    return this.View("SignatureResult", displayModel);
                });
        }

        private static string SerializeEndorsingKeys(OpenIdConnectConfiguration oidcConfig)
        {
            var signingKeys =
                oidcConfig.JsonWebKeySet.Keys
                    .Where(k => k.Use == "sig").ToArray();
            var jsonSerializerSettings = new Newtonsoft.Json.JsonSerializerSettings();
            jsonSerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
            var endorsingKeys =
                Newtonsoft.Json.JsonConvert
                    .SerializeObject(signingKeys, Newtonsoft.Json.Formatting.Indented,
                    jsonSerializerSettings);
            return endorsingKeys;
        }
    }
}