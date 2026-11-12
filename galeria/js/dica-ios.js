// Página abrir-app: no iPhone com Safari mostra uma seta para o botão ABRIR da faixa do Safari.
// A página não consegue saber se a app está instalada nem se a faixa está visível
// (o iOS não diz), por isso o texto é condicional: "Já tens a app?".
// A faixa não aparece noutros browsers nem dentro de apps (Instagram, WhatsApp, Facebook, Chrome...).
const ua = navigator.userAgent;
const ehIphone = /iPhone|iPod/.test(ua);
const ehSafari = /Safari/.test(ua) && !/CriOS|FxiOS|EdgiOS|OPiOS|GSA|FBAN|FBAV|Instagram|Line\/|WhatsApp|Snapchat|Twitter/.test(ua);

if (ehIphone && ehSafari) {
    document.documentElement.classList.add("ios-safari");
    document.getElementById("dicaIos").hidden = false;
}
