using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace signature_demo.Controllers
{
    public class SignatureController : Controller
    {
        // Very Poor Mans dependency injection used here.
        // Replace with your favorite wire-up methodology.
        private static readonly SignatureRequester signatureRequester;

        static SignatureController()
        {
            signatureRequester =
                new SignatureRequester(
                    ConfigurationManager.AppSettings["easyid:tenantAuthority"],
                    ConfigurationManager.AppSettings["easyid:applicationRealm"]);
        }

        // Set up the landing page with a default signing method selection
        [HttpGet]
        public ActionResult Text()
        {
            return View();
        }

        // The text-to-sign is post'ed here
        [HttpPost]
        public ActionResult Text(string textToSign, string selectedSignMethod)
        {
            // Set up the desired target URL, so easyID knows where to POST
            // the signature once the user has signed the text
            var currentAuthority = this.Request.Url.GetLeftPart(UriPartial.Authority);
            var ub = new UriBuilder(currentAuthority);
            // Poor Man's Routing
            ub.Path = "/Signature/Done";
            // simplistic state machine that roundtrips the actual signature method via easyID.
            ub.Query = "selectedSignMethod=" + selectedSignMethod;
            var replyTo = ub.Uri;

            var signerUrl = signatureRequester.AsRedirectUrl(textToSign, selectedSignMethod, replyTo);
            return this.Redirect(signerUrl);
        }

        // The response from easyID is post'ed here, because the replyTo variable in the 
        // HTTP POST action Text just above says so.
        // This demo implementation builds a view model with some select properties.
        // In real-life scenarios, you would want to store
        //  - the raw signature as POST'ed by easyID
        //  - the Json Web Key(s) used for validating the JWT signature
        // in your data store for compliance and non-repudiation purposes.
        [HttpPost]
        public async Task<ViewResult> Done(string signature, string selectedSignMethod)
        {
            var displayModel = 
                await signatureRequester.ValidateSignature(signature, selectedSignMethod);
            return this.View("SignatureResult", displayModel);
        }
    }
}