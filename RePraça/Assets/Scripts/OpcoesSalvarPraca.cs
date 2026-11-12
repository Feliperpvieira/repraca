using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static NativeGallery;

// Painel "Opções - Salvar praça", aberto pelo botão de opções da tela de exportar.
// Trata os três toggles: galeria do dispositivo (com o pedido de permissão,
// antes no ConfigGaleria), galeria online e salvar como cópia.
// O CameraCapture lê estas opções no ConfirmarSalvar.
public class OpcoesSalvarPraca : MonoBehaviour
{
    // 1 = envia para a galeria online (padrão), 0 = salva só no dispositivo
    public const string ChaveExportarOnline = "ExportarOnline";
    // 1 = salva também as imagens na galeria de fotos (padrão 0, porque pede permissão)
    public const string ChaveSalvarGaleria = "SalvarGaleria";

    [Header("Painel")]
    public GameObject painelOpcoes;         // raiz do painel (com o fundo), liga/desliga
    public RectTransform conteudoPainel;    // o cartão branco que faz a animação de escala

    [Header("Toggles")]
    public Toggle toggleGaleria;
    public Toggle toggleExportarOnline;
    public Toggle toggleSalvarCopia;

    [Header("Botão de guardar")]
    public TMP_Text textoBotaoGuardar;      // texto do botão de guardar, muda conforme a galeria online e a cópia
    public string textoGuardarEExportar = "Guardar e Publicar";
    public string textoSoGuardar = "Guardar";
    public string textoGuardarCopiaEPublicar = "Guardar cópia e Publicar";
    public string textoGuardarCopia = "Guardar cópia";

    private BuildingManager buildingManager;
    private CanvasGroup grupoPainel;
    private Vector3 tamanhoOrgConteudo;
    private bool inicializado = false;

    public static bool ExportarOnline => PlayerPrefs.GetInt(ChaveExportarOnline, 1) == 1;
    public static bool SalvarGaleria => PlayerPrefs.GetInt(ChaveSalvarGaleria, 0) == 1;

    // Só conta se o toggle estiver ligado E clicável (praça já salva antes)
    public bool SalvarComoCopia =>
        toggleSalvarCopia != null && toggleSalvarCopia.interactable && toggleSalvarCopia.isOn;

    void Awake()
    {
        Inicializar();
    }

    // Os métodos públicos podem ser chamados com o painel ainda desligado
    // (o Awake ainda não correu), por isso todos passam por aqui.
    private void Inicializar()
    {
        if (inicializado)
            return;
        inicializado = true;

        GameObject bm = GameObject.Find("BuildingManager");
        if (bm != null)
            buildingManager = bm.GetComponent<BuildingManager>();

        if (painelOpcoes != null)
        {
            grupoPainel = painelOpcoes.GetComponent<CanvasGroup>();
            if (grupoPainel == null)
                grupoPainel = painelOpcoes.AddComponent<CanvasGroup>();
        }

        if (conteudoPainel != null)
            tamanhoOrgConteudo = conteudoPainel.localScale;

        if (toggleExportarOnline != null)
        {
            toggleExportarOnline.SetIsOnWithoutNotify(ExportarOnline);
            toggleExportarOnline.onValueChanged.AddListener(AoMudarExportarOnline);
        }

        if (toggleSalvarCopia != null)
        {
            // Começa sempre desligado, seja qual for o estado do toggle na cena
            toggleSalvarCopia.SetIsOnWithoutNotify(false);
            toggleSalvarCopia.onValueChanged.AddListener(_ => AtualizarTextoBotao());
        }

        AtualizarTextoBotao();

        if (toggleGaleria != null)
        {
            AtualizarToggleGaleria();
            toggleGaleria.onValueChanged.AddListener(AoMudarGaleria);
        }
    }

    // O toggle só aparece ligado se o jogador QUER salvar E o telemóvel PERMITE
    // (a permissão pode ter sido retirada nas definições do dispositivo).
    private void AtualizarToggleGaleria()
    {
        if (toggleGaleria == null)
            return;

        bool temPermissao = NativeGallery.CheckPermission(PermissionType.Write, MediaType.Image);
        toggleGaleria.SetIsOnWithoutNotify(SalvarGaleria && temPermissao);
    }

    private void AoMudarGaleria(bool ligado)
    {
        if (!ligado)
        {
            // O jogador desligou manualmente
            DefinirSalvarGaleria(false);
            return;
        }

        if (NativeGallery.CheckPermission(PermissionType.Write, MediaType.Image))
        {
            // Já tem permissão: só grava a preferência
            DefinirSalvarGaleria(true);
            return;
        }

        // Não tem permissão: pede (o sistema decide se mostra o pop-up ou bloqueia)
        NativeGallery.RequestPermissionAsync((resultado) =>
        {
            if (resultado == Permission.Granted)
            {
                DefinirSalvarGaleria(true);
            }
            else
            {
                if (resultado == Permission.Denied)
                {
                    // Recusada permanentemente no passado: abre as definições do dispositivo.
                    // Quando voltar e tocar de novo, se tiver autorizado, cai no caso "já tem permissão".
                    Debug.Log("Permissão permanentemente negada. A abrir as definições do dispositivo...");
                    NativeGallery.OpenSettings();
                }

                // ShouldAsk (clicou "Não" no pop-up) ou Denied
                toggleGaleria.SetIsOnWithoutNotify(false);
                DefinirSalvarGaleria(false);
            }
        }, PermissionType.Write, MediaType.Image);
    }

    private void DefinirSalvarGaleria(bool ligado)
    {
        PlayerPrefs.SetInt(ChaveSalvarGaleria, ligado ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void AoMudarExportarOnline(bool ligado)
    {
        PlayerPrefs.SetInt(ChaveExportarOnline, ligado ? 1 : 0);
        PlayerPrefs.Save();
        AtualizarTextoBotao();
    }

    // Texto do botão conforme as opções: publicar ou só local, cópia ou não
    private void AtualizarTextoBotao()
    {
        if (textoBotaoGuardar == null)
            return;

        if (SalvarComoCopia)
            textoBotaoGuardar.text = ExportarOnline ? textoGuardarCopiaEPublicar : textoGuardarCopia;
        else
            textoBotaoGuardar.text = ExportarOnline ? textoGuardarEExportar : textoSoGuardar;
    }

    // Chamado pelo CameraCapture sempre que a tela de exportar abre
    public void PrepararParaNovaExportacao()
    {
        Inicializar();

        if (painelOpcoes != null)
        {
            LeanTween.cancel(painelOpcoes);
            if (conteudoPainel != null)
            {
                LeanTween.cancel(conteudoPainel.gameObject);
                conteudoPainel.localScale = tamanhoOrgConteudo;
            }
            painelOpcoes.SetActive(false);
        }

        if (toggleExportarOnline != null)
            toggleExportarOnline.SetIsOnWithoutNotify(ExportarOnline);

        AtualizarToggleGaleria();
        // Mantém a escolha de "Guardar como cópia" se o jogador voltou a editar sem
        // guardar; só a desliga se deixou de estar disponível. Depois de guardar,
        // o CameraCapture chama AtualizarOpcaoCopia, que a desliga.
        AtualizarDisponibilidadeCopia();
    }

    // Desliga "Salvar como cópia" e só a deixa clicável se a praça já tiver arquivo no dispositivo.
    // Também é chamado pelo CameraCapture logo depois de salvar.
    public void AtualizarOpcaoCopia()
    {
        Inicializar();

        bool disponivel = buildingManager != null && buildingManager.PodeSalvarComoCopia();

        if (toggleSalvarCopia != null)
        {
            toggleSalvarCopia.SetIsOnWithoutNotify(false);
            toggleSalvarCopia.interactable = disponivel;
        }

        AtualizarTextoBotao();
    }

    // Liga/desliga o interactable conforme haja arquivo no dispositivo, mantendo
    // a escolha do jogador (só força desligado quando fica indisponível).
    private void AtualizarDisponibilidadeCopia()
    {
        if (toggleSalvarCopia == null)
            return;

        bool disponivel = buildingManager != null && buildingManager.PodeSalvarComoCopia();
        toggleSalvarCopia.interactable = disponivel;
        if (!disponivel)
            toggleSalvarCopia.SetIsOnWithoutNotify(false);

        AtualizarTextoBotao();
    }

    // Botão de opções da tela de exportar (mesma animação da tela de exportar)
    public void AbrirOpcoes()
    {
        Inicializar();
        if (painelOpcoes == null)
            return;

        // Só confere se a cópia está disponível; não desliga o toggle se o jogador
        // o ligou, fechou e reabriu o painel. A reposição completa é feita ao abrir
        // a tela de exportar (PrepararParaNovaExportacao) e depois de salvar.
        AtualizarDisponibilidadeCopia();

        painelOpcoes.SetActive(true);
        LeanTween.cancel(painelOpcoes);
        grupoPainel.alpha = 0f;
        LeanTween.alphaCanvas(grupoPainel, 1f, 0.18f).setEaseOutQuad();

        if (conteudoPainel != null)
        {
            LeanTween.cancel(conteudoPainel.gameObject);
            conteudoPainel.localScale = tamanhoOrgConteudo * 0.92f;
            LeanTween.scale(conteudoPainel, tamanhoOrgConteudo, 0.36f).setEaseOutBack();
        }
    }

    // Botão X do painel
    public void FecharOpcoes()
    {
        Inicializar();
        if (painelOpcoes == null || !painelOpcoes.activeSelf)
            return;

        LeanTween.cancel(painelOpcoes);
        LeanTween.alphaCanvas(grupoPainel, 0f, 0.14f).setEaseInQuad()
            .setOnComplete(() => painelOpcoes.SetActive(false));

        if (conteudoPainel != null)
        {
            LeanTween.cancel(conteudoPainel.gameObject);
            LeanTween.scale(conteudoPainel, tamanhoOrgConteudo * 0.96f, 0.14f).setEaseInQuad();
        }
    }
}
