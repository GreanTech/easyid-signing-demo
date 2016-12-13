using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Web;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.WsFederation;
using Owin;
using System.Threading.Tasks;

namespace iframe_demo
{
    public partial class Startup
    {
        private static string realm = ConfigurationManager.AppSettings["ida:Wtrealm"];
        private static string adfsMetadata = ConfigurationManager.AppSettings["ida:ADFSMetadata"];

        public void ConfigureAuth(IAppBuilder app)
        {
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions());

            app.UseWsFederationAuthentication(
                new WsFederationAuthenticationOptions
                {
                    Wtrealm = realm,
                    MetadataAddress = adfsMetadata,
                    Notifications = new WsFederationAuthenticationNotifications
                    {
                        SecurityTokenReceived = (context) =>
                        {
                            return Task.FromResult(0);
                        },
                        RedirectToIdentityProvider = (context) =>
                        {
                            var authMethod = context.Request.Query["method"];
                            switch (authMethod)
                            {
                                case "nobid-mobile":
                                    context.ProtocolMessage.Wauth = "urn:grn:authn:no:bankid:mobile";
                                    break;
                                case "nobid-central":
                                    context.ProtocolMessage.Wauth = "urn:grn:authn:no:bankid:central";
                                    break;
                                case "sbid":
                                    context.ProtocolMessage.Wauth = "urn:grn:authn:se:bankid:another-device";
                                    break;
                                case "sbid-local":
                                    context.ProtocolMessage.Wauth = "urn:grn:authn:se:bankid:same-device";
                                    break;
                                case "dknemid-poces":
                                context.ProtocolMessage.Wauth = "urn:grn:authn:dk:nemid:poces";
                                    break;
                                case "dknemid-moces":
                                    context.ProtocolMessage.Wauth = "urn:grn:authn:dk:nemid:moces";
                                    break;
                                case "dknemid-moces-codefile":
                                    context.ProtocolMessage.Wauth = "urn:grn:authn:dk:nemid:moces:codefile";
                                    break;
                                default:
                                    context.ProtocolMessage.Wauth = "";
                                    break;
                            }

                            var signText = context.Request.Query["signtext"];
                            if (signText != null && signText.Length > 0)
                            {
                                var url = new UriBuilder(context.ProtocolMessage.IssuerAddress);
                                url.Path = "/passive/sign";
                                context.ProtocolMessage.IssuerAddress = url.ToString();
                                context.ProtocolMessage.SetParameter("signtext", signText);
                            }
                            var ssn = context.Request.Query["ssn"];
                            if (ssn != null && ssn.Length > 0)
                                context.ProtocolMessage.SetParameter("ssn", ssn);

                            return Task.FromResult(0); 
                        }
                    }
                });
        }
    }
}