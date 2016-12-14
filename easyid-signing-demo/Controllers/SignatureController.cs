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
        private readonly string signerAuthority = "https://easyid.localhost";
        private readonly string realm = "urn:grn:app:easyid-signing-demo";
        private readonly string signWith = "urn:grn:authn:no:bankid:central";
        private readonly string ppidClaim = "ssn";

        // GET: Signature
        public ActionResult Text()
        {
            return View(new SignModel());
        }

        private Encoding GetEncoding(string signMethod)
        {
            if (signMethod.StartsWith("urn:grn:authn:no:bankid"))
            {
                return Encoding.GetEncoding("ISO-8859-1");
            }

            return Encoding.UTF8;
        }

        // POST: Signature
        [HttpPost]
        public ActionResult Text(SignModel model)
        {
            var replyTo = "https://localhost:44300/Signature/Done";
            var encoding = GetEncoding(signWith);
            var signText = Convert.ToBase64String(encoding.GetBytes(model.TextToSign));
            var signerUrl =
                String.Format(
                    CultureInfo.InvariantCulture,
                    "{0}/sign/text/?wa=wsignin1.0&wtrealm={1}&wreply={2}&wauth={3}&signtext={4}",
                    signerAuthority,
                    realm,
                    replyTo,
                    signWith,
                    signText);
            return this.Redirect(signerUrl);
        }

        private string ValueOrDefault(ClaimsPrincipal p, string claimtype, string def)
        {
            var c = p.FindFirst(claimtype);
            if (c == null) return def;
            return c.Value;
        }

        public Task<ViewResult> Done(SignedModel model)
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
                    var ppid = ValueOrDefault(principal, ppidClaim, "N/A");
                    var issuer = ValueOrDefault(principal, "iss", "N/A");
                    var encoding = GetEncoding(signWith);
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