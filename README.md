# Welcome to the Criipto Verify document signature demo repository

*If you need a legal agreement with your customers, then look no further. `Criipto Verify` will get you going in a few minutes*

To make this happen on your own website(s), you will need 2 things:
- An `Criipto Verify` account (you can sign up for a free test account on [criipto.com](https://www.criipto.com))
- A small amount of work on your website

For the purpose of illustration, we'll pretend that this demo web app is your actual web site.
You can use the guide below, and the contents of this repository, as a step-by-step guide to 
providing your actual website with signed agreements from your users.

Start by cloning the repository to your development box, and open the .sln file in VS 2015.
Press play (`F5`) to start, and you are ready to try the signature flow.

## Runtime
Enter the text you would like to have signed in the input field.
Select the desired method for document signing in the dropdown, and the process starts by 
a browser redirection to `Criipto Verify` (potentially inside an iframe).

`Criipto Verify` will then start and run the flow for the selected signature method, and when done, 
`Criipto Verify` will deliver the signed document back to your website.

Once delivered, you can use your designated JWT signature validation library to ensure that the
response did indeed come from your `Criipto Verify` tenant, and you must validate that the text which was 
signed matches what you asked `Criipto Verify` to sign. This value is roundtripped by `Criipto Verify` in the 
`signtext` property in the returned response.

You must store the raw response in your backend data store of choice, along with 
the JWT signing keys which can be fetched from your `Criipto Verify` tenant's OpenID Connect discovery endpoint. 
That is required in case a user ever questions the validity of the agreement later on. 

And that's all there is to it - your web site can now go ahead and proceed with whatever was agreed
upon by the user.

### Notes
You have 2 options for running the `Criipto Verify` signature flow: You can either use
a full-blown browser redirection, or you can run it inside an iframe. This demo site let's you choose which one
to test - but beware (see below) that not all signature methods work with the iframed approch.
The first option (redirection) is the simplest way, and also the one with the broadest reach 
(all signature methods supports this on all platforms). The second option (iframed) can be used with
most signature methods - except same-device Swedish BankId on most mobile operating systems (with Windows Phone 10 being a curious exception).
For an up-to-date view of the frame-able methods, check out the `Criipto Verify` authentication demo site on [GitHub](https://www.github.com/greantech/easyiddemo)

For a redirection-based integration, you can choose between two different delivery mechanisms for the resulting signed document: 
- HTTP POST (the default): Result is sent to your backend for validation via an auto-submitting form.
- URL fragment: Result 'reference' is sent in the URL fragment (aka location hash), via a 302 Redirect from `Criipto Verify`.
Add a `responseStrategy=urlFragment` query string parameter in the request to `Criipto Verify` to use this delivery mechanism. 
You can then open an invisible iframe using the URL present in the fragment, which will trigger a `window.postMessage` event along the
same lines as the iframe-based integration described below. `Criipto Verify` does not support direct delivery of the result in a GET response,
because in most cases, the size of the result would create a URL that exceeds the URL length limitations enforced by most browsers.

For iframe-based integrations, you have the additional option of the following delivery mechanism:
- DOM event: Result is made available in the browser via the `window.postMessage` API. 
Add a `responseStrategy=postMessage` query string parameter in the request to `Criipto Verify` to use this delivery mechanism.

In any case, the signed document is delivered on a URL, or window, which is entirely under
your control. You have to pre-register the desired target URL in `Criipto Verify` first, though, to avoid 
phishing attacks (and the like) against your users. You can roundtrip dynamic state parameters through 
`Criipto Verify` by adding them to the query string. 

## Technical context
This demo contains the code needed to get the signing process up and running on .NET 4.6.1.
But any platform that can validate asymmetrically signed Json Web Tokens can be used.
(for a very comprehensive overview of such libraries for various platforms, check out `Auth0's` [JWT.io](https://jwt.io)).

The demo runs in IISExpress on port 44300, using the default-generated SSL certificate for localhost.
It is configured to run against the online test-and-development instance of `Criipto Verify`, which, in turn, 
has been set up to accept signing requests from `https://localhost:44300`. 

You can of course use your own `Criipto Verify` tenant for this as well. In that case, you'll need to register 
an application in `Criipto Verify` with a proper returnUrl (value for this demo is `https://localhost:44300/Signature/Done`), 
and change the values of the following `appSetting` entries in `web.config`:
- `verify:tenantAuthority`
- `verify:applicationRealm`

## Points of interest in the code
The HTTP layer consists of a single controller: `SignatureController`. 
The controller handles the routing, which is not very advanced here, and uses the classes in
the `easyIDSignature.cs` module to handle the interation with `Criipto Verify`.

The `SignatureController` has 3 action methods:
- `Text` (HTTP GET): Displays the landing page
- `Text` (HTTP POST): Receives the designated text-to-sign, and the selected signature method. Starts the signing process with `Criipto Verify`.
- `Done` (HTTP POST): Receives the signed document from `Criipto Verify`, and does the validation.
Check the comments in the code for further details - and please do send us a 
[Pull Request](https://github.com/greantech/easyid-signing-demo/pulls)
if you have suggestions, or find that the documentation is lacking.

The `easyIDSignature.cs` module wraps the details of .NET's JWT parsing and validation, 
and the peculiarities of the different signature schemes supported by `Criipto Verify`.
Expect quite a few nitty-gritty details if you decide to take a peek at it, but we
certainly encourage you to do so: If you are already familiar with OpenID Connect and JWT's, it 
should contain no surprises. If not, we hope it can serve as a starting point for getting aqcuainted
with these matters.



