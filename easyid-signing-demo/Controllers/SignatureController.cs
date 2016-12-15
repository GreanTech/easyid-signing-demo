using iframe_demo.Models;
using Microsoft.IdentityModel.Protocols;
using System;
using System.Collections.Generic;
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
        private readonly string signerAuthority = "https://easyid.www.prove.id";
        private readonly string realm = "urn:grn:app:easyid-signing-demo";

        private SignMethod[] SignMethods()
        {
            return new[] {
                new SignMethod {
                    Id = "urn:grn:authn:no:bankid:central",
                    DisplayName = "NO BankID kodebrik" },
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
        // GET: Signature
        public ActionResult Text()
        {
            var model = new SignModel { SignMethods = this.SignMethods() };
            return View(model);
        }

        private Encoding GetEncoding(string signMethod)
        {
            if (signMethod.StartsWith("urn:grn:authn:no:bankid"))
            {
                return Encoding.GetEncoding("ISO-8859-1");
            }

            return Encoding.UTF8;
        }

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

        // POST: Signature
        [HttpPost]
        public ActionResult Text(SignModel model, string selectedSignMethod)
        {
            var replyTo = 
                string.Format(CultureInfo.InvariantCulture,
                    "https://localhost:44300/Signature/Done?selectedSignMethod={0}",
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

        public Task<ViewResult> Done(SignedModel model, string selectedSignMethod)
        {
            var client = new System.Net.Http.HttpClient();
            var oidcEndpoint = new UriBuilder(signerAuthority);
            oidcEndpoint.Path = "/.well_known/openid-configuration";
            var oidcConfigMgr =
                    new ConfigurationManager<OpenIdConnectConfiguration>(
                        oidcEndpoint.Uri.AbsoluteUri, client);

            return oidcConfigMgr.GetConfigurationAsync().ContinueWith(
                r =>
                {
                    var cfg = r.Result;
                    JwtSecurityTokenHandler.InboundClaimTypeMap = new Dictionary<string, string>();
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var validationParams = new TokenValidationParameters();
                    validationParams.ValidIssuer = cfg.Issuer;
                    validationParams.ValidAudience = realm;
                    validationParams.ClockSkew = TimeSpan.FromMinutes(5);
                    validationParams.ValidateLifetime = false;
                    validationParams.IssuerSigningTokens = cfg.SigningTokens;            
                    SecurityToken token = null;
                    var principal = 
                        tokenHandler.ValidateToken(model.Signature, validationParams, out token);
                    var evidence = 
                        Encoding.UTF8.GetString(
                            Convert.FromBase64String(
                                ValueOrDefault(principal, "evidence", "")));
                    var ppidClaim = this.PpidClaimType(selectedSignMethod);
                    var ppid = ValueOrDefault(principal, ppidClaim, "N/A");
                    var issuer = ValueOrDefault(principal, "iss", "N/A");
                    var encoding = GetEncoding(selectedSignMethod);
                    var signText =
                        encoding.GetString(Convert.FromBase64String(
                            ValueOrDefault(principal, "signtext", "N/A")));
                    var displayModel = new SignatureModel {
                        Evidence = evidence,
                        Ppid = ppid,
                        SignText = signText,
                        Issuer = issuer
                    };
                    return this.View("SignatureResult", displayModel);
                });
        }
    }
}