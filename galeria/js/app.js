// ==========================================
// 1. CONFIGURAÇÃO SUPABASE
// ==========================================

const supabaseUrl = 'https://ldynpvhqbmrcrlcabnuf.supabase.co';
const supabaseKey = 'sb_publishable_qtshAGmadXj9SbNhrgJOXg_lFROY3Yb';


// Cores do CSS
const estilo =
    getComputedStyle(document.documentElement);

const verde =
    estilo.getPropertyValue("--verde").trim();

const terracota =
    estilo.getPropertyValue("--terracota").trim();

const bege =
    estilo.getPropertyValue("--bege").trim();


// Usamos window.supabase e guardamos na variável "db"
const db = window.supabase.createClient(supabaseUrl, supabaseKey);


// ==========================================
// 1b. CATÁLOGO DE ITENS
// ==========================================
//
// NOVO:
// Os dados dos itens agora ficam em:
//
// dados/itens.json
//
// Exemplo:
//
// {
//     "Banco de madeira": {
//         "categoria": "Mobiliário",
//         "icone": "banco-madeira.png"
//     }
// }
//
// Isso substitui o antigo dicionário "iconePorNome"
// que ficava diretamente neste arquivo.
//

let catalogoItens = {};


// NOVO:
// Carrega o catálogo de itens UMA VEZ quando o site inicia.
async function carregarCatalogoItens() {
    try {
        const resposta = await fetch("dados/itens.json");

        if (!resposta.ok) {
            throw new Error("Não foi possível carregar dados/itens.json");
        }

        catalogoItens = await resposta.json();

        console.log("Catálogo de itens carregado.");

    } catch (erro) {
        console.error("Erro ao carregar catálogo de itens:", erro);
    }
}


// NOVO:
// Retorna o caminho do ícone de um item.
function iconeParaItem(nome) {
    return "icones/" +
        (catalogoItens[nome]?.icone || "generico.png");
}


// NOVO:
// Retorna a categoria cadastrada para um item.
function categoriaDoItem(nome) {
    return catalogoItens[nome]?.categoria || "Sem categoria";
}


// Preenche o <select id="filtroEspecifico> a partir do
// catálogo de itens carregado do JSON.
function preencherFiltroDeItens() {

    const select =
        document.getElementById("filtroEspecifico");

    // Mantém a opção "todos"
    // e limpa as demais caso a função seja chamada novamente.
    select.querySelectorAll("option:not([value='todos'])")
        .forEach(opt => opt.remove());


    Object.keys(catalogoItens)
        .sort()
        .forEach(nome => {

            const opt =
                document.createElement("option");

            opt.value = nome;

            const img =
                document.createElement("img");

            img.src =
                iconeParaItem(nome);

            img.alt = "";

            img.className =
                "opcao-icone";

            opt.appendChild(img);

            opt.appendChild(
                document.createTextNode(nome)
            );

            select.appendChild(opt);
        });
}


// ==========================================
// NOVO:
// 1c. DADOS DAS PRAÇAS ORIGINAIS
// ==========================================
//
// Cada praça possui seu próprio JSON:
//
// dados/pracas/estacionamento.json
//
// Exemplo:
//
// {
//     "nome": "Estacionamento",
//     "itens": {
//         "Vaga de Carro": 4,
//         "Árvore": 1
//     }
// }
//
// O nome do arquivo é obtido automaticamente
// a partir do nome da praça.
//


// Transforma:
// "Centro de Saúde de Alcântara"
// em:
// "centro-de-saude-de-alcantara"
function slugificar(texto) {

    return texto
        .normalize("NFD")
        .replace(/[\u0300-\u036f]/g, "")
        .toLowerCase()
        .replace(/[^a-z0-9]+/g, "-")
        .replace(/^-|-$/g, "");
}


// Carrega o JSON correspondente à praça original.
async function carregarDadosDaPraca(nomeBase) {

    const arquivo =
        slugificar(nomeBase) + ".json";

    try {

        const resposta =
            await fetch(
                "dados/pracas/" + arquivo
            );

        if (!resposta.ok) {

            console.warn(
                "Não existe JSON para a praça:",
                nomeBase
            );

            return {
                nome: nomeBase,
                itens: {}
            };
        }

        return await resposta.json();

    } catch (erro) {

        console.error(
            "Erro ao carregar dados da praça:",
            erro
        );

        return {
            nome: nomeBase,
            itens: {}
        };
    }
}


// ==========================================
// 1d. GRÁFICO RADAR
// ==========================================
//
// Toda a lógica relacionada ao gráfico fica
// concentrada neste bloco.
//
// O gráfico compara a DISTRIBUIÇÃO percentual
// das categorias.
//
// Portanto:
// - uma praça com 5 itens
// - uma praça com 200 itens
//
// podem ter polígonos de tamanhos comparáveis,
// porque ambas usam a mesma escala de 0% a 100%.
//
// Praça original:
//     terracota
//
// Sua praça:
//     verde
//
// As categorias vêm do itens.json.
// ==========================================

let graficoRadar = null;


// ------------------------------------------
// Conta quantos itens existem no total.
// ------------------------------------------

function totalDeItens(dados) {

    return Object.values(dados).reduce(
        (total, itens) => {

            return total +
                Object.values(itens).reduce(
                    (soma, quantidade) =>
                        soma + quantidade,
                    0
                );

        },
        0
    );
}


// ------------------------------------------
// Converte quantidade por categoria
// em porcentagem.
//
// Exemplo:
//
// Infraestrutura: 4
// Natureza: 1
//
// Total = 5
//
// Resultado:
//
// Infraestrutura: 80
// Natureza: 20
// ------------------------------------------

function percentualPorCategoria(dados) {

    const total =
        totalDeItens(dados);

    if (total === 0) {
        return {};
    }

    const resultado = {};

    for (const categoria in dados) {

        const quantidade =
            Object.values(dados[categoria])
                .reduce(
                    (soma, valor) =>
                        soma + valor,
                    0
                );

        resultado[categoria] =
            (quantidade / total) * 100;
    }

    return resultado;
}


// ==========================================
// ORGANIZA ITENS DA PRAÇA ORIGINAL
// ==========================================
//
// O JSON da praça original é simples:
//
// {
//     "Banco de madeira": 2,
//     "Palmeira": 3
// }
//
// Aqui transformamos em:
//
// {
//     "Mobiliário": {
//         "Banco de madeira": 2
//     },
//     "Natureza": {
//         "Palmeira": 3
//     }
// }
//
// A categoria vem do itens.json.
// ==========================================

function organizarItensOriginais(itens) {

    const resultado = {};

    for (
        const [nome, quantidade]
        of Object.entries(itens)
    ) {

        const categoria =
            categoriaDoItem(nome);

        if (!resultado[categoria]) {
            resultado[categoria] = {};
        }

        resultado[categoria][nome] =
            quantidade;
    }

    return resultado;
}


// ==========================================
// CONTA OS ITENS DA PRAÇA IMAGINADA
// ==========================================
//
// IMPORTANTE:
//
// O layout_data possui uma categoria antiga
// em cada item, mas agora usamos o itens.json
// como fonte oficial da categoria.
//
// Assim:
//
// "Cereja-do-mato"
//     ↓
// itens.json
//     ↓
// "Natureza"
//
// Isso garante que a praça original e a praça
// imaginada usem as MESMAS categorias no radar.
//
// Se um item ainda não existir no itens.json,
// usamos a categoria salva no layout_data.
// ==========================================

function contarItensImaginados(itens) {

    const resultado = {};

    itens.forEach(item => {

        const nome =
            item.nome ||
            "Item sem nome";


        const categoria =
            catalogoItens[nome]?.categoria ||
            item.categoria ||
            "Sem categoria";


        if (!resultado[categoria]) {
            resultado[categoria] = {};
        }


        resultado[categoria][nome] =
            (
                resultado[categoria][nome] ||
                0
            ) + 1;
    });

    return resultado;
}


// ==========================================
// DESENHA O RADAR
// ==========================================
//
// Toda a configuração visual do Chart.js
// fica aqui.
//
// Escala:
// 0% → 100%
//
// Marcações:
// 25%
// 50%
// 75%
// 100%
//
// 0% não aparece como texto para evitar
// poluição visual no centro do gráfico.
// ==========================================

function desenharRadar(
    dadosOriginais,
    dadosImaginados
) {

    const canvas =
        document.getElementById(
            "radarPraca"
        );


    if (!canvas) {

        console.warn(
            "Canvas #radarPraca não encontrado."
        );

        return;
    }


    if (
        typeof Chart ===
        "undefined"
    ) {

        console.error(
            "Chart.js não foi carregado."
        );

        return;
    }


    // ------------------------------------------
    // Destrói o gráfico anterior
    // ------------------------------------------

    if (graficoRadar) {

        graficoRadar.destroy();

        graficoRadar =
            null;
    }


    // ------------------------------------------
    // Cores
    //
    // São buscadas novamente aqui para garantir
    // que o gráfico sempre acompanhe o CSS.
    // ------------------------------------------

    const estiloRadar =
        getComputedStyle(
            document.documentElement
        );


    const corVerde =
        estiloRadar
            .getPropertyValue("--verde")
            .trim();


    const corTerracota =
        estiloRadar
            .getPropertyValue("--terracota")
            .trim();


    const corBege =
        estiloRadar
            .getPropertyValue("--bege")
            .trim();


    // ------------------------------------------
    // Categorias
    // ------------------------------------------

    const categorias = [
        ...new Set([
            ...Object.keys(
                dadosOriginais
            ),

            ...Object.keys(
                dadosImaginados
            )
        ])
    ];


    // ------------------------------------------
    // Porcentagens
    // ------------------------------------------

    const percentuaisOriginais =
        percentualPorCategoria(
            dadosOriginais
        );


    const percentuaisImaginados =
        percentualPorCategoria(
            dadosImaginados
        );


    // ------------------------------------------
    // Container visual do radar
    //
    // Não depende de CSS novo.
    // Apenas deixa o bloco do gráfico mais
    // integrado ao painel azul-marinho.
    // ------------------------------------------

    const radarContainer =
        canvas.closest(
            ".radar-container"
        );


    if (radarContainer) {

        radarContainer.style.background =
            "rgba(255, 255, 255, 0.035)";

        radarContainer.style.border =
            "1px solid rgba(249, 239, 231, 0.08)";

        radarContainer.style.borderRadius =
            "16px";

        radarContainer.style.padding =
            "8px";

        radarContainer.style.boxSizing =
            "border-box";
    }


    // ------------------------------------------
    // CHART.JS
    // ------------------------------------------

    graficoRadar =
        new Chart(
            canvas,
            {

                type: "radar",


                data: {

                    labels:
                        categorias,


                    datasets: [

                        // ==================================
                        // PRAÇA ORIGINAL
                        // ==================================

                        {
                            label:
                                "Praça original",


                            data:
                                categorias.map(
                                    categoria =>
                                        percentuaisOriginais[
                                            categoria
                                        ] || 0
                                ),


                            borderColor:
                                corTerracota,


                            backgroundColor:
                                "rgba(183, 111, 81, 0.14)",


                            borderWidth:
                                2,


                            pointBackgroundColor:
                                corTerracota,


                            pointBorderColor:
                                corBege,


                            pointBorderWidth:
                                2,


                            pointRadius:
                                4,


                            pointHoverRadius:
                                6
                        },


                        // ==================================
                        // SUA PRAÇA
                        // ==================================

                        {
                            label:
                                "Sua praça",


                            data:
                                categorias.map(
                                    categoria =>
                                        percentuaisImaginados[
                                            categoria
                                        ] || 0
                                ),


                            borderColor:
                                corVerde,


                            backgroundColor:
                                "rgba(152, 171, 86, 0.18)",


                            borderWidth:
                                2,


                            pointBackgroundColor:
                                corVerde,


                            pointBorderColor:
                                corBege,


                            pointBorderWidth:
                                2,


                            pointRadius:
                                4,


                            pointHoverRadius:
                                6
                        }
                    ]
                },


                // ==========================================
                // CONFIGURAÇÃO VISUAL
                // ==========================================

                options: {

                    responsive:
                        true,


                    maintainAspectRatio:
                        false,


                    layout: {

                        padding: {

                            top:
                                4,

                            right:
                                24,

                            bottom:
                                12,

                            left:
                                24
                        }
                    },


                    // ======================================
                    // ESCALA RADIAL
                    // ======================================

                    scales: {

                        r: {

                            min:
                                0,


                            max:
                                100,


                            beginAtZero:
                                true,


                            // ----------------------------------
                            // NÚMEROS
                            // ----------------------------------
                            //
                            // Só mostramos:
                            //
                            // 25%
                            // 50%
                            // 75%
                            // 100%
                            //
                            // O 0% é escondido para não ficar
                            // competindo com o centro.
                            //

                            ticks: {

                                stepSize:
                                    25,


                                color:
                                    "rgba(249, 239, 231, 0.72)",


                                font: {

                                    family:
                                        "Cabin",

                                    size:
                                        10,

                                    weight:
                                        "500"
                                },


                                backdropColor:
                                    "transparent",


                                showLabelBackdrop:
                                    false,


                                padding:
                                    2,


                                callback:
                                    valor => {

                                        if (
                                            valor ===
                                            0
                                        ) {

                                            return "";
                                        }


                                        return (
                                            valor +
                                            "%"
                                        );
                                    }
                            },


                            // ----------------------------------
                            // GRADE
                            // ----------------------------------

                            grid: {

                                color:
                                    "rgba(249, 239, 231, 0.14)",

                                lineWidth:
                                    1
                            },


                            // ----------------------------------
                            // LINHAS DOS EIXOS
                            // ----------------------------------

                            angleLines: {

                                color:
                                    "rgba(249, 239, 231, 0.11)",

                                lineWidth:
                                    1
                            },


                            // ----------------------------------
                            // NOMES DAS CATEGORIAS
                            // ----------------------------------

                            pointLabels: {

                                color:
                                    corBege,


                                padding:
                                    12,


                                font: {

                                    family:
                                        "Cabin",

                                    size:
                                        12,

                                    weight:
                                        "600"
                                }
                            }
                        }
                    },


                    // ==========================================
                    // PLUGINS
                    // ==========================================

                    plugins: {


                        // --------------------------------------
                        // LEGENDA
                        // --------------------------------------

                        legend: {

                            display:
                                true,


                            position:
                                "top",


                            labels: {

                                color:
                                    corBege,


                                padding:
                                    16,


                                usePointStyle:
                                    true,


                                pointStyle:
                                    "rectRounded",


                                boxWidth:
                                    24,


                                boxHeight:
                                    9,


                                font: {

                                    family:
                                        "Cabin",

                                    size:
                                        12,

                                    weight:
                                        "600"
                                }
                            }
                        },


                        // --------------------------------------
                        // TOOLTIP
                        // --------------------------------------

                        tooltip: {

                            backgroundColor:
                                "rgba(19, 28, 59, 0.96)",


                            titleColor:
                                corBege,


                            bodyColor:
                                corBege,


                            borderColor:
                                "rgba(249, 239, 231, 0.25)",


                            borderWidth:
                                1,


                            padding:
                                10,


                            titleFont: {

                                family:
                                    "Cabin",

                                weight:
                                    "600"
                            },


                            bodyFont: {

                                family:
                                    "Cabin",

                                size:
                                    12
                            },


                            callbacks: {

                                label:
                                    context => {

                                        return (
                                            context.dataset.label +
                                            ": " +
                                            Number(
                                                context.raw ||
                                                0
                                            ).toFixed(0) +
                                            "%"
                                        );
                                    }
                            }
                        }
                    }
                }
            }
        );
}


// ==========================================
// NOVO:
// Lista comparativa:
//
// Banco de Madeira
// Na sua praça: 5
// Original: 2
// +3
//
// Vaga de Carro
// Na sua praça: 0
// Original: 4
// -4
// ==========================================

function criarListaComparativa(
    dadosImaginados,
    dadosOriginais
) {

    const lista =
        document.getElementById(
            "modalListaItens"
        );


    if (!lista) {
        return;
    }


    lista.innerHTML =
        "";


    // Junta categorias que aparecem
    // em qualquer um dos dois lados.
    const categorias = [
        ...new Set([
            ...Object.keys(
                dadosImaginados
            ),
            ...Object.keys(
                dadosOriginais
            )
        ])
    ];


    categorias.forEach(categoria => {

        const grupo =
            document.createElement(
                "div"
            );


        grupo.className =
            "grupo-categoria";


        const titulo =
            document.createElement(
                "div"
            );


        titulo.className =
            "grupo-categoria-titulo";


        titulo.innerText =
            categoria;


        grupo.appendChild(
            titulo
        );


        const itensContainer =
            document.createElement(
                "div"
            );


        itensContainer.className =
            "lista-comparativa";


        const itensImaginados =
            dadosImaginados[
                categoria
            ] || {};


        const itensOriginais =
            dadosOriginais[
                categoria
            ] || {};


        // Junta os itens que aparecem
        // em qualquer um dos dois lados.
        const nomesItens = [
            ...new Set([
                ...Object.keys(
                    itensImaginados
                ),
                ...Object.keys(
                    itensOriginais
                )
            ])
        ];


        nomesItens.forEach(nome => {

            const quantidadeAtual =
                itensImaginados[
                    nome
                ] || 0;


            const quantidadeOriginal =
                itensOriginais[
                    nome
                ] || 0;


            const diferenca =
                quantidadeAtual -
                quantidadeOriginal;


            let classe =
                "igual";


            let sinal =
                "";


            if (
                diferenca > 0
            ) {

                classe =
                    "aumentou";

                sinal =
                    "+";
            }


            if (
                diferenca < 0
            ) {

                classe =
                    "diminuiu";
            }


            const linha =
                document.createElement(
                    "div"
                );


            linha.className =
                "item-comparativo";


            linha.innerHTML = `

                <div
                    class="item-comparativo-nome"
                >

                    <img
                        src="${iconeParaItem(nome)}"
                        alt=""
                    >

                    <span>
                        ${nome}
                    </span>

                </div>


                <div
                    class="item-comparativo-dados"
                >

                    <span>

                        <small>
                            Na sua praça
                        </small>

                        <strong>
                            ${quantidadeAtual}
                        </strong>

                    </span>


                    <span>

                        <small>
                            Original
                        </small>

                        <strong>
                            ${quantidadeOriginal}
                        </strong>

                    </span>


                    <span
                        class="badge-diferenca ${classe}"
                    >
                        ${sinal}${diferenca}
                    </span>

                </div>
            `;


            itensContainer.appendChild(
                linha
            );
        });


        grupo.appendChild(
            itensContainer
        );


        lista.appendChild(
            grupo
        );
    });
}


// ==========================================
// 2. BUSCA DE DADOS E SCROLL INFINITO
// ==========================================

let paginaAtual =
    0;

const itensPorPagina =
    12;

let carregando =
    false;

let chegouAoFim =
    false;


// Elementos da UI
const gallery =
    document.getElementById(
        "gallery"
    );

const popup =
    document.getElementById(
        "popup"
    );

const loader =
    document.getElementById(
        "fim-da-pagina"
    );


// ==========================================
// 2. BUSCA DE DADOS E SCROLL INFINITO
// ==========================================

async function carregarPracas() {

    if (
        carregando ||
        chegouAoFim
    ) {

        return;
    }


    carregando =
        true;


    // Lemos o que o utilizador escolheu nos filtros
    const ordem =
        document
            .getElementById(
                "filtroOrdem"
            )
            .value;


    const minItens =
        parseInt(
            document
                .getElementById(
                    "filtroItens"
                )
                .value
        );


    const itemEspecifico =
        document
            .getElementById(
                "filtroEspecifico"
            )
            .value;


    const inicio =
        paginaAtual *
        itensPorPagina;


    const fim =
        inicio +
        itensPorPagina -
        1;


    // Usamos "db" em vez de "supabase".
    // Quando filtra por item específico, chamamos uma função SQL (RPC) em
    // vez de montar o filtro com .ilike() direto no client — o Postgres
    // exige um cast (layout_data::text) pra comparar jsonb com ilike, e
    // esse cast não estava sendo repassado corretamente pelos filtros do
    // PostgREST client-side (voltava sempre o mesmo erro "operator does
    // not exist: jsonb ~~* unknown", mesmo escrevendo o cast). A função
    // SQL já faz esse cast por dentro, então evita o problema — veja
    // filtrar_pracas_por_item.sql pra criar essa função no Supabase.

    const colunas =
        'praca_id, image_topo_url, created_at, likes, total_objects';


    let query;


    if (
        itemEspecifico !==
        "todos"
    ) {

        query =
            db.rpc(
                'filtrar_pracas_por_item',
                {
                    item_nome:
                        itemEspecifico
                }
            ).select(
                colunas
            );

    } else {

        query =
            db
                .from(
                    'city_creations'
                )
                .select(
                    colunas
                );
    }


    // Aplica o filtro de Mínimo de Itens
    query =
        query.gte(
            'total_objects',
            minItens
        );


    // Aplica a Ordenação
    if (
        ordem ===
        "recentes"
    ) {

        query =
            query.order(
                'created_at',
                {
                    ascending:
                        false
                }
            );
    }


    if (
        ordem ===
        "antigas"
    ) {

        query =
            query.order(
                'created_at',
                {
                    ascending:
                        true
                }
            );
    }


    if (
        ordem ===
        "likes"
    ) {

        query =
            query.order(
                'likes',
                {
                    ascending:
                        false
                }
            );
    }


    // Aplica a paginação
    const {
        data,
        error
    } =
        await query.range(
            inicio,
            fim
        );


    if (error) {

        console.error(
            "Erro ao buscar:",
            error
        );


        loader.innerText =
            "Erro ao carregar: " +
            (
                error.message ||
                JSON.stringify(error)
            );


        carregando =
            false;


        return;
    }


    if (
        data.length <
        itensPorPagina
    ) {

        chegouAoFim =
            true;


        loader.innerText =
            "Chegou ao fim da galeria!";
    }


    desenharCards(
        data
    );


    paginaAtual++;

    carregando =
        false;
}


function desenharCards(
    pracas
) {

    pracas.forEach(praca => {

        const dataFormatada =
            new Date(
                praca.created_at
            )
            .toLocaleDateString(
                'pt-PT'
            );


        const card =
            document.createElement(
                "div"
            );


        card.className =
            "card-praca";


        card.innerHTML = `

            <img
                src="${praca.image_topo_url}"
                class="card-img"
                loading="lazy"
                alt="Praça"
            >

            <div class="card-info">

                <span class="card-data">
                    📅 ${dataFormatada}
                </span>

                <span class="card-likes">
                    ❤️ ${praca.likes || 0}
                </span>

            </div>
        `;


        card.addEventListener(
            "click",
            () => {

                window.location.hash =
                    praca.praca_id;
            }
        );


        gallery.appendChild(
            card
        );
    });
}


// O Observador que dispara quando chegamos ao fim da página
const observer =
    new IntersectionObserver(
        (entradas) => {

            if (
                entradas[0]
                    .isIntersecting
            ) {

                carregarPracas();
            }
        }
    );


observer.observe(
    loader
);


// ==========================================
// 3. RECARREGAR AO MUDAR OS FILTROS
// ==========================================

function aplicarFiltros() {

    gallery.innerHTML =
        "";


    paginaAtual =
        0;


    chegouAoFim =
        false;


    loader.innerText =
        "A carregar mais praças...";


    carregarPracas();
}


document
    .getElementById(
        "filtroOrdem"
    )
    .addEventListener(
        "change",
        aplicarFiltros
    );


document
    .getElementById(
        "filtroItens"
    )
    .addEventListener(
        "change",
        aplicarFiltros
    );


document
    .getElementById(
        "filtroEspecifico"
    )
    .addEventListener(
        "change",
        aplicarFiltros
    );


// ==========================================
// 4. SISTEMA DE HASH ROUTING
// ==========================================

window.addEventListener(
    "hashchange",
    lidarComNavegacao
);


async function lidarComNavegacao() {

    const hash =
        window.location.hash.replace(
            "#",
            ""
        );


    if (!hash) {

        popup.classList.add(
            "escondido"
        );


        document.body.style.overflow =
            "auto";


        return;
    }


    document
        .getElementById(
            "modalTitulo"
        )
        .innerText =
            "A carregar dados...";


    popup.classList.remove(
        "escondido"
    );


    document.body.style.overflow =
        "hidden";


    // Usamos "db" em vez de "supabase".
    // NOTA: .eq('praca_id', hash).single() quebrava assim que o mesmo
    // praca_id passasse a ter mais de uma linha (cada edição salva de
    // novo). Agora buscamos sempre a versão mais recente.

    const {
        data,
        error
    } =
        await db
            .from('city_creations')
            .select('*')
            .eq(
                'praca_id',
                hash
            )
            .order(
                'created_at',
                {
                    ascending:
                        false
                }
            )
            .limit(1)
            .single();


    if (error) {

        console.error(
            "Erro ao carregar praça:",
            error
        );

        alert(
            "Erro ao carregar a praça."
        );

        window.location.hash =
            "";

        return;
    }


    if (data) {

        await preencherModal(
            data
        );

    } else {

        alert(
            "Praça não encontrada!"
        );

        window.location.hash =
            "";
    }
}


document
    .getElementById(
        "btnFecharModal"
    )
    .addEventListener(
        "click",
        () =>
            window.location.hash =
                ""
    );


popup.addEventListener(
    "click",
    (e) => {

        if (
            e.target ===
            popup
        ) {

            window.location.hash =
                "";
        }
    }
);


// ==========================================
// 5. PREENCHER O MODAL
// ==========================================

let pracaAbertaId =
    null;


async function preencherModal(
    praca
) {

    pracaAbertaId =
        praca.praca_id;


    // --- Imagens: abre sempre na vista de topo ---

    document
        .getElementById(
            "selectedImage"
        )
        .src =
            praca.image_topo_url;


    document
        .getElementById(
            "btnVistaTopo"
        )
        .classList.add(
            "ativo"
        );


    document
        .getElementById(
            "btnVistaAngulo"
        )
        .classList.remove(
            "ativo"
        );


    document
        .getElementById(
            "btnVistaTopo"
        )
        .onclick =
            () => {

                document
                    .getElementById(
                        "selectedImage"
                    )
                    .src =
                        praca.image_topo_url;


                document
                    .getElementById(
                        "btnVistaTopo"
                    )
                    .classList.add(
                        "ativo"
                    );


                document
                    .getElementById(
                        "btnVistaAngulo"
                    )
                    .classList.remove(
                        "ativo"
                    );
            };


    document
        .getElementById(
            "btnVistaAngulo"
        )
        .onclick =
            () => {

                document
                    .getElementById(
                        "selectedImage"
                    )
                    .src =
                        praca.image_url;


                document
                    .getElementById(
                        "btnVistaAngulo"
                    )
                    .classList.add(
                        "ativo"
                    );


                document
                    .getElementById(
                        "btnVistaTopo"
                    )
                    .classList.remove(
                        "ativo"
                    );
            };


    document
        .getElementById(
            "btnRemix"
        )
        .href =
            "https://feliperpv.com/repraca/galeria/abrir-app/?id=" +
            praca.praca_id;


    // ------------------------------------------
    // NOVO:
    // DADOS DA PRAÇA IMAGINADA
    // ------------------------------------------

    const jsonConvertido =
        JSON.parse(
            praca.layout_data
        );


    const itens =
        jsonConvertido.layoutDaPraca ||
        [];


    // Título/nome da praça-base.
    // Usamos exatamente a mesma lógica que já existia.
    const nomeBase =
        praca.nome_da_cena ||
        jsonConvertido.nomeDaCena ||
        "";


    // ------------------------------------------
    // NOVO:
    // Conta os itens criados pelo usuário.
    // ------------------------------------------

    const itensPorCategoria =
        contarItensImaginados(
            itens
        );


    // ------------------------------------------
    // NOVO:
    // Busca o JSON da praça original.
    //
    // Exemplo:
    //
    // "Estacionamento"
    //       ↓
    // dados/pracas/estacionamento.json
    // ------------------------------------------

    const dadosPracaOriginal =
        await carregarDadosDaPraca(
            nomeBase
        );


    // ------------------------------------------
    // NOVO:
    // Organiza os dados originais usando
    // as categorias cadastradas em itens.json.
    // ------------------------------------------

    const itensOriginais =
        organizarItensOriginais(
            dadosPracaOriginal.itens ||
            {}
        );


    // ------------------------------------------
    // NOVO:
    // Desenha o radar.
    // ------------------------------------------

    desenharRadar(
        itensOriginais,
        itensPorCategoria
    );


    // ------------------------------------------
    // NOVO:
    // Monta a lista comparativa.
    // ------------------------------------------

    criarListaComparativa(
        itensPorCategoria,
        itensOriginais
    );


    // "rePraça {numero}" = id da LINHA na tabela
    // (não o praca_id, que é o UUID)
    document
        .getElementById(
            "modalNumero"
        )
        .innerText =
            "rePraça " +
            praca.id;


    // Título = o nome que o criador deu à própria criação;
    // se não tiver (linhas antigas sem esse campo),
    // cai pro nome da praça-base.

    const titulo =
        (
            praca.titulo &&
            praca.titulo.trim()
        )
            ? praca.titulo
            : (
                nomeBase ||
                "Praça Personalizada"
            );


    document
        .getElementById(
            "modalTitulo"
        )
        .innerText =
            titulo;


    const baseadoEmEl =
        document.getElementById(
            "modalBaseadoEm"
        );


    if (
        nomeBase &&
        nomeBase !== titulo
    ) {

        baseadoEmEl.innerText =
            "baseado em: " +
            nomeBase;


        baseadoEmEl.style.display =
            "";

    } else {

        baseadoEmEl.style.display =
            "none";
    }


    document
        .getElementById(
            "modalData"
        )
        .innerText =
            "Última edição: " +
            praca.created_at
                .substring(
                    0,
                    10
                )
                .split('-')
                .reverse()
                .join('/');


    document
        .getElementById(
            "modalTotalItens"
        )
        .innerText =
            praca.total_objects;


    // ------------------------------------------
    // Foco da praça imaginada.
    // ------------------------------------------

    let categoriaPrincipal =
        "Mista";


    let max =
        0;


    for (
        let cat in itensPorCategoria
    ) {

        const totalNaCategoria =
            Object.values(
                itensPorCategoria[cat]
            )
            .reduce(
                (a, b) =>
                    a + b,
                0
            );


        if (
            totalNaCategoria >
            max
        ) {

            max =
                totalNaCategoria;


            categoriaPrincipal =
                cat;
        }
    }


    document
        .getElementById(
            "modalCategoria"
        )
        .innerText =
            categoriaPrincipal;


    const comentarioEl =
        document.getElementById(
            "modalComentario"
        );


    if (
        praca.comentario &&
        praca.comentario.trim()
    ) {

        comentarioEl.innerText =
            praca.comentario;


        comentarioEl.style.display =
            "";

    } else {

        comentarioEl.style.display =
            "none";
    }


    verificarStatusDoLike(
        praca.praca_id,
        praca.likes || 0
    );


    // Sempre volta pra aba "itens"
    // ao abrir uma praça
    trocarAba(
        "itens"
    );


    carregarRemixes(
        praca.praca_id
    );


    carregarHistorico(
        praca.praca_id,
        praca.id
    );
}


// ==========================================
// 5b. ABAS (itens / remixes / histórico)
// ==========================================

document
    .querySelectorAll(
        ".tab-btn"
    )
    .forEach(btn => {

        btn.addEventListener(
            "click",
            () =>
                trocarAba(
                    btn.dataset.tab
                )
        );
    });


function trocarAba(
    nomeAba
) {

    document
        .querySelectorAll(
            ".tab-btn"
        )
        .forEach(btn => {

            btn.classList.toggle(
                "ativo",
                btn.dataset.tab ===
                nomeAba
            );
        });


    document
        .querySelectorAll(
            ".tab-painel"
        )
        .forEach(painel =>
            painel.classList.remove(
                "ativo"
            )
        );


    const painel =
        document.getElementById(
            "painel" +
            nomeAba
                .charAt(0)
                .toUpperCase() +
            nomeAba.slice(1)
        );


    if (painel) {

        painel.classList.add(
            "ativo"
        );
    }


    document
        .querySelector(
            ".tabs-conteudo"
        )
        .className =
            "tabs-conteudo tabs-conteudo--" +
            nomeAba;
}


// ==========================================
// 5c. ABA "REMIXES" — criações filhas desta praça
// ==========================================

async function carregarRemixes(
    pracaId
) {

    const container =
        document.getElementById(
            "listaRemixes"
        );


    container.innerHTML =
        "<p class='texto-dica'>A carregar...</p>";


    const {
        data,
        error
    } =
        await db
            .from('city_creations')
            .select(
                'praca_id, image_topo_url, created_at'
            )
            .eq(
                'praca_pai_id',
                pracaId
            )
            .order(
                'created_at',
                {
                    ascending:
                        false
                }
            );


    if (error) {

        container.innerHTML =
            "<p class='texto-dica'>" +
            "Não foi possível carregar os remixes." +
            "</p>";


        return;
    }


    if (
        !data ||
        data.length ===
        0
    ) {

        container.innerHTML = `

            <div
                class="remix-vazio"
            >

                Ninguém reimaginou esta praça ainda.

                <br/>

                <a
                    href="https://feliperpv.com/repraca/galeria/abrir-app/?id=${pracaId}"
                >
                    Seja o primeiro a remixar →
                </a>

            </div>
        `;


        return;
    }


    container.innerHTML =
        "";


    data.forEach(
        filho => {

            const linha =
                document.createElement(
                    "div"
                );


            linha.className =
                "remix-card";


            linha.innerHTML = `

                <span>

                    ${
                        new Date(
                            filho.created_at
                        )
                        .toLocaleDateString(
                            'pt-PT'
                        )
                    }

                </span>


                <img
                    src="${filho.image_topo_url}"
                    alt=""
                    style="
                        width:40px;
                        height:40px;
                        border-radius:8px;
                        object-fit:cover;
                    "
                >
            `;


            linha.addEventListener(
                "click",
                () => {

                    window.location.hash =
                        filho.praca_id;
                }
            );


            container.appendChild(
                linha
            );
        }
    );
}


// ==========================================
// 5d. ABA "HISTÓRICO" — versões anteriores do mesmo praca_id
// ==========================================
//
// NOTA: aqui eu listo as versões antigas só como informação
// (data + número da linha) — não fiz elas abrirem, porque
// a navegação por hash hoje é pelo praca_id (que é igual em
// todas as versões), então clicar não teria como carregar
// especificamente UMA versão antiga sem mudar esse esquema.
// Se quiser isso navegável, dá pra fazer, mas é uma mudança
// um pouco maior na forma como a URL identifica a praça.
//

async function carregarHistorico(
    pracaId,
    idAtual
) {

    const abaBtn =
        document.getElementById(
            "btnTabHistorico"
        );


    const container =
        document.getElementById(
            "listaHistorico"
        );


    const {
        data,
        error
    } =
        await db
            .from('city_creations')
            .select(
                'id, created_at'
            )
            .eq(
                'praca_id',
                pracaId
            )
            .order(
                'created_at',
                {
                    ascending:
                        false
                }
            );


    if (
        error ||
        !data ||
        data.length <= 1
    ) {

        // Sem edições anteriores —
        // some com a aba (mas "remixes"
        // continua ali)

        abaBtn.style.display =
            "none";


        if (
            abaBtn.classList.contains(
                "ativo"
            )
        ) {

            trocarAba(
                "itens"
            );
        }


        return;
    }


    abaBtn.style.display =
        "";


    container.innerHTML =
        "";


    data.forEach(
        versao => {

            const linha =
                document.createElement(
                    "div"
                );


            linha.className =
                "historico-linha";


            linha.innerHTML = `

                <span>

                    ${
                        new Date(
                            versao.created_at
                        )
                        .toLocaleDateString(
                            'pt-PT'
                        )
                    }

                </span>


                <span>

                    ${
                        versao.id === idAtual
                            ? "atual"
                            : "rePraça " +
                              versao.id
                    }

                </span>
            `;


            container.appendChild(
                linha
            );
        }
    );
}


// ==========================================
// 6. SISTEMA DE LIKES
// ==========================================

const btnLike =
    document.getElementById(
        "btnLikeModal"
    );


function verificarStatusDoLike(
    id,
    totalLikes
) {

    document
        .getElementById(
            "modalLikesCount"
        )
        .innerText =
            totalLikes;


    if (
        localStorage.getItem(
            "liked_" + id
        )
    ) {

        btnLike.classList.add(
            "curtido"
        );


        btnLike.disabled =
            true;

    } else {

        btnLike.classList.remove(
            "curtido"
        );


        btnLike.disabled =
            false;


        btnLike.onclick =
            () =>
                enviarLikeParaSupabase(
                    id
                );
    }
}


async function enviarLikeParaSupabase(
    id
) {

    btnLike.disabled =
        true;


    // Usamos "db" em vez de "supabase"

    const {
        error
    } =
        await db.rpc(
            'dar_like',
            {
                id_praca:
                    id
            }
        );


    if (!error) {

        let currentLikes =
            parseInt(
                document
                    .getElementById(
                        "modalLikesCount"
                    )
                    .innerText
            );


        document
            .getElementById(
                "modalLikesCount"
            )
            .innerText =
                currentLikes + 1;


        btnLike.classList.add(
            "curtido"
        );


        localStorage.setItem(
            "liked_" + id,
            "true"
        );

    } else {

        btnLike.disabled =
            false;


        alert(
            "Erro ao registar o gosto!"
        );
    }
}


// ==========================================
// NOVO:
// INICIALIZAÇÃO
// ==========================================
//
// Primeiro carrega itens.json.
// Depois monta o filtro.
// Só então começa a galeria.
//
// Assim o itens.json é baixado UMA VEZ
// por carregamento da página.
// ==========================================

async function iniciarGaleria() {

    await carregarCatalogoItens();


    preencherFiltroDeItens();


    lidarComNavegacao();


    carregarPracas();
}


iniciarGaleria();