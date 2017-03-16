var signMethods = [
    {
        Id: "urn:grn:authn:no:bankid:central",
        DisplayName: "NO BankID kodebrikke"
    },
    {
        Id: "urn:grn:authn:no:bankid:mobile",
        DisplayName: "NO BankID mobil"
    },
    {
        Id: "urn:grn:authn:se:bankid:same-device",
        DisplayName: "SE BankID denna enhet"
    },
    {
        Id: "urn:grn:authn:se:bankid:another-device",
        DisplayName: "SE BankID annan enhet"
    },
    {
        Id: "urn:grn:authn:dk:nemid:poces",
        DisplayName: "DK NemID privat"
    },
    {
        Id: "urn:grn:authn:dk:nemid:moces",
        DisplayName: "DK NemID erhverv"
    },
    {
        Id: "urn:grn:authn:dk:nemid:moces:codefile",
        DisplayName: "DK NemID nøglefil (erhverv)"
    }
];

var dropdown = $("#signMethods");
$.each(signMethods, function (index, m) {
    var li = $('<li/>')
        .attr('role', 'menuitem')
        .appendTo(dropdown);
    var aaa = $('<a/>')
        .attr("data-value", m.Id)
        .text(m.DisplayName)
        .appendTo(li);
});

if (top !== self) {
    $('#responseStrategy').show();
} else {
    $('#responseStrategy').hide();
}