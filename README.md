# Welcome to the easyID digital agreement signatures demo repository

*If you need a legally binding signature on some text document from your customers - then look no further.
`easyID` will get you going in a few minutes*

To make this happen on your own website(s), you'll need 2 things:
- An `easyID` account (you can sign up for a free test account on [grean.com](https://www.grean.com))
- A small amount of work on your website

You can use the guide below, and the contents of this repository, as a step-by-step guide to providing your website with signed agreements from your users.

## A bit of technical context
This demo contains the code needed to get the signing process up and running on .NET.
But any platform that can validate asymmetrically signed Json Web Tokens can be used.
(for a very comprehensive overview of libraries for various platforms, check out `Auth0`s [JWT.io](https://jwt.io)).

The demo runs in IISExpress on port 44300, using the default-generated SSL certificate for localhost.
It is configured to run against the online test-and-development instance of `easyID`, which, in turn, 
has been set up to accept signing requests from `https://localhost:44300`. 
So after downloading the repository and opening the solution, you are all set to go when you open
the solution in VS 2015.

You can of course use your own `easyID` tenant for this as well. In that case, you'll need to register 
an application in `easyID` with a proper returnUrl (value for this demo is `https://localhost:44300/Signature/Done`), 
and change the values of the following `appSetting` entries in 'web.config':
- `easyid:signerAuthority`
- `easyid:clientid`

## Runtime
Point your browser to the root of the application, and enter the text you would like to have signed 
in the input field. Select the desired method for siging, and let the process run its course.

## Demo code walkthrough
The HTTP layer consists of a single controller: `SignatureController`. 
The controller handles the routing, which is not very advanced here, and uses the classes in
the `easyIDSignature.cs` module to handle the interation with `easyID`.
The module also wraps the details of .NET's JWT parsing and validation, 
and the peculiarities of the different signature schemes supported by `easyID`.

The UI uses full-blown browser redirection for the signature flow - as this is the 
simplest way, and also the one with the broadest reach (all signature methods supports this on all platforms).
Most of the methods can also be run inside an iframe, should you want a more embedded experience (white-labelling scenarios).
For an up-to-date view of the frame-able methods, check out the `easyID` authentication demo site on [GitHub](https://www.github.com/greantech/easyiddemo)

