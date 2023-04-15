using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using NaughtyAttributes;

namespace Enter
{
  // from https://www.youtube.com/watch?v=-8xlPP4qgVo
  // this is also a good video on urp render feature: https://www.youtube.com/watch?v=MLl4yzaYMBY&t=185s
  public class BlitFeature : ScriptableRendererFeature
  {

    [System.Serializable]
    public class CustomPassSettings
    {
      public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
      [Tooltip(""), Required] public Material material;
    }

    [SerializeField] CustomPassSettings settings;
    public BlitPass customPass;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
#if UNITY_EDITOR
      if (renderingData.cameraData.isSceneViewCamera) return;
#endif
      renderer.EnqueuePass(customPass);
    }

    public override void Create()
    {
      customPass = new BlitPass(settings);
    }
  }

  public class BlitPass : ScriptableRenderPass
  {
    BlitFeature.CustomPassSettings settings;
    RenderTargetIdentifier colorBuffer, pixelBuffer;
    int pixelBufferID = Shader.PropertyToID("_PixelBuffer");

    Material material;
    Vector2Int pixelScreenDimension;

    public BlitPass(BlitFeature.CustomPassSettings settings)
    {
      this.settings = settings;
      this.renderPassEvent = settings.renderPassEvent;
      this.material = settings.material;
      if (!this.material) Debug.LogWarning("Pixelation Render Pass lacks a material. Currently it should do nothing.");
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
      CommandBuffer cmd = CommandBufferPool.Get(); // issuing GPU commands go through the command buffer i think
      if (material)
      {
        // no idea what this is doing
        using (new ProfilingScope(cmd, new ProfilingSampler("Pixelize Pass")))
        {
          // blit just applies this buffer on the screen, pretty sure
          Blit(cmd, colorBuffer, pixelBuffer, material);
          Blit(cmd, pixelBuffer, colorBuffer);
        }
      }

      context.ExecuteCommandBuffer(cmd);
      CommandBufferPool.Release(cmd); // important to release this back to the pool
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
      colorBuffer = renderingData.cameraData.renderer.cameraColorTarget; // where we'll write to
      RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;

      pixelScreenDimension.x = renderingData.cameraData.camera.pixelWidth; // round up so you won't get black edge
      pixelScreenDimension.y = renderingData.cameraData.camera.pixelHeight;
	  Vector2 screenSize = renderingData.cameraData.camera.ViewportToScreenPoint(Vector2.one);

      // provide material with what it needs
      if (material)
      {
        material.SetVector("_PixelSz", new Vector4(pixelScreenDimension.x, pixelScreenDimension.y, 1.0f / pixelScreenDimension.x, 1.0f / pixelScreenDimension.y));
        material.SetVector("_ScreenSz", new Vector4(screenSize.x, screenSize.y, 1/screenSize.x, 1/screenSize.y));
      }

      descriptor.width = pixelScreenDimension.x;
      descriptor.height = pixelScreenDimension.y;

      // create new (or get old) render texture
      cmd.GetTemporaryRT(pixelBufferID, descriptor, FilterMode.Bilinear);
      pixelBuffer = new RenderTargetIdentifier(pixelBufferID);
    }

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
      // release the render texture we use
      cmd.ReleaseTemporaryRT(pixelBufferID);
    }
  }
}
