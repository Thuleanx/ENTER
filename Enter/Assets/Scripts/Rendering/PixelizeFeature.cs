using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Enter
{
  // from https://www.youtube.com/watch?v=-8xlPP4qgVo
  // this is also a good video on urp render feature: https://www.youtube.com/watch?v=MLl4yzaYMBY&t=185s
  public class PixelizeFeature : ScriptableRendererFeature
  {

    [System.Serializable]
    public class CustomPassSettings
    {
      public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
	  public int pixelPerUnit = 16;
      [Tooltip("")]
      public Material material;
    }

    [SerializeField] CustomPassSettings settings;
    public PixelizePass customPass;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
#if UNITY_EDITOR
      if (renderingData.cameraData.isSceneViewCamera) return;
#endif
      renderer.EnqueuePass(customPass);
    }

    public override void Create()
    {
      customPass = new PixelizePass(settings);
    }
  }

  public class PixelizePass : ScriptableRenderPass
  {
    PixelizeFeature.CustomPassSettings settings;
    RenderTargetIdentifier colorBuffer, pixelBuffer;
    int pixelBufferID = Shader.PropertyToID("_PixelBuffer");

    Material material;
    Vector2Int pixelScreenDimension;

    public PixelizePass(PixelizeFeature.CustomPassSettings settings)
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
			Debug.Log("HI");
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

		float height = renderingData.cameraData.camera.orthographicSize * settings.pixelPerUnit * 2;
		float width = height * renderingData.cameraData.camera.aspect + 0.5f;

      pixelScreenDimension.x = (int) width; // round up so you won't get black edge
      pixelScreenDimension.y = (int) height;
    //   pixelScreenDimension.x = (int) 256; // round up so you won't get black edge
    //   pixelScreenDimension.y = (int) 144;

      // provide material with what it needs
      if (material)
      {
        material.SetVector("_PixelCnt", (Vector2)pixelScreenDimension);
        material.SetVector("_PixelSz", new Vector2(1.0f / pixelScreenDimension.x, 1.0f / pixelScreenDimension.y));
        material.SetVector("_HalfPixelSz", new Vector2(0.5f / pixelScreenDimension.x, 0.5f / pixelScreenDimension.y));
      }

      descriptor.width = pixelScreenDimension.x;
      descriptor.height = pixelScreenDimension.y;

	  Debug.Log(descriptor.width + " " + descriptor.height);

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