// Gera um QR code com o URL atual, para quem está no PC abrir a página no telemóvel.
const url = window.location.href;
const caixaQR = document.getElementById("qrcode");
 
if (!url.startsWith("http")) {
  // Aberto como ficheiro local (file://): o telemóvel não consegue abrir este endereço.
  caixaQR.textContent = "O QR code só funciona com a página publicada no site.";
} else {
  new QRCode(caixaQR, {
    text: url,
    width: 180,
    height: 180,
    colorDark: "#000000",
    colorLight: "#ffffff",
    correctLevel: QRCode.CorrectLevel.M // H é desnecessário sem logótipo e falha em alguns tamanhos de URL nesta biblioteca
  });
}
 