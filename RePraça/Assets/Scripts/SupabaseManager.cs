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

    // NOVO: Agora a função pede DUAS imagens (Angulo e Topo)
    public async Task UploadCreationData(string playerName, string jsonLayoutData, byte[] imageAnguloBytes, byte[] imageTopoBytes, int totalObjects)
    {
        Debug.Log("Iniciando upload para a nuvem...");

        // PASSO A: Nomes únicos para as duas imagens
        string fileAngulo = $"praca_ang_{System.Guid.NewGuid()}.png";
        string fileTopo = $"praca_topo_{System.Guid.NewGuid()}.png";

        // PASSO B: Fazer o upload das DUAS imagens simultaneamente (Task.WhenAll deixa mais rápido!)
        var taskAngulo = UploadImageToStorage(imageAnguloBytes, fileAngulo);
        var taskTopo = UploadImageToStorage(imageTopoBytes, fileTopo);

        await Task.WhenAll(taskAngulo, taskTopo);

        if (taskAngulo.Result == null || taskTopo.Result == null)
        {
            Debug.LogError("Falha ao fazer upload de uma das imagens. Cancelando envio dos dados.");
            return;
        }

        // PASSO C: Pegar as URLs públicas das duas imagens
        string urlAngulo = supabaseClient.Storage.From("praca_images").GetPublicUrl(fileAngulo);
        string urlTopo = supabaseClient.Storage.From("praca_images").GetPublicUrl(fileTopo);

        // PASSO D: Enviar o JSON e as DUAS URLs para o Banco de Dados
        await SaveDataToDatabase(playerName, jsonLayoutData, urlAngulo, urlTopo, totalObjects);
    }

    private async Task<string> UploadImageToStorage(byte[] imageBytes, string fileName)
    {
        try
        {
            var storage = supabaseClient.Storage.From("praca_images");
            var result = await storage.Upload(imageBytes, fileName, new Supabase.Storage.FileOptions { ContentType = "image/png" });
            Debug.Log($"Imagem {fileName} salva com sucesso no bucket!");
            return result;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Erro no Upload do Storage: " + e.Message);
            return null;
        }
    }

    private async Task SaveDataToDatabase(string playerName, string jsonPayload, string urlAngulo, string urlTopo, int totalObjects)
    {
        try
        {
            var insertData = new CityCreationModel
            {
                PlayerName = playerName,
                ImageUrl = urlAngulo,
                ImageTopoUrl = urlTopo, // Passando a URL da imagem de topo
                TotalObjects = totalObjects,
                LayoutData = jsonPayload
            };

            var result = await supabaseClient.From<CityCreationModel>().Insert(insertData);
            Debug.Log("✅ Criação da praça salva no banco de dados com sucesso!");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Erro no banco de dados: " + e.Message);
        }
    }


    [Postgrest.Attributes.Table("city_creations")]
    class CityCreationModel : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.Column("player_name")]
        public string PlayerName { get; set; }

        //imagem em angulo
        [Postgrest.Attributes.Column("image_url")]
        public string ImageUrl { get; set; }

        // imagem de topo
        [Postgrest.Attributes.Column("image_topo_url")]
        public string ImageTopoUrl { get; set; }

        //  total de objetos
        [Postgrest.Attributes.Column("total_objects")]
        public int TotalObjects { get; set; }

        [Postgrest.Attributes.Column("layout_data")]
        public string LayoutData { get; set; }
    }
}