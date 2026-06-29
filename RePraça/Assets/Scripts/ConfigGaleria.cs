using UnityEngine;
using UnityEngine.UI;
using static NativeGallery;

public class ConfigGaleria : MonoBehaviour
{
    [Header("UI da Configuração")]
    public Toggle togglePermissao;

    void Start()
    {
        // 1. Verifica a preferência do jogador (1 = Quer salvar, 0 = Não quer)
        bool querSalvar = PlayerPrefs.GetInt("SalvarGaleria") == 1; //confere se o dado salvo é igual a 1 (quer salvar). se nao achar nada por ser a primeira vez no app, o dado vai ser 0

        // 2. CORREÇÃO: CheckPermission retorna apenas um bool (true = tem permissão, false = não tem)
        bool temPermissao = NativeGallery.CheckPermission(PermissionType.Write, MediaType.Image);

        // 3. A Checkbox só aparece ligada se o jogador QUER salvar E o telemóvel PERMITE
        bool deveEstarLigado = querSalvar && temPermissao;

        togglePermissao.SetIsOnWithoutNotify(deveEstarLigado);
        togglePermissao.onValueChanged.AddListener(AoClicarNaCheckbox);
    }

    void AoClicarNaCheckbox(bool ligado)
    {
        if (ligado)
        {
            // Verificamos de novo apenas com o bool
            bool temPermissao = NativeGallery.CheckPermission(PermissionType.Write, MediaType.Image);

            if (temPermissao)
            {
                // CENÁRIO A: O telemóvel já tem permissão. Apenas gravamos a preferência do jogador.
                PlayerPrefs.SetInt("SalvarGaleria", 1);
                PlayerPrefs.Save();
            }
            else
            {
                // CENÁRIO B & C: Não tem permissão. Vamos pedir (o sistema decide se mostra o pop-up ou bloqueia)
                NativeGallery.RequestPermissionAsync((resultado) =>
                {
                    if (resultado == Permission.Granted)
                    {
                        // Sucesso! O jogador clicou "Sim".
                        PlayerPrefs.SetInt("SalvarGaleria", 1);
                    }
                    else if (resultado == Permission.ShouldAsk)
                    {
                        // O jogador clicou "Não" no pop-up desta vez. 
                        togglePermissao.SetIsOnWithoutNotify(false);
                        PlayerPrefs.SetInt("SalvarGaleria", 0);
                    }
                    else if (resultado == Permission.Denied)
                    {
                        // CENÁRIO C: Permissão foi permanentemente recusada no passado pelo iOS/Android.
                        Debug.Log("Permissão permanentemente negada. A abrir as definições do dispositivo...");
                        NativeGallery.OpenSettings();

                        // Desligamos a checkbox. Quando ele voltar das configurações e tentar clicar de novo,
                        // se tiver ativado a permissão lá, o CENÁRIO A assume o controlo!
                        togglePermissao.SetIsOnWithoutNotify(false);
                        PlayerPrefs.SetInt("SalvarGaleria", 0);
                    }

                    // Salva a alteração no disco imediatamente
                    PlayerPrefs.Save();

                }, PermissionType.Write, MediaType.Image);
            }
        }
        else
        {
            // CENÁRIO D: O jogador desligou a checkbox manualmente.
            PlayerPrefs.SetInt("SalvarGaleria", 0);
            PlayerPrefs.Save();
        }
    }
}