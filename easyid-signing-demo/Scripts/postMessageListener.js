var eventMethod = window.addEventListener ? "addEventListener" : "attachEvent";
var eventer = window[eventMethod];
var messageEvent = eventMethod == "attachEvent" ? "onmessage" : "message";
var ensureSlashTerminated = function (s) {
    if (s.endsWith("/")) {
        return s;
    }

    return s + "/";
}

// Listen to message from child window and send the user to the desired target URL. 
// For this demo, we stay on the home page, but you could certainly add some more
// refined logic for taking the user to a better place.
eventer(messageEvent, function (e) {
    var signerAuth = $('input[type="hidden"][name="signerAuthority"]').attr('value');
    var trustedOrigin = ensureSlashTerminated(signerAuth);

    if (e && e.data) {
        console.log("Received postMessage event with signature (event origin " + e.origin + ")");
        let eventOrigin = ensureSlashTerminated(e.origin);
        if (eventOrigin === trustedOrigin) {
            console.log("Message is from trusted authority " + trustedOrigin);
            if (e.data.signature) {                
                $('iframe').hide();
                let pre = document.createElement("pre");
                pre.innerText = e.data.signature;
                let rawSig = $('#rawSignature')
                rawSig.append(pre);
                rawSig.show();
            }
        }
    }
}, false);