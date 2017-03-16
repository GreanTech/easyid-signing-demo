function hideResults() {
    $('#rawSignature').hide();
    $('#signatureResult').hide();
}

function showInFrame() {
    hideResults();
    $('#signForm').hide();
    let frame = document.getElementById('signFrame');
    frame.setAttribute('class', 'visible-frame');
    frame.src = "/Signature/Framed";
    $('#signFrame').show();
}

function showInDocument() {
    hideResults();
    $('#signFrame').hide();
    let frame = document.getElementById('signFrame');
    frame.setAttribute('class', 'hidden-frame');
    delete frame.src;
    $('#signForm').show();
}