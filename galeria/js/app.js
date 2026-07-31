// ==========================================
// 1. CONFIGURAÇÃO SUPABASE
// ==========================================
const supabaseUrl = 'https://ldynpvhqbmrcrlcabnuf.supabase.co';
const supabaseKey = 'sb_publishable_qtshAGmadXj9SbNhrgJOXg_lFROY3Yb';

// Usamos window.supabase e guardamos na variável "db"
const db = window.supabase.createClient(supabaseUrl, supabaseKey);

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

    // Usamos "db" em vez de "supabase"
    let query = db.from('city_creations').select('praca_id, image_topo_url, created_at, likes, total_objects');

    // Aplica o filtro de Mínimo de Itens
    query = query.gte('total_objects', minItens);

    // Aplica o filtro de Item Específico
    if (itemEspecifico !== "todos") {
        query = query.contains('layout_data', `[{"nome": "${itemEspecifico}"}]`);
    }

    // Aplica a Ordenação
    if (ordem === "recentes") query = query.order('created_at', { ascending: false });
    if (ordem === "antigas") query = query.order('created_at', { ascending: true });
    if (ordem === "likes") query = query.order('likes', { ascending: false });

    // Aplica a paginação
    const { data, error } = await query.range(inicio, fim);

    if (error) {
        console.error("Erro ao buscar:", error);
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

    // Usamos "db" em vez de "supabase"
    const { data, error } = await db
        .from('city_creations')
        .select('*')
        .eq('praca_id', hash)
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

function preencherModal(praca) {
    pracaAbertaId = praca.praca_id;
    document.getElementById("selectedImage").src = praca.image_topo_url;
    
    document.getElementById("btnRemix").href = "https://feliperpv.com/repraca/galeria/abrir-app/?id=" + praca.praca_id;
    
    const jsonConvertido = JSON.parse(praca.layout_data);
    const itens = jsonConvertido.layoutDaPraca; 
    
    let contagemCategorias = {};
    let resumoMobiliario = {};

    itens.forEach(item => {
        contagemCategorias[item.categoria] = (contagemCategorias[item.categoria] || 0) + 1;
        resumoMobiliario[item.nome] = (resumoMobiliario[item.nome] || 0) + 1;
    });

    let categoriaPrincipal = "Mista";
    let max = 0;
    for (let cat in contagemCategorias) {
        if (contagemCategorias[cat] > max) {
            max = contagemCategorias[cat];
            categoriaPrincipal = cat;
        }
    }

    document.getElementById("modalTitulo").innerText = jsonConvertido.nomeDaCena || "Praça Personalizada";
    document.getElementById("modalData").innerText = "Criada a " + praca.created_at.substring(0,10);
    document.getElementById("modalTotalItens").innerText = praca.total_objects;
    document.getElementById("modalCategoria").innerText = categoriaPrincipal;

    let htmlMobiliario = "<strong>Itens utilizados:</strong><br/>";
    for (let movel in resumoMobiliario) {
        htmlMobiliario += `${resumoMobiliario[movel]}x ${movel}<br/>`;
    }
    document.getElementById("modalListaItens").innerHTML = htmlMobiliario;

    verificarStatusDoLike(praca.praca_id, praca.likes || 0);
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
