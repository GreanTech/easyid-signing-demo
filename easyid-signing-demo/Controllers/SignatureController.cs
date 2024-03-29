﻿using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace signature_demo.Controllers
{    
    public class SignatureController : Controller
    {
        // Very Poor Mans dependency injection used here.
        // Replace with your favorite wire-up methodology.
        private static readonly SignatureRequester signatureRequester;
        // An in-memory agreement repository, just to illustrate the idea
        // of pairing the per-request-id state to an actual agreement text.
        // That is not what you would want to do on your actual website.
        // Replace the in-memory agreement store with what is appropriate for your scenario.
        private class Agreement
        {
            public Guid Id;
            public string Text;
        }
        private static ConcurrentDictionary<Guid, Agreement> agreementRequestRepository;

        static SignatureController()
        {
            signatureRequester =
                new SignatureRequester(
                    ConfigurationManager.AppSettings["verify:tenantAuthority"],
                    ConfigurationManager.AppSettings["verify:applicationRealm"]);

            agreementRequestRepository = new ConcurrentDictionary<Guid, Agreement>();
        }

        private TrustedSenderRepresentation TrustedSender
        {
            get
            {
                return new TrustedSenderRepresentation {
                    Origin = signatureRequester.SignerAuthority
                };
            }
        }
        // Set up the landing page with the available signing methods
        [HttpGet]
        public ActionResult Text()
        {
            return View(this.TrustedSender);
        }

        [HttpGet]
        public ActionResult Framed()
        {
            return View(this.TrustedSender);
        }

        // The text-to-sign is post'ed here, as specified by the user.
        [HttpPost]
        public ActionResult Text(
            string textToSign, 
            string selectedSignMethod, 
            string responseStrategy)
        {
            if (string.IsNullOrWhiteSpace(textToSign))
                textToSign = "The agreement to sign goes here";

            // Set up the desired target URL, so Criipto Verify knows where to POST
            // the signature once the user has signed the text
            var currentAuthority = this.Request.Url.GetLeftPart(UriPartial.Authority);
            var ub = new UriBuilder(currentAuthority);
            // Poor Man's Routing
            ub.Path = "/Signature/Done";
            // simplistic state machine that roundtrips the actual signature method 
            // and the unique requestId via Criipto Verify.
            var requestId = Guid.NewGuid();
            this.PushRequestState(requestId, textToSign);
            ub.Query = 
                WebUtility.UrlEncode(
                    String.Format(CultureInfo.InvariantCulture,
                    "selectedSignMethod={0}&state={1}",
                    selectedSignMethod,
                    requestId.ToString()));
            var replyTo = ub.Uri;

            var signerUrl = 
                signatureRequester.AsRedirectUrl(
                    textToSign, selectedSignMethod, replyTo, responseStrategy);
            return this.Redirect(signerUrl);
        }

        // This demo uses an in-memory dictionary as a backing store.
        // You could equally well use any persistent key-value storage engine for this.
        // As the user controls the text to sign here, we just create an agreement on-the-fly.
        private void PushRequestState(Guid id, string textToSign)
        {
            var agreement = new Agreement { Id = id, Text = textToSign };
            agreementRequestRepository.TryAdd(id, agreement);
        }

        private string PopRequestState(Guid id)
        {
            Agreement a = null;
            agreementRequestRepository.TryRemove(id, out a);
            return a.Text;
        }

        // The response from Criipto Verify is post'ed here, because the `replyTo` variable in the 
        // HTTP POST action `Text` points to this path.
        // This demo implementation builds a view model with some select properties.
        // In real-life scenarios, you would want to store
        //  - the raw signature as POST'ed by Criipto Verify (the `signature` parameter)
        //  - the Json Web Key(s) used for validating the JWT signature.
        //    These are present in serialized form in the `EndorsingKeys` property
        //    on the return value from `signatureRequester.ValidateSignature`.
        // in your data store for compliance and non-repudiation purposes.
        [HttpPost]
        public async Task<ActionResult> Done(string signature, string selectedSignMethod, string state)
        {
            Guid requestId = Guid.Empty;
            if (!Guid.TryParse(state, out requestId))
            {
                ActionResult result = new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                return await Task.FromResult(result);
            }
            var expectedText = this.PopRequestState(requestId);
            if (string.IsNullOrWhiteSpace(expectedText))
            {
                ActionResult result = new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
                return await Task.FromResult(result);
            }

            var displayModel = 
                await signatureRequester.ValidateSignature(signature, selectedSignMethod);
            if (displayModel.SignText != expectedText)
            {
                ActionResult result = new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                return await Task.FromResult(result);
            }

            return this.View("SignatureResult", displayModel);
        }

        // An entirely forgiving endpoint, which is ONLY useful for displaying 
        // the contents of a response from Criipto Verify>
        // It does not check if the user has altered the text to be signed. 
        // Do NOT, under ANY circumstances, take this approach for production purposes.
        [HttpPost]
        public async Task<ActionResult> Show(string signature, string selectedSignMethod)
        {
            var displayModel =
                await signatureRequester.ValidateSignature(signature, selectedSignMethod);
            return this.View("SignatureResult", displayModel);
        }
    }
}