// Página abrir-app: gera o QR code do link atual e aponta o "Voltar à galeria" para a praça certa.
const GALERIA = "https://repraca.feliperpv.com/galeria/";
const params = new URLSearchParams(window.location.search);

// --- QR code com o URL atual, para quem está no PC abrir a página no telemóvel ---
const caixaQR = document.getElementById("qrcode");

new QRCode(caixaQR, {
    text: window.location.href,
    width: 168,
    height: 168,
    colorDark: "#131C3B",
    colorLight: "#ffffff",
    correctLevel: QRCode.CorrectLevel.M // H falha em alguns tamanhos de URL nesta biblioteca
});
caixaQR.title = ""; // a biblioteca põe o URL como tooltip


// --- "Voltar à galeria" ---
// ?mapa=<slug>               (botão "Reimaginar na app") -> página da praça
// ?id=<uuid>&praca=<slug>    (botão reMixar da galeria)  -> pasta dessa versão
// só ?id=<uuid> (links antigos)                          -> galeria geral
// Nota: a galeria usa "praca" e não "mapa" porque a app recusa links com id e mapa ao mesmo tempo.
const slugValido = /^[a-z0-9-]+$/;
const id = params.get("id");
const slug = params.get("mapa") || params.get("praca");

if (slug && slugValido.test(slug)) {
    let destino = GALERIA + "praca/" + slug + "/";
    if (id) destino += "?id=" + encodeURIComponent(id);
    document.getElementById("voltarGaleria").href = destino;
}
