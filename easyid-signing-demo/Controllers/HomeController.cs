using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Security.Claims;
using Microsoft.Owin.Security.WsFederation;
using Microsoft.Owin.Security;

namespace iframe_demo.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index( string ssn, string method )
        {
            if (!Request.IsAuthenticated)
            {
                HttpContext.GetOwinContext().Authentication.Challenge(new AuthenticationProperties { RedirectUri = "/" },
                    WsFederationAuthenticationDefaults.AuthenticationType);
                return null;
            }
            else
            {
                var identity = User.Identity as ClaimsIdentity;
                var cpr = identity.Claims.Where(c => c.Type == "dk:gov:saml:attribute:CprNumberIdentifier")
                                   .Select(c => c.Value).SingleOrDefault();
                ViewBag.isValidated = (cpr != null && cpr.Length >= 10);
                ViewBag.cpr = cpr;
                return View();
            }
        }
    }
}