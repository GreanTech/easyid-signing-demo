# Welcome to the easyID document signature demo repository

*If you need a legal agreement with your customers, then look no further. `easyID` will get you going in a few minutes*

To make this happen on your own website(s), you will need 2 things:
- An `easyID` account (you can sign up for a free test account on [grean.com](https://www.grean.com))
- A small amount of work on your website

For the purpose of illustration, we'll pretend that this demo web app is your actual web site.
You can use the guide below, and the contents of this repository, as a step-by-step guide to 
providing your actual website with signed agreements from your users.

Start by cloning the repository to your development box, and open the .sln file in VS 2015.
Press play (`F5`) to start, and you are ready to try the signature flow.

## Runtime
Enter the text you would like to have signed in the input field.
Select the desired method for document signing in the dropdown, and the process starts by 
a browser redirection to `easyID`.

`easyID` will then start and run the flow for the selected signature method, and when done, 
`easyID` will deliver the signed document back to your website.

Once delivered, you can use your designated JWT signature validation library to ensure that the
response did indeed come from your `easyID` tenant, and you must validate that the text which was 
signed matches what you asked `easyID` to sign.

If so, you must store the raw response, along with the JWT signing keys, 
in your data store of choice. That is required in case a user ever questions
the validity of the agreement later on.

And that's all there is to it - your web site can now go ahead and proceed with whatever was agreed
upon by the user.

### Notes
The UI uses full-blown browser redirection for the signature flow - as this is the 
simplest way, and also the one with the broadest reach (all signature methods supports this on all platforms).
Most of the methods can also be run inside an iframe, should you want a more embedded experience (white-labelling scenarios).
For an up-to-date view of the frame-able methods, check out the `easyID` authentication demo site on [GitHub](https://www.github.com/greantech/easyiddemo)

The signed document is delivered with an HTTP POST to your website, on a URL that is entirely under
your control. You have to pre-register the desired target URL in `easyID` first, though, to avoid 
phishing attacks (and the like) against your users. You can roundtrip dynamic state parameters through 
`easyID` by adding them to the query string. 

## Technical context
This demo contains the code needed to get the signing process up and running on .NET 4.6.1.
But any platform that can validate asymmetrically signed Json Web Tokens can be used.
(for a very comprehensive overview of such libraries for various platforms, check out `Auth0's` [JWT.io](https://jwt.io)).

The demo runs in IISExpress on port 44300, using the default-generated SSL certificate for localhost.
It is configured to run against the online test-and-development instance of `easyID`, which, in turn, 
has been set up to accept signing requests from `https://localhost:44300`. 

You can of course use your own `easyID` tenant for this as well. In that case, you'll need to register 
an application in `easyID` with a proper returnUrl (value for this demo is `https://localhost:44300/Signature/Done`), 
and change the values of the following `appSetting` entries in `web.config`:
- `easyid:tenantAuthority`
- `easyid:applicationRealm`


## Points of interest in the code
The HTTP layer consists of a single controller: `SignatureController`. 
The controller handles the routing, which is not very advanced here, and uses the classes in
the `easyIDSignature.cs` module to handle the interation with `easyID`.

The `SignatureController` has 3 action methods:
- `Text` (HTTP GET): Displays the landing page
- `Text` (HTTP POST): Receives the designated text-to-sign, and the selected signature method. Starts the signing process with `easyID`.
- `Done` (HTTP POST): Receives the signed document from `easyID`, and does the validation.
Check the comments in the code for further details - and please do send us a 
[Pull Request](https://github.com/greantech/easyid-signing-demo/pulls)
if you have suggestions, or find that the documentation is lacking.

The `easyIDSignature.cs` module wraps the details of .NET's JWT parsing and validation, 
and the peculiarities of the different signature schemes supported by `easyID`.
Expect quite a few nitty-gritty details if you decide to take a peek at it, but we
certainly encourage you to do so: If you are already familiar with OpenID Connect and JWT's, it 
should contain no surprises. If not, we hope it can serve as a starting point for getting aqcuainted
with these matters.



