using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Enter
{
  public class MaskBlurRenderFeature: ScriptableRendererFeature
  {

    [System.Serializable]
    public class CustomPassSettings
    {
      public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
      [Tooltip("blur material")] public Material material;
    }

    [SerializeField] CustomPassSettings settings;
    public MaskBlurPass customPass;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
#if UNITY_EDITOR
      if (renderingData.cameraData.isSceneViewCamera) return;
#endif
      renderer.EnqueuePass(customPass);
    }

    public override void Create()
    {
      customPass = new MaskBlurPass(settings);
    }
  }

  public class MaskBlurPass : ScriptableRenderPass
  {
    MaskBlurRenderFeature.CustomPassSettings settings;
    RenderTargetIdentifier colorBuffer, pixelBuffer;
    int pixelBufferID = Shader.PropertyToID("_PixelBuffer");

    Material material;
    Vector2Int pixelScreenDimension;

    public MaskBlurPass(MaskBlurRenderFeature.CustomPassSettings settings)
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
        using (new ProfilingScope(cmd, new ProfilingSampler("MaskBlur Pass")))
        {
          // blit just applies this buffer on the screen, pretty sure
          // vertical blur
          Blit(cmd, colorBuffer, pixelBuffer, material, 0);
          Blit(cmd, pixelBuffer, colorBuffer, material, 1);
        }
      }

      context.ExecuteCommandBuffer(cmd);
      CommandBufferPool.Release(cmd); // important to release this back to the pool
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
      colorBuffer = renderingData.cameraData.renderer.cameraColorTarget; // where we'll write to
      RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;

      pixelScreenDimension.x = (int) renderingData.cameraData.camera.pixelWidth; // round up so you won't get black edge
      pixelScreenDimension.y = (int) renderingData.cameraData.camera.pixelHeight;

      descriptor.width = pixelScreenDimension.x;
      descriptor.height = pixelScreenDimension.y;

      // create new (or get old) render texture
      cmd.GetTemporaryRT(pixelBufferID, descriptor, FilterMode.Point);
      pixelBuffer = new RenderTargetIdentifier(pixelBufferID);
    }

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
      // release the render texture we use
      cmd.ReleaseTemporaryRT(pixelBufferID);
    }
  }
}
