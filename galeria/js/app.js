// ==========================================
// 1. CONFIGURAÇÃO SUPABASE
// ==========================================

const supabaseUrl = 'https://ldynpvhqbmrcrlcabnuf.supabase.co';
const supabaseKey = 'sb_publishable_qtshAGmadXj9SbNhrgJOXg_lFROY3Yb';

const db = window.supabase.createClient(supabaseUrl, supabaseKey);

// ==========================================
// 1b. CATÁLOGO DE ITENS
// ==========================================
// Os dados dos itens ficam em dados/itens.json, ex:
// { "Banco de madeira": { "categoria": "Mobiliário", "icone": "banco-madeira.png" } }

let catalogoItens = {};

// Carrega o catálogo UMA VEZ quando o site inicia.
async function carregarCatalogoItens() {
    try {
        const resposta = await fetch("dados/itens.json");
        if (!resposta.ok) throw new Error("Não foi possível carregar dados/itens.json");
        catalogoItens = await resposta.json();
    } catch (erro) {
        console.error("Erro ao carregar catálogo de itens:", erro);
    }
}

function iconeParaItem(nome) {
    return "icones/" + (catalogoItens[nome]?.icone || "generico.png");
}

function categoriaDoItem(nome) {
    return catalogoItens[nome]?.categoria || "Sem categoria";
}

// Preenche o <select id="filtroEspecifico"> a partir do catálogo carregado.
function preencherFiltroDeItens() {
    const select = document.getElementById("filtroEspecifico");

    // Mantém a opção "todos" e limpa as demais caso seja chamada de novo.
    select.querySelectorAll("option:not([value='todos'])").forEach(opt => opt.remove());

    Object.keys(catalogoItens).sort().forEach(nome => {
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

// ==========================================
// 1c. DADOS DAS PRAÇAS ORIGINAIS
// ==========================================
// Cada praça-base tem seu próprio JSON em dados/pracas/<slug>.json, ex:
// { "nome": "Estacionamento", "itens": { "Vaga de Carro": 4, "Árvore": 1 } }
// O nome do arquivo é obtido automaticamente a partir do nome da praça.

function slugificar(texto) {
    return texto
        .normalize("NFD")
        .replace(/[\u0300-\u036f]/g, "")
        .toLowerCase()
        .replace(/[^a-z0-9]+/g, "-")
        .replace(/^-|-$/g, "");
}

// Cache em memória: várias criações costumam partilhar a mesma praça-base,
// então evitamos rebuscar o mesmo JSON a cada popup aberto.
const cachePracasOriginais = {};

async function carregarDadosDaPraca(nomeBase) {
    const arquivo = slugificar(nomeBase) + ".json";

    if (cachePracasOriginais[arquivo]) {
        return cachePracasOriginais[arquivo];
    }

    try {
        const resposta = await fetch("dados/pracas/" + arquivo);

        if (!resposta.ok) {
            console.warn("Não existe JSON para a praça:", nomeBase);
            const vazio = { nome: nomeBase, itens: {} };
            cachePracasOriginais[arquivo] = vazio;
            return vazio;
        }

        const dados = await resposta.json();
        cachePracasOriginais[arquivo] = dados;
        return dados;
    } catch (erro) {
        console.error("Erro ao carregar dados da praça:", erro);
        return { nome: nomeBase, itens: {} };
    }
}

// ==========================================
// 1d. GRÁFICO RADAR
// ==========================================
// Compara a DISTRIBUIÇÃO percentual das categorias (não a contagem bruta),
// então uma praça com 5 itens e uma com 200 ficam em escalas comparáveis
// (0% a 100%). Praça original em terracota, praça imaginada em verde.
// As categorias vêm do itens.json.

let graficoRadar = null;

// Cores lidas do CSS uma única vez (o valor não muda em runtime).
const coresGrafico = (() => {
    const estilo = getComputedStyle(document.documentElement);
    return {
        verde: estilo.getPropertyValue("--verde").trim(),
        terracota: estilo.getPropertyValue("--terracota").trim(),
        bege: estilo.getPropertyValue("--bege").trim(),
    };
})();

function totalDeItens(dados) {
    return Object.values(dados).reduce((total, itens) => {
        return total + Object.values(itens).reduce((soma, qtd) => soma + qtd, 0);
    }, 0);
}

// Converte quantidade por categoria em porcentagem do total.
// Ex: { Infraestrutura: 4, Natureza: 1 } com total 5 → { Infraestrutura: 80, Natureza: 20 }
function percentualPorCategoria(dados) {
    const total = totalDeItens(dados);
    if (total === 0) return {};

    const resultado = {};
    for (const categoria in dados) {
        const quantidade = Object.values(dados[categoria]).reduce((soma, v) => soma + v, 0);
        resultado[categoria] = (quantidade / total) * 100;
    }
    return resultado;
}

// Transforma { "Banco de madeira": 2, "Palmeira": 3 } (formato simples do
// JSON da praça original) em { "Mobiliário": { "Banco de madeira": 2 }, ... }
// usando a categoria cadastrada em itens.json.
function organizarItensOriginais(itens) {
    const resultado = {};
    for (const [nome, quantidade] of Object.entries(itens)) {
        const categoria = categoriaDoItem(nome);
        if (!resultado[categoria]) resultado[categoria] = {};
        resultado[categoria][nome] = quantidade;
    }
    return resultado;
}

// Conta os itens da praça imaginada (layout_data) por categoria.
// itens.json é a fonte oficial da categoria; se um item ainda não estiver
// cadastrado lá, cai para a categoria salva no próprio layout_data.
function contarItensImaginados(itens) {
    const resultado = {};

    itens.forEach(item => {
        const nome = item.nome || "Item sem nome";
        const categoria = catalogoItens[nome]?.categoria || item.categoria || "Sem categoria";

        if (!resultado[categoria]) resultado[categoria] = {};
        resultado[categoria][nome] = (resultado[categoria][nome] || 0) + 1;
    });

    return resultado;
}

function desenharRadar(dadosOriginais, dadosImaginados) {
    const canvas = document.getElementById("radarPraca");
    if (!canvas) {
        console.warn("Canvas #radarPraca não encontrado.");
        return;
    }
    if (typeof Chart === "undefined") {
        console.error("Chart.js não foi carregado.");
        return;
    }

    if (graficoRadar) {
        graficoRadar.destroy();
        graficoRadar = null;
    }

    const categorias = [...new Set([
        ...Object.keys(dadosOriginais),
        ...Object.keys(dadosImaginados),
    ])];

    const percentuaisOriginais = percentualPorCategoria(dadosOriginais);
    const percentuaisImaginados = percentualPorCategoria(dadosImaginados);

    // Integra visualmente o bloco do gráfico ao painel azul-marinho
    const radarContainer = canvas.closest(".radar-container");
    if (radarContainer) {
        Object.assign(radarContainer.style, {
            background: "rgba(255, 255, 255, 0.035)",
            border: "1px solid rgba(249, 239, 231, 0.08)",
            borderRadius: "16px",
            padding: "8px",
            boxSizing: "border-box",
        });
    }

    const { verde: corVerde, terracota: corTerracota, bege: corBege } = coresGrafico;
    const fonteBase = { family: "Cabin" };

    graficoRadar = new Chart(canvas, {
        type: "radar",
        data: {
            labels: categorias,
            datasets: [
                {
                    label: "Praça original",
                    data: categorias.map(c => percentuaisOriginais[c] || 0),
                    borderColor: corTerracota,
                    backgroundColor: "rgba(183, 111, 81, 0.14)",
                    borderWidth: 2,
                    pointBackgroundColor: corTerracota,
                    pointBorderColor: corBege,
                    pointBorderWidth: 2,
                    pointRadius: 4,
                    pointHoverRadius: 6,
                },
                {
                    label: "Sua praça",
                    data: categorias.map(c => percentuaisImaginados[c] || 0),
                    borderColor: corVerde,
                    backgroundColor: "rgba(152, 171, 86, 0.18)",
                    borderWidth: 2,
                    pointBackgroundColor: corVerde,
                    pointBorderColor: corBege,
                    pointBorderWidth: 2,
                    pointRadius: 4,
                    pointHoverRadius: 6,
                },
            ],
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            layout: { padding: { top: 4, right: 24, bottom: 12, left: 24 } },
            scales: {
                r: {
                    min: 0,
                    max: 100,
                    beginAtZero: true,
                    // Só mostra 25/50/75/100% — o 0% fica escondido pra não
                    // competir visualmente com o centro do gráfico.
                    ticks: {
                        stepSize: 25,
                        color: "rgba(249, 239, 231, 0.72)",
                        font: { ...fonteBase, size: 10, weight: "500" },
                        backdropColor: "transparent",
                        showLabelBackdrop: false,
                        padding: 2,
                        callback: valor => (valor === 0 ? "" : valor + "%"),
                    },
                    grid: { color: "rgba(249, 239, 231, 0.14)", lineWidth: 1 },
                    angleLines: { color: "rgba(249, 239, 231, 0.11)", lineWidth: 1 },
                    pointLabels: {
                        color: corBege,
                        padding: 12,
                        font: { ...fonteBase, size: 12, weight: "600" },
                    },
                },
            },
            plugins: {
                legend: {
                    display: true,
                    position: "top",
                    labels: {
                        color: corBege,
                        padding: 16,
                        usePointStyle: true,
                        pointStyle: "rectRounded",
                        boxWidth: 24,
                        boxHeight: 9,
                        font: { ...fonteBase, size: 12, weight: "600" },
                    },
                },
                tooltip: {
                    backgroundColor: "rgba(19, 28, 59, 0.96)",
                    titleColor: corBege,
                    bodyColor: corBege,
                    borderColor: "rgba(249, 239, 231, 0.25)",
                    borderWidth: 1,
                    padding: 10,
                    titleFont: { ...fonteBase, weight: "600" },
                    bodyFont: { ...fonteBase, size: 12 },
                    callbacks: {
                        label: context => `${context.dataset.label}: ${Number(context.raw || 0).toFixed(0)}%`,
                    },
                },
            },
        },
    });
}

// Lista comparativa por item, ex:
// "Banco de Madeira — Na sua praça: 5, Original: 2, +3"
function criarListaComparativa(dadosImaginados, dadosOriginais) {
    const lista = document.getElementById("modalListaItens");
    if (!lista) return;

    lista.innerHTML = "";

    const categorias = [...new Set([
        ...Object.keys(dadosImaginados),
        ...Object.keys(dadosOriginais),
    ])];

    categorias.forEach(categoria => {
        const grupo = document.createElement("div");
        grupo.className = "grupo-categoria";

        const titulo = document.createElement("div");
        titulo.className = "grupo-categoria-titulo";
        titulo.innerText = categoria;
        grupo.appendChild(titulo);

        const itensContainer = document.createElement("div");
        itensContainer.className = "lista-comparativa";

        const itensImaginados = dadosImaginados[categoria] || {};
        const itensOriginais = dadosOriginais[categoria] || {};
        const nomesItens = [...new Set([
            ...Object.keys(itensImaginados),
            ...Object.keys(itensOriginais),
        ])];

        nomesItens.forEach(nome => {
            const quantidadeAtual = itensImaginados[nome] || 0;
            const quantidadeOriginal = itensOriginais[nome] || 0;
            const diferenca = quantidadeAtual - quantidadeOriginal;

            let classe = "igual";
            let sinal = "";
            if (diferenca > 0) { classe = "aumentou"; sinal = "+"; }
            if (diferenca < 0) { classe = "diminuiu"; }

            const linha = document.createElement("div");
            linha.className = "item-comparativo";
            linha.innerHTML = `
                <div class="item-comparativo-nome">
                    <img src="${iconeParaItem(nome)}" alt="">
                    <span>${nome}</span>
                </div>
                <div class="item-comparativo-dados">
                    <span><small>Na sua praça</small><strong>${quantidadeAtual}</strong></span>
                    <span><small>Original</small><strong>${quantidadeOriginal}</strong></span>
                    <span class="badge-diferenca ${classe}">${sinal}${diferenca}</span>
                </div>
            `;
            itensContainer.appendChild(linha);
        });

        grupo.appendChild(itensContainer);
        lista.appendChild(grupo);
    });
}

// ==========================================
// 2. BUSCA DE DADOS E SCROLL INFINITO
// ==========================================

let paginaAtual = 0;
const itensPorPagina = 12;
let carregando = false;
let chegouAoFim = false;

const gallery = document.getElementById("gallery");
const popup = document.getElementById("popup");
const loader = document.getElementById("fim-da-pagina");

async function carregarPracas() {
    if (carregando || chegouAoFim) return;
    carregando = true;

    const ordem = document.getElementById("filtroOrdem").value;
    const minItens = parseInt(document.getElementById("filtroItens").value);
    const itemEspecifico = document.getElementById("filtroEspecifico").value;

    const inicio = paginaAtual * itensPorPagina;
    const fim = inicio + itensPorPagina - 1;

    // Quando filtra por item específico, chamamos uma função SQL (RPC) em
    // vez de montar o filtro com .ilike() direto no client — o Postgres
    // exige um cast (layout_data::text) pra comparar jsonb com ilike, e
    // esse cast não é repassado corretamente pelos filtros do PostgREST
    // client-side. A função SQL já faz esse cast por dentro (veja
    // filtrar_pracas_por_item.sql).
    const colunas = 'praca_id, image_topo_url, created_at, likes, total_objects';
    let query = itemEspecifico !== "todos"
        ? db.rpc('filtrar_pracas_por_item', { item_nome: itemEspecifico }).select(colunas)
        : db.from('city_creations').select(colunas);

    query = query.gte('total_objects', minItens);

    if (ordem === "recentes") query = query.order('created_at', { ascending: false });
    if (ordem === "antigas") query = query.order('created_at', { ascending: true });
    if (ordem === "likes") query = query.order('likes', { ascending: false });

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

        card.addEventListener("click", () => { window.location.hash = praca.praca_id; });
        gallery.appendChild(card);
    });
}

// Dispara carregarPracas() quando o loader entra na tela (scroll infinito)
const observer = new IntersectionObserver(entradas => {
    if (entradas[0].isIntersecting) carregarPracas();
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

    // NOTA: .eq('praca_id', hash).single() quebrava assim que o mesmo
    // praca_id passasse a ter mais de uma linha (cada edição salva de
    // novo). Buscamos sempre a versão mais recente.
    const { data, error } = await db
        .from('city_creations')
        .select('*')
        .eq('praca_id', hash)
        .order('created_at', { ascending: false })
        .limit(1)
        .single();

    if (error) {
        console.error("Erro ao carregar praça:", error);
        alert("Erro ao carregar a praça.");
        window.location.hash = "";
        return;
    }

    if (data) {
        await preencherModal(data);
    } else {
        alert("Praça não encontrada!");
        window.location.hash = "";
    }
}

document.getElementById("btnFecharModal").addEventListener("click", () => window.location.hash = "");
popup.addEventListener("click", e => {
    if (e.target === popup) window.location.hash = "";
});

// ==========================================
// 5. PREENCHER O MODAL
// ==========================================

let pracaAbertaId = null;

async function preencherModal(praca) {
    pracaAbertaId = praca.praca_id;

    // --- Imagens: abre sempre na vista de topo ---
    const imgEl = document.getElementById("selectedImage");
    const btnTopo = document.getElementById("btnVistaTopo");
    const btnAngulo = document.getElementById("btnVistaAngulo");

    imgEl.src = praca.image_topo_url;
    btnTopo.classList.add("ativo");
    btnAngulo.classList.remove("ativo");

    btnTopo.onclick = () => {
        imgEl.src = praca.image_topo_url;
        btnTopo.classList.add("ativo");
        btnAngulo.classList.remove("ativo");
    };
    btnAngulo.onclick = () => {
        imgEl.src = praca.image_url;
        btnAngulo.classList.add("ativo");
        btnTopo.classList.remove("ativo");
    };

    document.getElementById("btnRemix").href =
        "https://feliperpv.com/repraca/galeria/abrir-app/?id=" + praca.praca_id;

    // --- Dados da praça imaginada ---
    const jsonConvertido = JSON.parse(praca.layout_data);
    const itens = jsonConvertido.layoutDaPraca || [];
    const nomeBase = praca.nome_da_cena || jsonConvertido.nomeDaCena || "";

    const itensPorCategoria = contarItensImaginados(itens);
    const dadosPracaOriginal = await carregarDadosDaPraca(nomeBase);
    const itensOriginais = organizarItensOriginais(dadosPracaOriginal.itens || {});

    desenharRadar(itensOriginais, itensPorCategoria);
    criarListaComparativa(itensPorCategoria, itensOriginais);

    // "rePraça {numero}" = id da LINHA na tabela (não o praca_id, que é o UUID)
    document.getElementById("modalNumero").innerText = "rePraça " + praca.id;

    // Título = o nome que o criador deu à própria criação; se não tiver
    // (linhas antigas sem esse campo), cai pro nome da praça-base.
    const titulo = (praca.titulo && praca.titulo.trim()) ? praca.titulo : (nomeBase || "Praça Personalizada");
    document.getElementById("modalTitulo").innerText = titulo;

    const baseadoEmEl = document.getElementById("modalBaseadoEm");
    if (nomeBase && nomeBase !== titulo) {
        baseadoEmEl.innerText = "baseado em: " + nomeBase;
        baseadoEmEl.style.display = "";
    } else {
        baseadoEmEl.style.display = "none";
    }

    document.getElementById("modalData").innerText =
        "Última edição: " + praca.created_at.substring(0, 10).split('-').reverse().join('/');
    document.getElementById("modalTotalItens").innerText = praca.total_objects;

    // Foco = categoria com mais itens na praça imaginada
    let categoriaPrincipal = "Mista";
    let max = 0;
    for (let cat in itensPorCategoria) {
        const totalNaCategoria = Object.values(itensPorCategoria[cat]).reduce((a, b) => a + b, 0);
        if (totalNaCategoria > max) {
            max = totalNaCategoria;
            categoriaPrincipal = cat;
        }
    }
    document.getElementById("modalCategoria").innerText = categoriaPrincipal;

    const comentarioEl = document.getElementById("modalComentario");
    if (praca.comentario && praca.comentario.trim()) {
        comentarioEl.innerText = praca.comentario;
        comentarioEl.style.display = "";
    } else {
        comentarioEl.style.display = "none";
    }

    verificarStatusDoLike(praca.praca_id, praca.likes || 0);

    trocarAba("itens"); // sempre volta pra aba "itens" ao abrir uma praça
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
    document.querySelectorAll(".tab-btn").forEach(btn => {
        btn.classList.toggle("ativo", btn.dataset.tab === nomeAba);
    });
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
// NOTA: listo as versões antigas só como informação (data + número da
// linha) — não abrem, porque a navegação por hash hoje é pelo praca_id
// (igual em todas as versões). Pra tornar isso navegável precisaria mudar
// como a URL identifica a praça.

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

    const { error } = await db.rpc('dar_like', { id_praca: id });

    if (!error) {
        const contador = document.getElementById("modalLikesCount");
        contador.innerText = parseInt(contador.innerText) + 1;
        btnLike.classList.add("curtido");
        localStorage.setItem("liked_" + id, "true");
    } else {
        btnLike.disabled = false;
        alert("Erro ao registar o gosto!");
    }
}

// ==========================================
// 7. INICIALIZAÇÃO
// ==========================================
// Primeiro carrega itens.json, depois monta o filtro, só então abre a
// galeria — assim itens.json é baixado UMA VEZ por carregamento da página.

async function iniciarGaleria() {
    await carregarCatalogoItens();
    preencherFiltroDeItens();
    lidarComNavegacao();
    carregarPracas();
}

iniciarGaleria();
