using UnityEditor;
using UnityEngine;

// O Unity 6 importa a cor dos materiais embutidos nos .fbx/.blend com o valor linear
// do Blender, mas a cor de um material no Unity é sempre guardada em sRGB (gamma),
// tanto em projeto Gamma como Linear. Sem a conversão os objetos ficam escuros.
// NÃO APAGAR: ao apagar, os modelos são reimportados sem a correção.
public class CorrigeCoresBlender : AssetPostprocessor
{
    // mudar este número força o Unity a reimportar todos os modelos
    public override uint GetVersion() { return 2; }

    void OnPostprocessMaterial(Material material)
    {
        if (!(assetImporter is ModelImporter)) return; // só materiais vindos de modelos

        if (material.HasProperty("_Color"))
            material.SetColor("_Color", material.GetColor("_Color").gamma); // linear -> sRGB
    }
}
