// Página abrir-app: no Android mostra o botão "Abrir na app".
// Usa um link intent:// com o mesmo endereço desta página: se a app estiver instalada,
// o Android abre-a com este link (o DeepLinkManager lê o ?id= / ?mapa=);
// se não estiver, segue o browser_fallback_url e vai para o Google Play.
// Serve para quando o App Link não abre a app sozinho (link não verificado,
// link aberto dentro do Instagram/Facebook, colado na barra de endereço...).
const PLAY_STORE = "https://play.google.com/store/apps/details?id=com.frpv.rePraca";

if (/Android/i.test(navigator.userAgent)) {
    const destino = window.location.host + window.location.pathname + window.location.search;
    const intent = "intent://" + destino +
        "#Intent;scheme=https;package=com.frpv.rePraca;" +
        "S.browser_fallback_url=" + encodeURIComponent(PLAY_STORE) + ";end";

    document.getElementById("abrirApp").href = intent;
    document.documentElement.classList.add("android");
    document.getElementById("blocoAndroid").hidden = false;
}
