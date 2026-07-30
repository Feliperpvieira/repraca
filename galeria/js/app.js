// ==========================================
// 1. CONFIGURAÇÃO SUPABASE
// ==========================================
const supabaseUrl = 'https://ldynpvhqbmrcrlcabnuf.supabase.co';
const supabaseKey = 'sb_publishable_qtshAGmadXj9SbNhrgJOXg_lFROY3Yb';
const supabase = supabase.createClient(supabaseUrl, supabaseKey);

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

    // Inicia a query básica (Pedimos só o essencial para poupar internet na Grid)
    let query = supabase.from('city_creations').select('praca_id, image_url, created_at, likes, total_objects');

    // Aplica o filtro de Mínimo de Itens
    query = query.gte('total_objects', minItens);

    // Aplica o filtro de Item Específico (Procura dentro do JSONB no Supabase!)
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
        // Formata a data (de YYYY-MM-DD para um formato legível)
        const dataFormatada = new Date(praca.created_at).toLocaleDateString('pt-PT');

        // Cria o HTML do Card
        const card = document.createElement("div");
        card.className = "card-praca";
        card.innerHTML = `
            <img src="${praca.image_url}" class="card-img" loading="lazy" alt="Praça">
            <div class="card-info">
                <span class="card-data">📅 ${dataFormatada}</span>
                <span class="card-likes">❤️ ${praca.likes || 0}</span>
            </div>
        `;

        // Ao clicar no card, não abrimos o modal. Mudamos o URL! (Hash Routing)
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
    gallery.innerHTML = ""; // Limpa a tela
    paginaAtual = 0;
    chegouAoFim = false;
    loader.innerText = "A carregar mais praças...";
    carregarPracas(); // Busca do zero
}

document.getElementById("filtroOrdem").addEventListener("change", aplicarFiltros);
document.getElementById("filtroItens").addEventListener("change", aplicarFiltros);
document.getElementById("filtroEspecifico").addEventListener("change", aplicarFiltros);

// ==========================================
// 4. SISTEMA DE HASH ROUTING (Navegação via URL)
// ==========================================
window.addEventListener("hashchange", lidarComNavegacao);

async function lidarComNavegacao() {
    const hash = window.location.hash.replace("#", ""); // Pega no ID do URL

    if (!hash) {
        popup.classList.add("escondido"); // Se não há URL, esconde o modal
        document.body.style.overflow = "auto"; // Devolve o scroll da página
        return;
    }

    // Se há um URL, vai buscar A PRAÇA COMPLETA (incluindo o JSON pesado layout_data)
    document.getElementById("modalTitulo").innerText = "A carregar dados...";
    popup.classList.remove("escondido");
    document.body.style.overflow = "hidden"; // Trava o scroll da página ao fundo

    const { data, error } = await supabase
        .from('city_creations')
        .select('*')
        .eq('praca_id', hash)
        .single(); // Pega apenas uma linha

    if (data) {
        preencherModal(data);
    } else {
        alert("Praça não encontrada!");
        window.location.hash = ""; // Limpa o URL se falhar
    }
}

// Fechar modal no botão X ou ao clicar fora
document.getElementById("btnFecharModal").addEventListener("click", () => window.location.hash = "");
popup.addEventListener("click", (e) => {
    if (e.target === popup) window.location.hash = "";
});

// ==========================================
// 5. PREENCHER O MODAL E LER O JSON DA UNITY
// ==========================================
let pracaAbertaId = null;

function preencherModal(praca) {
    pracaAbertaId = praca.praca_id;
    document.getElementById("selectedImage").src = praca.image_url;
    
    // Atualizar o Deep Link do App
    document.getElementById("btnRemix").href = `repraca://editar?id=${praca.praca_id}`;
    
    // Matemática dos Itens lidos do JSON da Unity
    const jsonConvertido = JSON.parse(praca.layout_data);
    const itens = jsonConvertido.layoutDaPraca; // A lista que vem do seu C#
    
    let contagemCategorias = {};
    let resumoMobiliario = {};

    itens.forEach(item => {
        // Conta as categorias
        contagemCategorias[item.categoria] = (contagemCategorias[item.categoria] || 0) + 1;
        // Conta móveis específicos (Ex: 3x Banco de Madeira)
        resumoMobiliario[item.nome] = (resumoMobiliario[item.nome] || 0) + 1;
    });

    // Descobre a categoria principal (a mais usada)
    let categoriaPrincipal = "Mista";
    let max = 0;
    for (let cat in contagemCategorias) {
        if (contagemCategorias[cat] > max) {
            max = contagemCategorias[cat];
            categoriaPrincipal = cat;
        }
    }

    // Preenche as caixinhas estatísticas
    document.getElementById("modalTitulo").innerText = jsonConvertido.nomeDaCena || "Praça Personalizada";
    document.getElementById("modalData").innerText = "Criada a " + praca.created_at.substring(0,10);
    document.getElementById("modalTotalItens").innerText = praca.total_objects;
    document.getElementById("modalCategoria").innerText = categoriaPrincipal;

    // Escreve a listinha de móveis na sidebar
    let htmlMobiliario = "<strong>Itens utilizados:</strong><br/>";
    for (let movel in resumoMobiliario) {
        htmlMobiliario += `${resumoMobiliario[movel]}x ${movel}<br/>`;
    }
    document.getElementById("modalListaItens").innerHTML = htmlMobiliario;

    // Configura o Botão de Like
    verificarStatusDoLike(praca.praca_id, praca.likes || 0);
}

// ==========================================
// 6. SISTEMA DE LIKES (Com LocalStorage)
// ==========================================
const btnLike = document.getElementById("btnLikeModal");

function verificarStatusDoLike(id, totalLikes) {
    document.getElementById("modalLikesCount").innerText = totalLikes;
    
    // Verifica se este browser já deu like nesta praça hoje
    if (localStorage.getItem("liked_" + id)) {
        btnLike.classList.add("curtido");
        btnLike.disabled = true;
    } else {
        btnLike.classList.remove("curtido");
        btnLike.disabled = false;
        // Importante: limpa eventos antigos antes de adicionar novo para evitar cliques duplos
        btnLike.onclick = () => enviarLikeParaSupabase(id);
    }
}

async function enviarLikeParaSupabase(id) {
    btnLike.disabled = true; // Trava o botão imediatamente

    // Chama a função (RPC) que vamos criar no Supabase
    const { error } = await supabase.rpc('dar_like', { id_praca: id });

    if (!error) {
        // Soma visualmente e salva no LocalStorage
        let currentLikes = parseInt(document.getElementById("modalLikesCount").innerText);
        document.getElementById("modalLikesCount").innerText = currentLikes + 1;
        btnLike.classList.add("curtido");
        localStorage.setItem("liked_" + id, "true");
    } else {
        btnLike.disabled = false;
        alert("Erro ao registar o gosto!");
    }
}

// Arranca o site validando se alguém abriu um link direto para uma praça
lidarComNavegacao();
