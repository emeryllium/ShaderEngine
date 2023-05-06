using UnityEngine;

namespace ShaderEngine;

public class ShaderEngine_CameraBehavior : MonoBehaviour
{
    public void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        RenderTexture end = src;
        foreach (Material mat in ShaderEngine.materials.Keys)
        {
            if (mat == null || !ShaderEngine.materials[mat]) continue;
            Graphics.Blit(end, end, mat);
        }
        Graphics.Blit(end, dst);
    }
}