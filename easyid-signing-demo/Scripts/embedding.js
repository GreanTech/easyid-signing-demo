function showInFrame() {
    $('#signForm').hide();
    let frame = document.getElementById('signFrame');
    frame.setAttribute('class', 'visible-frame');
    frame.src = "/Signature/Framed";
}

function showInDocument() {
    let frame = document.getElementById('signFrame');
    frame.setAttribute('class', 'hidden-frame');
    delete frame.src;
    $('#signForm').show();
}