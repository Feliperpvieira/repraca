// ==========================================
// 1. CONFIGURAÇÃO SUPABASE
// ==========================================
const supabaseUrl = 'https://ldynpvhqbmrcrlcabnuf.supabase.co';
const supabaseKey = 'sb_publishable_qtshAGmadXj9SbNhrgJOXg_lFROY3Yb';

// Usamos window.supabase e guardamos na variável "db"
const db = window.supabase.createClient(supabaseUrl, supabaseKey);

// ==========================================
// 1b. DICIONÁRIO DE ÍCONES (fonte única — usado no popup E no filtro)
// ==========================================
// Coloque as imagens pequenas na pasta /icones/ (pode reaproveitar as
// "imagemObjeto" que já existem nos ScriptableObjects do Unity,
// exportadas como PNG). Itens sem entrada aqui caem no genérico.png —
// crie esse arquivo também, como fallback.
const iconePorNome = {
    "Banco de concreto": "banco-concreto.png",
    "Banco de madeira": "banco-madeira.png",
    "Barra Fixa": "barra-fixa.png",
    "Bicicletário U": "bicicletario-u.png",
    "Cereja-do-mato": "cereja-do-mato.png",
    "Cesta de Basquete": "cesta-basquete.png",
    "Escorrega": "escorrega.png",
    "Estátua": "estatua.png",
    "Gangorra": "gangorra.png",
    "Lixeira Comlurb": "lixeira-comlurb.png",
    "Mesa de xadrez": "mesa-xadrez.png",
    "MUPI": "mupi.png",
    "Palmeira": "palmeira.png",
    "Poste duplo": "poste-duplo.png",
};

function iconeParaItem(nome) {
    return "icones/" + (iconePorNome[nome] || "generico.png");
}

// Preenche o <select id="filtroEspecifico"> a partir desse mesmo
// dicionário, em vez de manter uma segunda lista de itens só pro filtro.
// Cada <option> ganha um <img> (ícone) + o nome — em navegadores sem
// suporte a "appearance: base-select" (ver style.css) o <img> é
// simplesmente descartado e sobra só o texto, então nada quebra.
function preencherFiltroDeItens() {
    const select = document.getElementById("filtroEspecifico");
    Object.keys(iconePorNome).sort().forEach(nome => {
        const opt = document.createElement("option");
        opt.value = nome;

        const img = document.createElement("img");
        img.src = iconeParaItem(nome);
        img.alt = "";
        img.className = "opcao-icone";

        opt.appendChild(img);
        opt.appendChild(document.createTextNode(nome));

        select.appendChild(opt);
    });
}
preencherFiltroDeItens();

// Variáveis de Estado
let paginaAtual = 0;
const itensPorPagina = 12;
let carregando = false;
let chegouAoFim = false;

// Elementos da UI
const gallery = document.getElementById("gallery");
const popup = document.getElementById("popup");
const loader = document.getElementById("fim-da-pagina");

// ==========================================
// 2. BUSCA DE DADOS E SCROLL INFINITO
// ==========================================
async function carregarPracas() {
    if (carregando || chegouAoFim) return;
    carregando = true;

    // Lemos o que o utilizador escolheu nos filtros
    const ordem = document.getElementById("filtroOrdem").value;
    const minItens = parseInt(document.getElementById("filtroItens").value);
    const itemEspecifico = document.getElementById("filtroEspecifico").value;

    const inicio = paginaAtual * itensPorPagina;
    const fim = inicio + itensPorPagina - 1;

    // Usamos "db" em vez de "supabase".
    // Quando filtra por item específico, chamamos uma função SQL (RPC) em
    // vez de montar o filtro com .ilike() direto no client — o Postgres
    // exige um cast (layout_data::text) pra comparar jsonb com ilike, e
    // esse cast não estava sendo repassado corretamente pelos filtros do
    // PostgREST client-side (voltava sempre o mesmo erro "operator does
    // not exist: jsonb ~~* unknown", mesmo escrevendo o cast). A função
    // SQL já faz esse cast por dentro, então evita o problema — veja
    // filtrar_pracas_por_item.sql pra criar essa função no Supabase.
    const colunas = 'praca_id, image_topo_url, created_at, likes, total_objects';
    let query;
    if (itemEspecifico !== "todos") {
        query = db.rpc('filtrar_pracas_por_item', { item_nome: itemEspecifico }).select(colunas);
    } else {
        query = db.from('city_creations').select(colunas);
    }

    // Aplica o filtro de Mínimo de Itens
    query = query.gte('total_objects', minItens);

    // Aplica a Ordenação
    if (ordem === "recentes") query = query.order('created_at', { ascending: false });
    if (ordem === "antigas") query = query.order('created_at', { ascending: true });
    if (ordem === "likes") query = query.order('likes', { ascending: false });

    // Aplica a paginação
    const { data, error } = await query.range(inicio, fim);

    if (error) {
        console.error("Erro ao buscar:", error);
        loader.innerText = "Erro ao carregar: " + (error.message || JSON.stringify(error));
        carregando = false;
        return;
    }

    if (data.length < itensPorPagina) {
        chegouAoFim = true;
        loader.innerText = "Chegou ao fim da galeria!";
    }

    desenharCards(data);
    paginaAtual++;
    carregando = false;
}

function desenharCards(pracas) {
    pracas.forEach(praca => {
        const dataFormatada = new Date(praca.created_at).toLocaleDateString('pt-PT');

        const card = document.createElement("div");
        card.className = "card-praca";
        card.innerHTML = `
            <img src="${praca.image_topo_url}" class="card-img" loading="lazy" alt="Praça">
            <div class="card-info">
                <span class="card-data">📅 ${dataFormatada}</span>
                <span class="card-likes">❤️ ${praca.likes || 0}</span>
            </div>
        `;

        card.addEventListener("click", () => {
            window.location.hash = praca.praca_id;
        });

        gallery.appendChild(card);
    });
}

// O Observador que dispara quando chegamos ao fim da página
const observer = new IntersectionObserver((entradas) => {
    if (entradas[0].isIntersecting) {
        carregarPracas();
    }
});
observer.observe(loader);

// ==========================================
// 3. RECARREGAR AO MUDAR OS FILTROS
// ==========================================
function aplicarFiltros() {
    gallery.innerHTML = ""; 
    paginaAtual = 0;
    chegouAoFim = false;
    loader.innerText = "A carregar mais praças...";
    carregarPracas(); 
}

document.getElementById("filtroOrdem").addEventListener("change", aplicarFiltros);
document.getElementById("filtroItens").addEventListener("change", aplicarFiltros);
document.getElementById("filtroEspecifico").addEventListener("change", aplicarFiltros);

// ==========================================
// 4. SISTEMA DE HASH ROUTING
// ==========================================
window.addEventListener("hashchange", lidarComNavegacao);

async function lidarComNavegacao() {
    const hash = window.location.hash.replace("#", ""); 

    if (!hash) {
        popup.classList.add("escondido"); 
        document.body.style.overflow = "auto"; 
        return;
    }

    document.getElementById("modalTitulo").innerText = "A carregar dados...";
    popup.classList.remove("escondido");
    document.body.style.overflow = "hidden"; 

    // Usamos "db" em vez de "supabase".
    // NOTA: .eq('praca_id', hash).single() quebrava assim que o mesmo
    // praca_id passasse a ter mais de uma linha (cada edição salva de
    // novo). Agora buscamos sempre a versão mais recente.
    const { data, error } = await db
        .from('city_creations')
        .select('*')
        .eq('praca_id', hash)
        .order('created_at', { ascending: false })
        .limit(1)
        .single();

    if (data) {
        preencherModal(data);
    } else {
        alert("Praça não encontrada!");
        window.location.hash = ""; 
    }
}

document.getElementById("btnFecharModal").addEventListener("click", () => window.location.hash = "");
popup.addEventListener("click", (e) => {
    if (e.target === popup) window.location.hash = "";
});

// ==========================================
// 5. PREENCHER O MODAL
// ==========================================
let pracaAbertaId = null;

// Mapa nome do item -> arquivo de ícone dentro de /icones/.
function preencherModal(praca) {
    pracaAbertaId = praca.praca_id;

    // --- Imagens: abre sempre na vista de topo ---
    document.getElementById("selectedImage").src = praca.image_topo_url;
    document.getElementById("btnVistaTopo").classList.add("ativo");
    document.getElementById("btnVistaAngulo").classList.remove("ativo");

    document.getElementById("btnVistaTopo").onclick = () => {
        document.getElementById("selectedImage").src = praca.image_topo_url;
        document.getElementById("btnVistaTopo").classList.add("ativo");
        document.getElementById("btnVistaAngulo").classList.remove("ativo");
    };
    document.getElementById("btnVistaAngulo").onclick = () => {
        document.getElementById("selectedImage").src = praca.image_url;
        document.getElementById("btnVistaAngulo").classList.add("ativo");
        document.getElementById("btnVistaTopo").classList.remove("ativo");
    };

    document.getElementById("btnRemix").href = "https://feliperpv.com/repraca/galeria/abrir-app/?id=" + praca.praca_id;

    const jsonConvertido = JSON.parse(praca.layout_data);
    const itens = jsonConvertido.layoutDaPraca || [];

    // Agrupa por categoria e, dentro de cada categoria, conta por nome
    const itensPorCategoria = {};
    itens.forEach(item => {
        if (!itensPorCategoria[item.categoria]) itensPorCategoria[item.categoria] = {};
        itensPorCategoria[item.categoria][item.nome] = (itensPorCategoria[item.categoria][item.nome] || 0) + 1;
    });

    let categoriaPrincipal = "Mista";
    let max = 0;
    for (let cat in itensPorCategoria) {
        const totalNaCategoria = Object.values(itensPorCategoria[cat]).reduce((a, b) => a + b, 0);
        if (totalNaCategoria > max) {
            max = totalNaCategoria;
            categoriaPrincipal = cat;
        }
    }

    // "rePraça {numero}" = id da LINHA na tabela (não o praca_id, que é o UUID)
    document.getElementById("modalNumero").innerText = "rePraça " + praca.id;

    // Título = o nome que o criador deu à própria criação; se não tiver
    // (linhas antigas sem esse campo), cai pro nome da praça-base.
    const nomeBase = praca.nome_da_cena || jsonConvertido.nomeDaCena || "";
    const titulo = (praca.titulo && praca.titulo.trim()) ? praca.titulo : (nomeBase || "Praça Personalizada");
    document.getElementById("modalTitulo").innerText = titulo;

    const baseadoEmEl = document.getElementById("modalBaseadoEm");
    if (nomeBase && nomeBase !== titulo) {
        baseadoEmEl.innerText = "baseado em: " + nomeBase;
        baseadoEmEl.style.display = "";
    } else {
        baseadoEmEl.style.display = "none";
    }

    document.getElementById("modalData").innerText = "Última edição: " + praca.created_at.substring(0, 10).split('-').reverse().join('/');
    document.getElementById("modalTotalItens").innerText = praca.total_objects;
    document.getElementById("modalCategoria").innerText = categoriaPrincipal;

    const comentarioEl = document.getElementById("modalComentario");
    if (praca.comentario && praca.comentario.trim()) {
        comentarioEl.innerText = praca.comentario;
        comentarioEl.style.display = "";
    } else {
        comentarioEl.style.display = "none";
    }

    // Monta os grupos por categoria (com ícone + contagem) na aba "itens"
    const listaEl = document.getElementById("modalListaItens");
    listaEl.innerHTML = "";
    for (let categoria in itensPorCategoria) {
        const grupo = document.createElement("div");
        grupo.className = "grupo-categoria";

        const titulo = document.createElement("div");
        titulo.className = "grupo-categoria-titulo";
        titulo.innerText = categoria;
        grupo.appendChild(titulo);

        const linha = document.createElement("div");
        linha.className = "grupo-categoria-itens";
        for (let nome in itensPorCategoria[categoria]) {
            const qtd = itensPorCategoria[categoria][nome];
            const span = document.createElement("span");
            span.className = "item-icone";
            span.title = nome;
            span.innerHTML = `<img src="${iconeParaItem(nome)}" alt="${nome}"> ${qtd}x`;
            linha.appendChild(span);
        }
        grupo.appendChild(linha);
        listaEl.appendChild(grupo);
    }

    verificarStatusDoLike(praca.praca_id, praca.likes || 0);

    // Sempre volta pra aba "itens" ao abrir uma praça
    trocarAba("itens");

    carregarRemixes(praca.praca_id);
    carregarHistorico(praca.praca_id, praca.id);
}

// ==========================================
// 5b. ABAS (itens / remixes / histórico)
// ==========================================
document.querySelectorAll(".tab-btn").forEach(btn => {
    btn.addEventListener("click", () => trocarAba(btn.dataset.tab));
});

function trocarAba(nomeAba) {
    document.querySelectorAll(".tab-btn").forEach(btn => btn.classList.toggle("ativo", btn.dataset.tab === nomeAba));
    document.querySelectorAll(".tab-painel").forEach(painel => painel.classList.remove("ativo"));

    const painel = document.getElementById("painel" + nomeAba.charAt(0).toUpperCase() + nomeAba.slice(1));
    if (painel) painel.classList.add("ativo");

    document.querySelector(".tabs-conteudo").className = "tabs-conteudo tabs-conteudo--" + nomeAba;
}

// ==========================================
// 5c. ABA "REMIXES" — criações filhas desta praça
// ==========================================
async function carregarRemixes(pracaId) {
    const container = document.getElementById("listaRemixes");
    container.innerHTML = "<p class='texto-dica'>A carregar...</p>";

    const { data, error } = await db
        .from('city_creations')
        .select('praca_id, image_topo_url, created_at')
        .eq('praca_pai_id', pracaId)
        .order('created_at', { ascending: false });

    if (error) {
        container.innerHTML = "<p class='texto-dica'>Não foi possível carregar os remixes.</p>";
        return;
    }

    if (!data || data.length === 0) {
        container.innerHTML = `
            <div class="remix-vazio">
                Ninguém reimaginou esta praça ainda.<br/>
                <a href="https://feliperpv.com/repraca/galeria/abrir-app/?id=${pracaId}">Seja o primeiro a remixar →</a>
            </div>
        `;
        return;
    }

    container.innerHTML = "";
    data.forEach(filho => {
        const linha = document.createElement("div");
        linha.className = "remix-card";
        linha.innerHTML = `
            <span>${new Date(filho.created_at).toLocaleDateString('pt-PT')}</span>
            <img src="${filho.image_topo_url}" alt="" style="width:40px;height:40px;border-radius:8px;object-fit:cover;">
        `;
        linha.addEventListener("click", () => { window.location.hash = filho.praca_id; });
        container.appendChild(linha);
    });
}

// ==========================================
// 5d. ABA "HISTÓRICO" — versões anteriores do mesmo praca_id
// ==========================================
// NOTA: aqui eu listo as versões antigas só como informação (data +
// número da linha) — não fiz elas abrirem, porque a navegação por hash
// hoje é pelo praca_id (que é igual em todas as versões), então clicar
// não teria como carregar especificamente UMA versão antiga sem mudar
// esse esquema. Se quiser isso navegável, dá pra fazer, mas é uma
// mudança um pouco maior na forma como a URL identifica a praça.
async function carregarHistorico(pracaId, idAtual) {
    const abaBtn = document.getElementById("btnTabHistorico");
    const container = document.getElementById("listaHistorico");

    const { data, error } = await db
        .from('city_creations')
        .select('id, created_at')
        .eq('praca_id', pracaId)
        .order('created_at', { ascending: false });

    if (error || !data || data.length <= 1) {
        // Sem edições anteriores — some com a aba (mas "remixes" continua ali)
        abaBtn.style.display = "none";
        if (abaBtn.classList.contains("ativo")) trocarAba("itens");
        return;
    }

    abaBtn.style.display = "";
    container.innerHTML = "";
    data.forEach(versao => {
        const linha = document.createElement("div");
        linha.className = "historico-linha";
        linha.innerHTML = `
            <span>${new Date(versao.created_at).toLocaleDateString('pt-PT')}</span>
            <span>${versao.id === idAtual ? "atual" : "rePraça " + versao.id}</span>
        `;
        container.appendChild(linha);
    });
}

// ==========================================
// 6. SISTEMA DE LIKES
// ==========================================
const btnLike = document.getElementById("btnLikeModal");

function verificarStatusDoLike(id, totalLikes) {
    document.getElementById("modalLikesCount").innerText = totalLikes;
    
    if (localStorage.getItem("liked_" + id)) {
        btnLike.classList.add("curtido");
        btnLike.disabled = true;
    } else {
        btnLike.classList.remove("curtido");
        btnLike.disabled = false;
        btnLike.onclick = () => enviarLikeParaSupabase(id);
    }
}

async function enviarLikeParaSupabase(id) {
    btnLike.disabled = true; 

    // Usamos "db" em vez de "supabase"
    const { error } = await db.rpc('dar_like', { id_praca: id });

    if (!error) {
        let currentLikes = parseInt(document.getElementById("modalLikesCount").innerText);
        document.getElementById("modalLikesCount").innerText = currentLikes + 1;
        btnLike.classList.add("curtido");
        localStorage.setItem("liked_" + id, "true");
    } else {
        btnLike.disabled = false;
        alert("Erro ao registar o gosto!");
    }
}

lidarComNavegacao();