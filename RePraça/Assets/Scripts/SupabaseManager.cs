using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Supabase;
using System.Threading.Tasks;

public class SupabaseManager : MonoBehaviour
{
    private Client supabaseClient;

    async void Start()
    {
        var options = new SupabaseOptions { AutoConnectRealtime = false };

        // Puxa as chaves do arquivo secreto
        supabaseClient = new Client(Secrets.SUPABASE_URL, Secrets.SUPABASE_KEY, options);
        await supabaseClient.InitializeAsync();

        Debug.Log("Supabase Conectado!");
    }

    public async Task<bool> UploadCreationData(string playerName, string jsonLayoutData, byte[] imageAnguloBytes, byte[] imageTopoBytes, int totalObjects, Action<float> onProgress)
    {
        Debug.Log("Iniciando upload para a nuvem...");

        // Nomes únicos para as duas imagens
        string fileAngulo = $"praca_ang_{System.Guid.NewGuid()}.png";
        string fileTopo = $"praca_topo_{System.Guid.NewGuid()}.png";

        // Variáveis para guardar o progresso individual de cada imagem
        float progressoAngulo = 0f;
        float progressoTopo = 0f;

        // Sempre que uma imagem avança, calculamos a média das duas e avisamos a UI!
        EventHandler<float> atualizaAngulo = (sender, val) => { progressoAngulo = val; onProgress?.Invoke((progressoAngulo + progressoTopo) / 2f); };
        EventHandler<float> atualizaTopo = (sender, val) => { progressoTopo = val; onProgress?.Invoke((progressoAngulo + progressoTopo) / 2f); };

        // Fazer o upload das DUAS imagens simultaneamente
        var taskAngulo = UploadImageToStorage(imageAnguloBytes, fileAngulo, atualizaAngulo);
        var taskTopo = UploadImageToStorage(imageTopoBytes, fileTopo, atualizaTopo);

        // CORREÇÃO 2: Bloco de espera duplicado foi removido
        await Task.WhenAll(taskAngulo, taskTopo);

        if (taskAngulo.Result == null || taskTopo.Result == null)
        {
            Debug.LogError("Falha ao fazer upload de uma das imagens. Cancelando envio dos dados.");
            return false;
        }

        // Pegar as URLs públicas das duas imagens
        string urlAngulo = supabaseClient.Storage.From("praca_images").GetPublicUrl(fileAngulo);
        string urlTopo = supabaseClient.Storage.From("praca_images").GetPublicUrl(fileTopo);

        // Enviar o JSON e as DUAS URLs para o Banco de Dados
        return await SaveDataToDatabase(playerName, jsonLayoutData, urlAngulo, urlTopo, totalObjects);
    }

    private async Task<string> UploadImageToStorage(byte[] imageBytes, string fileName, EventHandler<float> onProgress)
    {
        try
        {
            var storage = supabaseClient.Storage.From("praca_images");
            var result = await storage.Upload(imageBytes, fileName, new Supabase.Storage.FileOptions { ContentType = "image/png" }, onProgress);
            return result;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Erro no Upload do Storage: " + e.Message);
            return null;
        }
    }

    private async Task<bool> SaveDataToDatabase(string playerName, string jsonPayload, string urlAngulo, string urlTopo, int totalObjects)
    {
        try
        {
            var insertData = new CityCreationModel
            {
                PlayerName = playerName,
                ImageUrl = urlAngulo,
                ImageTopoUrl = urlTopo,
                TotalObjects = totalObjects,
                LayoutData = jsonPayload
            };

            var result = await supabaseClient.From<CityCreationModel>().Insert(insertData);
            Debug.Log("✅ Criação da praça salva no banco de dados com sucesso!");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Erro no banco de dados: " + e.Message);
            return false;
        }
    }


    [Postgrest.Attributes.Table("city_creations")]
    class CityCreationModel : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.Column("player_name")]
        public string PlayerName { get; set; }

        [Postgrest.Attributes.Column("image_url")]
        public string ImageUrl { get; set; }

        [Postgrest.Attributes.Column("image_topo_url")]
        public string ImageTopoUrl { get; set; }

        [Postgrest.Attributes.Column("total_objects")]
        public int TotalObjects { get; set; }

        [Postgrest.Attributes.Column("layout_data")]
        public string LayoutData { get; set; }
    }
}