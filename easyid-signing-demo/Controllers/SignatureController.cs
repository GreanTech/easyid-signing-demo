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

        // The text-to-sign is post'ed here, as specified by the user.
        // That is not what you would want to do on your actual website.
        // More likely, you have an agreement repository somewhere, and 
        // you roundtrip the Id of the agreement via the browser.
        // You must keep track of the selected Id server-side, as it is
        // needed for validation later on.
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

        // The response from easyID is post'ed here, because the `replyTo` variable in the 
        // HTTP POST action `Text` points to this path.
        // This demo implementation builds a view model with some select properties.
        // In real-life scenarios, you would want to store
        //  - the raw signature as POST'ed by easyID (the `signature` parameter)
        //  - the Json Web Key(s) used for validating the JWT signature.
        //    These are present in serialized form in the `EndorsingKeys` property
        //    on the return value from `signatureRequester.ValidateSignature`.
        // in your data store for compliance and non-repudiation purposes.
        [HttpPost]
        public async Task<ViewResult> Done(string signature, string selectedSignMethod)
        {
            var displayModel = 
                await signatureRequester.ValidateSignature(signature, selectedSignMethod);
            // Get the expected text by looking it up in your agreement repository
            // based on the Id stored in the users session. This demo let's the
            // user control the text to sign, so we'll let it pass.
            var expectedText = displayModel.SignText;
            if (displayModel.SignText != expectedText)
                throw new InvalidOperationException("The signed text has been modified in-flight.");
            return this.View("SignatureResult", displayModel);
        }
    }
}