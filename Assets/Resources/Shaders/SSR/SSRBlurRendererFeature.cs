// using System;
// using UnityEngine;
// using UnityEngine.Rendering;
// using UnityEngine.Rendering.RendererUtils;
// using UnityEngine.Rendering.Universal;
// public class SSRBlurRendererFeature : ScriptableRendererFeature
// {
//     class CustomRenderPass : ScriptableRenderPass
//     {
//         private readonly ShaderTagId shaderTagId;
//         private RTHandle targetRT, tempRT;
//         private RenderTextureDescriptor descriptor;
//         private readonly int blurIterations;
//         private readonly float blurSize, alphaVarianceThreshold, depthVarianceThreshold;
//
//
//         private static readonly int flipYID = Shader.PropertyToID("_FlipY"),
//             blurSizeID = Shader.PropertyToID("_BlurSize"),
//             alphaThresholdID = Shader.PropertyToID("_AlphaThreshold"),
//             depthThresholdID = Shader.PropertyToID("_DepthThreshold");
//
//         private readonly Material blitMat, denoiseMat;
//
//         public CustomRenderPass(string shaderTag, int iterations, float blurSize, float alphaThreshold, float depthThreshold, Shader flipShader, Shader denoiseShader)
//         {
//             shaderTagId = new ShaderTagId(shaderTag);
//             blurIterations = iterations;
//             blitMat = new Material(flipShader);
//             denoiseMat = new Material(denoiseShader);
//             this.blurSize = blurSize;
//             alphaVarianceThreshold = alphaThreshold;
//             this.depthVarianceThreshold = depthThreshold;
//         }
//
//         public override void Configure(CommandBuffer cmd,
//             RenderTextureDescriptor cameraTextureDescriptor)
//         {
//             try
//             {
//                 descriptor = new RenderTextureDescriptor(cameraTextureDescriptor.width, cameraTextureDescriptor.height, RenderTextureFormat.ARGBHalf, 0);
//                 RenderingUtils.ReAllocateIfNeeded(ref targetRT, descriptor, name: "_RenderTarget");
//                 RenderingUtils.ReAllocateIfNeeded(ref tempRT, descriptor, name: "_TempRT");
//                 ConfigureTarget(targetRT);
//                 ConfigureClear(ClearFlag.All, Color.clear);
//             }
//             catch (Exception e)
//             {
//                 Debug.Log(e.Message);
//             }
//         }
//
//         public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
//         {
//             try
//             {
//                 CommandBuffer cmd = CommandBufferPool.Get("SSR Blur");
//                 RTHandle cameraColorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
//                 int blurNum = blurIterations;
//
//                 RenderStateBlock renderStateBlock = new()
//                 {
//                     blendState = new BlendState
//                     {
//                         blendState0 = new RenderTargetBlendState
//                         {
//                             writeMask = ColorWriteMask.All,
//                             sourceColorBlendMode = BlendMode.SrcAlpha,
//                             destinationColorBlendMode = BlendMode.OneMinusSrcAlpha,
//                             sourceAlphaBlendMode = BlendMode.SrcAlpha,
//                             destinationAlphaBlendMode = BlendMode.OneMinusSrcAlpha,
//                         }
//                     }
//                 };
//
//                 //创建rendererListDescription，指定要绘制的物体（shaderTag为指定tag的物体）
//                 var rendererListDesc = new RendererListDesc(shaderTagId, renderingData.cullResults, renderingData.cameraData.camera)
//                 {
//                     rendererConfiguration = PerObjectData.None,
//                     renderQueueRange = RenderQueueRange.all,
//                     sortingCriteria = SortingCriteria.RenderQueue,
//                     excludeObjectMotionVectors = true,
//                     layerMask = -1,
//                     stateBlock = renderStateBlock
//                 };
//
//                 var rendererList = context.CreateRendererList(rendererListDesc);
//                 cmd.DrawRendererList(rendererList); //绘制shaderTag为指定tag的物体
//
//                 //降噪
//                 if (blurNum > 0)
//                 {
//                     denoiseMat.SetFloat(alphaThresholdID, alphaVarianceThreshold);
//                     denoiseMat.SetFloat(depthThresholdID, depthVarianceThreshold);
//                     denoiseMat.SetFloat(blurSizeID, blurSize);
//                     for (int i = 0; i < blurNum; i++)
//                     {
//                         if (i % 2 == 0)
//                         {
//                             cmd.Blit(targetRT, tempRT, denoiseMat);
//                         }
//                         else
//                         {
//                             cmd.Blit(tempRT, targetRT, denoiseMat);
//                         }
//                     }
//
//                     cmd.Blit(blurNum % 2 == 0 ? targetRT : tempRT, cameraColorTarget, blitMat);
//                 }
//                 else
//                 {
//                     cmd.Blit(targetRT, cameraColorTarget, blitMat);
//                 }
//
//                 context.ExecuteCommandBuffer(cmd);
//                 cmd.Clear();
//
//                 CommandBufferPool.Release(cmd);
//             }
//             catch (Exception e)
//             {
//                 Debug.Log(e.Message);
//             }
//         }
//
//         public override void OnCameraCleanup(CommandBuffer cmd)
//         {
//             targetRT?.Release();
//             tempRT?.Release();
//         }
//     }
//
//     CustomRenderPass m_ScriptablePass;
//
//     public string shaderTag = "SSRBase";
//     public int blurIterations = 3;
//     public float blurSize = 1;
//     public float alphaVarianceThreshold = 0.3f, depthVarianceThreshold = 0.4f;
//     public RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingOpaques;
//     public bool activated;
//     public Shader flipShader, denoiseShader;
//
//
//     public override void Create()
//     {
//         if (activated)
//         {
//             m_ScriptablePass = new CustomRenderPass(shaderTag, blurIterations, blurSize, alphaVarianceThreshold, depthVarianceThreshold, flipShader, denoiseShader)
//             {
//                 renderPassEvent = injectionPoint
//             };
//         }
//     }
//
//     public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
//     {
//         if (activated && renderingData.cameraData.cameraType == CameraType.Game)
//         {
//             renderer.EnqueuePass(m_ScriptablePass);
//         }
//     }
// }
//
// // using UnityEngine;
// // using UnityEngine.Rendering;
// // using UnityEngine.Rendering.RenderGraphModule;
// // using UnityEngine.Rendering.Universal;
// //
// // public class SSRBlurRendererFeature : ScriptableRendererFeature
// // {
// //     // 自定义上下文容器
// //     class SSRBlurContext : ContextContainer
// //     {
// //         public TextureHandle sourceTexture;
// //         public TextureHandle targetTexture;
// //         public TextureHandle tempTexture;
// //         public Material blitMaterial;
// //         public Material denoiseMaterial;
// //         public int blurIterations;
// //         public float blurSize;
// //         public float alphaThreshold;
// //         public float depthThreshold;
// //     }
// //
// //     class SSRBlurPass : ScriptableRenderPass
// //     {
// //         const string ProfilerTag = "SSR Blur Pass";
// //         readonly ShaderTagId m_ShaderTagId;
// //         readonly Material m_BlitMat;
// //         readonly Material m_DenoiseMat;
// //         readonly int m_BlurIterations;
// //         readonly float m_BlurSize;
// //         readonly float m_AlphaThreshold;
// //         readonly float m_DepthThreshold;
// //
// //         public SSRBlurPass(
// //             string shaderTag, 
// //             Material blitMat, 
// //             Material denoiseMat,
// //             int iterations, 
// //             float blurSize,
// //             float alphaThreshold,
// //             float depthThreshold)
// //         {
// //             m_ShaderTagId = new ShaderTagId(shaderTag);
// //             m_BlitMat = blitMat;
// //             m_DenoiseMat = denoiseMat;
// //             m_BlurIterations = iterations;
// //             m_BlurSize = blurSize;
// //             m_AlphaThreshold = alphaThreshold;
// //             m_DepthThreshold = depthThreshold;
// //         }
// //
// //         public override void RecordRenderGraph(
// //             RenderGraph renderGraph, 
// //             ContextContainer contextContainer,
// //             ref RenderingData renderingData)
// //         {
// //             if (renderingData.cameraData.cameraType != CameraType.Game)
// //                 return;
// //
// //             // 获取或创建自定义上下文
// //             var ssrContext = contextContainer.GetOrCreate<SSRBlurContext>();
// //             
// //             // 初始化上下文数据
// //             InitializeContext(renderGraph, ref renderingData, ssrContext);
// //
// //             using (var builder = renderGraph.AddRasterRenderPass<PassData>(
// //                 ProfilerTag, 
// //                 out var passData))
// //             {
// //                 ConfigurePassData(ssrContext, passData);
// //                 ConfigureResourceDependencies(builder, ssrContext);
// //
// //                 builder.AllowPassCulling(false);
// //
// //                 builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
// //                 {
// //                     ExecuteRenderPass(context, data);
// //                 });
// //             }
// //         }
// //
// //         void InitializeContext(
// //             RenderGraph renderGraph,
// //             ref RenderingData renderingData,
// //             SSRBlurContext context)
// //         {
// //             // 获取相机颜色目标
// //             var cameraColorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
// //             context.sourceTexture = renderGraph.ImportTexture(cameraColorTarget);
// //
// //             // 创建中间纹理
// //             var desc = renderingData.cameraData.cameraTargetDescriptor;
// //             desc.colorFormat = RenderTextureFormat.ARGBHalf;
// //             desc.depthBufferBits = 0;
// //
// //             context.targetTexture = UniversalRenderer.CreateRenderGraphTexture(
// //                 renderGraph, desc, "_SSRBlurTarget", true);
// //             context.tempTexture = UniversalRenderer.CreateRenderGraphTexture(
// //                 renderGraph, desc, "_SSRBlurTemp", true);
// //
// //             // 传递材质参数
// //             context.blitMaterial = m_BlitMat;
// //             context.denoiseMaterial = m_DenoiseMat;
// //             context.blurIterations = m_BlurIterations;
// //             context.blurSize = m_BlurSize;
// //             context.alphaThreshold = m_AlphaThreshold;
// //             context.depthThreshold = m_DepthThreshold;
// //         }
// //
// //         void ConfigurePassData(SSRBlurContext context, PassData passData)
// //         {
// //             passData.source = context.sourceTexture;
// //             passData.targetRT = context.targetTexture;
// //             passData.tempRT = context.tempTexture;
// //             passData.blitMat = context.blitMaterial;
// //             passData.denoiseMat = context.denoiseMaterial;
// //             passData.blurIterations = context.blurIterations;
// //             passData.blurSize = context.blurSize;
// //             passData.alphaThreshold = context.alphaThreshold;
// //             passData.depthThreshold = context.depthThreshold;
// //         }
// //
// //         void ConfigureResourceDependencies(
// //             RasterRenderPassBuilder builder, 
// //             SSRBlurContext context)
// //         {
// //             builder.UseTexture(context.sourceTexture, AccessFlags.Read);
// //             builder.UseTexture(context.targetTexture, AccessFlags.Write);
// //             builder.UseTexture(context.tempTexture, AccessFlags.Write);
// //         }
// //
// //         void ExecuteRenderPass(RasterGraphContext context, PassData data)
// //         {
// //             // 绘制指定 ShaderTag 的物体
// //             var renderParams = new RendererListParams(
// //                 context.cullResults,
// //                 data.source.rt.width,
// //                 data.source.rt.height,
// //                 data.source.rt.graphicsFormat,
// //                 1,
// //                 context.renderingData.cameraData.camera,
// //                 m_ShaderTagId,
// //                 RenderQueueRange.all,
// //                 SortingCriteria.RenderQueue);
// //
// //             renderParams.excludeObjectMotionVectors = true;
// //             var rendererList = context.renderContext.CreateRendererList(ref renderParams);
// //             context.cmd.DrawRendererList(rendererList);
// //
// //             // 执行降噪模糊
// //             if (data.blurIterations > 0)
// //             {
// //                 data.denoiseMat.SetFloat("_AlphaThreshold", data.alphaThreshold);
// //                 data.denoiseMat.SetFloat("_DepthThreshold", data.depthThreshold);
// //                 data.denoiseMat.SetFloat("_BlurSize", data.blurSize);
// //
// //                 TextureHandle currentSrc = data.targetRT;
// //                 TextureHandle currentDst = data.tempRT;
// //
// //                 for (int i = 0; i < data.blurIterations; i++)
// //                 {
// //                     Blitter.BlitTexture(context.cmd, currentSrc, new Vector4(1, 1, 0, 0), data.denoiseMat, 0);
// //
// //                     // 交换纹理
// //                     var temp = currentSrc;
// //                     currentSrc = currentDst;
// //                     currentDst = temp;
// //                 }
// //
// //                 // 最终 Blit 到相机颜色
// //                 Blitter.BlitTexture(context.cmd, (data.blurIterations % 2 == 0) ? data.targetRT : data.tempRT, new Vector4(1, 1, 0, 0), data.blitMat, 0);
// //             }
// //             else
// //             {
// //                 Blitter.BlitTexture(context.cmd, data.targetRT, new Vector4(1, 1, 0, 0), data.blitMat, 0);
// //             }
// //         }
// //
// //         class PassData
// //         {
// //             public TextureHandle source;
// //             public TextureHandle targetRT;
// //             public TextureHandle tempRT;
// //             public Material blitMat;
// //             public Material denoiseMat;
// //             public int blurIterations;
// //             public float blurSize;
// //             public float alphaThreshold;
// //             public float depthThreshold;
// //         }
// //     }
// //
// //     [System.Serializable]
// //     public class Settings
// //     {
// //         public string shaderTag = "SSRBase";
// //         public int blurIterations = 3;
// //         public float blurSize = 1f;
// //         public float alphaVarianceThreshold = 0.3f;
// //         public float depthVarianceThreshold = 0.4f;
// //         public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
// //         public bool activated = true;
// //         public Material flipMaterial;
// //         public Material denoiseMaterial;
// //     }
// //
// //     public Settings settings = new Settings();
// //     private SSRBlurPass m_SSRBlurPass;
// //
// //     public override void Create()
// //     {
// //         if (settings.activated && 
// //             settings.flipMaterial != null && 
// //             settings.denoiseMaterial != null)
// //         {
// //             m_SSRBlurPass = new SSRBlurPass(
// //                 settings.shaderTag,
// //                 settings.flipMaterial,
// //                 settings.denoiseMaterial,
// //                 settings.blurIterations,
// //                 settings.blurSize,
// //                 settings.alphaVarianceThreshold,
// //                 settings.depthVarianceThreshold)
// //             {
// //                 renderPassEvent = settings.renderPassEvent
// //             };
// //         }
// //     }
// //
// //     public override void AddRenderPasses(
// //         ScriptableRenderer renderer, 
// //         ref RenderingData renderingData)
// //     {
// //         if (settings.activated)
// //         {
// //             renderer.EnqueuePass(m_SSRBlurPass);
// //         }
// //     }
// //
// //     protected override void Dispose(bool disposing)
// //     {
// //         base.Dispose(disposing);
// //         m_SSRBlurPass?.Dispose();
// //     }
// // }
// // using UnityEngine;
// // using UnityEngine.Rendering;
// // using UnityEngine.Rendering.RendererUtils;
// // using UnityEngine.Rendering.RenderGraphModule;
// // using UnityEngine.Rendering.RenderGraphModule.Util;
// // using UnityEngine.Rendering.Universal;
// //
// // public class SSRBlurRendererFeature : ScriptableRendererFeature
// // {
// //     class SSRBlurPass : ScriptableRenderPass
// //     {
// //         class PassData
// //         {
// //             public Material blitMat;
// //             public Material denoiseMat;
// //             public TextureHandle source;
// //             public TextureHandle tempRT;
// //             public TextureHandle targetRT;
// //             public int blurIterations;
// //             public float blurSize;
// //             public float alphaThreshold;
// //             public float depthThreshold;
// //         }
// //
// //         private readonly string m_ProfilerTag;
// //         private readonly ShaderTagId m_ShaderTagId;
// //         private Material m_BlitMat;
// //         private Material m_DenoiseMat;
// //         private int m_BlurIterations;
// //         private float m_BlurSize;
// //         private float m_AlphaThreshold;
// //         private float m_DepthThreshold;
// //
// //         public SSRBlurPass(string profilerTag, string shaderTag, 
// //             Material blitMat, Material denoiseMat,
// //             int iterations, float blurSize, 
// //             float alphaThreshold, float depthThreshold)
// //         {
// //             m_ProfilerTag = profilerTag;
// //             m_ShaderTagId = new ShaderTagId(shaderTag);
// //             m_BlitMat = blitMat;
// //             m_DenoiseMat = denoiseMat;
// //             m_BlurIterations = iterations;
// //             m_BlurSize = blurSize;
// //             m_AlphaThreshold = alphaThreshold;
// //             m_DepthThreshold = depthThreshold;
// //         }
// //
// //         public override void RecordRenderGraph(RenderGraph renderGraph, 
// //             ContextContainer frameResources)
// //         {
// //             var cameraData = frameResources.Get<UniversalCameraData>();
// //             if (cameraData.cameraType != CameraType.Game)
// //                 return;
// //
// //             using (var builder = renderGraph.AddRasterRenderPass<PassData>(
// //                 m_ProfilerTag, out var passData))
// //             {
// //                 // 创建中间纹理
// //                 var desc = cameraData.cameraTargetDescriptor;
// //                 desc.colorFormat = RenderTextureFormat.ARGBHalf;
// //                 desc.depthBufferBits = 0;
// //
// //                 passData.targetRT = UniversalRenderer.CreateRenderGraphTexture(
// //                     renderGraph, desc, "_SSRBlurTarget", true);
// //                 passData.tempRT = UniversalRenderer.CreateRenderGraphTexture(
// //                     renderGraph, desc, "_SSRBlurTemp", true);
// //                 
// //                 passData.blitMat = m_BlitMat;
// //                 passData.denoiseMat = m_DenoiseMat;
// //                 passData.blurIterations = m_BlurIterations;
// //                 passData.blurSize = m_BlurSize;
// //                 passData.alphaThreshold = m_AlphaThreshold;
// //                 passData.depthThreshold = m_DepthThreshold;
// //
// //                 // 获取活动颜色纹理
// //                 UniversalResourceData resourceData = frameResources.Get<UniversalResourceData>();
// //                 passData.source = resourceData.activeColorTexture;
// //
// //                 // 设置纹理读写依赖
// //                 builder.UseTexture(passData.targetRT, AccessFlags.Write);
// //                 builder.UseTexture(passData.tempRT, AccessFlags.Write);
// //                 builder.UseTexture(passData.source, AccessFlags.Read);
// //
// //                 builder.AllowPassCulling(false);
// //
// //                 builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
// //                 {
// //                     // 绘制指定ShaderTag的物体到目标RT
// //                     var renderParams = new RendererListDesc(m_ShaderTagId, frameResources.Get<UniversalRenderingData>().cullResults, cameraData.camera);
// //
// //                     renderParams.excludeObjectMotionVectors = true;
// //                     var rendererList = frameResources.Get<UniversalRenderingData>().(ref renderParams);
// //                     context.cmd.DrawRendererList(rendererList);
// //
// //                     // 执行降噪模糊
// //                     if (data.blurIterations > 0)
// //                     {
// //                         data.denoiseMat.SetFloat("_AlphaThreshold", data.alphaThreshold);
// //                         data.denoiseMat.SetFloat("_DepthThreshold", data.depthThreshold);
// //                         data.denoiseMat.SetFloat("_BlurSize", data.blurSize);
// //
// //                         TextureHandle currentSrc = data.targetRT;
// //                         TextureHandle currentDst = data.tempRT;
// //
// //                         for (int i = 0; i < data.blurIterations; i++)
// //                         {
// //                             Blitter.BlitTexture(
// //                                 context.cmd,
// //                                 currentSrc,
// //                                 new Vector4(1, 1, 0, 0),
// //                                 data.denoiseMat,
// //                                 0);
// //
// //                             // 交换纹理
// //                             var temp = currentSrc;
// //                             currentSrc = currentDst;
// //                             currentDst = temp;
// //                         }
// //
// //                         // 最终Blit到相机颜色
// //                         Blitter.BlitTexture(
// //                             context.cmd,
// //                             (data.blurIterations % 2 == 0) ? data.targetRT : data.tempRT,
// //                             new Vector4(1, 1, 0, 0),
// //                             data.blitMat,
// //                             0);
// //                     }
// //                     else
// //                     {
// //                         Blitter.BlitTexture(
// //                             context.cmd,
// //                             data.targetRT,
// //                             new Vector4(1, 1, 0, 0),
// //                             data.blitMat,
// //                             0);
// //                     }
// //                 });
// //             }
// //         }
// //     }
// //
// //     [System.Serializable]
// //     public class Settings
// //     {
// //         public string shaderTag = "SSRBase";
// //         public int blurIterations = 3;
// //         public float blurSize = 1f;
// //         public float alphaVarianceThreshold = 0.3f;
// //         public float depthVarianceThreshold = 0.4f;
// //         public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
// //         public bool activated = true;
// //         public Material flipMaterial;
// //         public Material denoiseMaterial;
// //     }
// //
// //     public Settings settings = new Settings();
// //
// //     private SSRBlurPass m_SSRBlurPass;
// //
// //     public override void Create()
// //     {
// //         if (settings.activated && settings.flipMaterial != null && settings.denoiseMaterial != null)
// //         {
// //             m_SSRBlurPass = new SSRBlurPass(
// //                 "SSR Blur Pass",
// //                 settings.shaderTag,
// //                 settings.flipMaterial,
// //                 settings.denoiseMaterial,
// //                 settings.blurIterations,
// //                 settings.blurSize,
// //                 settings.alphaVarianceThreshold,
// //                 settings.depthVarianceThreshold)
// //             {
// //                 renderPassEvent = settings.renderPassEvent
// //             };
// //         }
// //     }
// //
// //     public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
// //     {
// //         if (settings.activated)
// //         {
// //             renderer.EnqueuePass(m_SSRBlurPass);
// //         }
// //     }
// //
// //     protected override void Dispose(bool disposing)
// //     {
// //         base.Dispose(disposing);
// //         m_SSRBlurPass?.Dispose();
// //     }
// // }
