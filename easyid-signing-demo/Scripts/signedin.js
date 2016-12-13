
function ready(cpr, validated) {
    window.postMessage({ cpr: cpr, validated: validated });
}

