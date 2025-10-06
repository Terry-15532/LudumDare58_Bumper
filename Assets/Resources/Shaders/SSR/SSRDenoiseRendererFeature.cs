using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Serialization;

public class SSRDenoiseRendererFeature : ScriptableRendererFeature{
    static readonly int alphaId = Shader.PropertyToID("_Alpha");

    class SSRDenoiseRenderPass : ScriptableRenderPass{

        const string m_PassName = "SSR Denoise";
        ShaderTagId[] shaderTagIds;
        Material denoiseMaterial, blitMaterial;
        int denoiseIterations = 3;
        float alpha = 0.5f;

        class DenoisePassData{
            public RendererListHandle rendererList;
            public TextureHandle source;
            public TextureHandle output;
        }
        
        public void SetUp(Material denoiseMat, Material blitMat, ShaderTagId[] ids, int denoiseIterations, float alpha){
            denoiseMaterial = denoiseMat;
            blitMaterial = blitMat;
            shaderTagIds = ids;
            this.denoiseIterations = denoiseIterations;
            this.alpha = alpha;

        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData){
            var resourceData = frameData.Get<UniversalResourceData>();

            if (resourceData.isActiveTargetBackBuffer){
                Debug.LogWarning("Can't use the back buffer as input.");
                return;
            }

            var cameraData = frameData.Get<UniversalCameraData>();
            var renderingData = frameData.Get<UniversalRenderingData>();

            var source = resourceData.activeColorTexture;
            var destinationDesc = renderGraph.GetTextureDesc(source);
            destinationDesc.colorFormat = GraphicsFormat.R16G16B16A16_SFloat;
            destinationDesc.clearBuffer = true;
            destinationDesc.clearColor = Color.clear;
            destinationDesc.depthBufferBits = 0;
            // destinationDesc.enableRandomWrite = true;

            destinationDesc.name = "SSRDenoise_RenderTarget";
            TextureHandle renderTarget = renderGraph.CreateTexture(destinationDesc);
            destinationDesc.name = "SSRDenoise_Temp1";
            TextureHandle tempTexture1 = renderGraph.CreateTexture(destinationDesc);
            destinationDesc.name = "SSRDenoise_Temp2";
            TextureHandle tempTexture2 = renderGraph.CreateTexture(destinationDesc);

            using (var builder = renderGraph.AddRasterRenderPass<DenoisePassData>("Render SSR", out var passData)){

                RenderStateBlock renderStateBlock = new RenderStateBlock{
                    blendState = new BlendState{
                        blendState0 = new RenderTargetBlendState{
                            writeMask = ColorWriteMask.All,
                            sourceColorBlendMode = BlendMode.SrcAlpha,
                            destinationColorBlendMode = BlendMode.OneMinusSrcAlpha,
                            sourceAlphaBlendMode = BlendMode.SrcAlpha,
                            destinationAlphaBlendMode = BlendMode.OneMinusSrcAlpha,
                        }
                    }
                };
                var desc = new RendererListDesc(shaderTagIds, renderingData.cullResults, cameraData.camera){
                    rendererConfiguration = PerObjectData.None,
                    renderQueueRange = RenderQueueRange.all,
                    sortingCriteria = SortingCriteria.RenderQueue,
                    excludeObjectMotionVectors = true,
                    stateBlock = renderStateBlock
                    // layerMask = -1,
                };

                passData.rendererList = renderGraph.CreateRendererList(desc);

                builder.UseRendererList(passData.rendererList);
                builder.SetRenderAttachment(renderTarget, 0);

                builder.SetRenderFunc((DenoisePassData data, RasterGraphContext context) => {
                    context.cmd.ClearRenderTarget(true, true, Color.clear);
                    context.cmd.DrawRendererList(data.rendererList);
                });

            }


            if (denoiseMaterial && blitMaterial){
                int i = 0;
                RenderGraphUtils.BlitMaterialParameters paramA = new(renderTarget, tempTexture1, blitMaterial, 0);
                renderGraph.AddBlitPass(paramA, m_PassName + "_BlitToTemp");
                RenderGraphUtils.BlitMaterialParameters paramB = new(tempTexture1, tempTexture2, denoiseMaterial, 0);

                for (; i < denoiseIterations; i++){
                    renderGraph.AddBlitPass(paramB, m_PassName + "_" + i);

                    (tempTexture1, tempTexture2) = (tempTexture2, tempTexture1);
                }

                blitMaterial.SetFloat(alphaId, alpha);

                RenderGraphUtils.BlitMaterialParameters paramC = new(tempTexture1, resourceData.activeColorTexture, blitMaterial, 0);
                renderGraph.AddBlitPass(paramC, m_PassName + "_FinalBlit");

                resourceData.cameraColor = resourceData.activeColorTexture;

            }
            else{
                Debug.LogWarning("No material assigned.");
            }
        }

    }

    SSRDenoiseRenderPass m_SSRDenoise;

    public RenderPassEvent injectionPoint = RenderPassEvent.BeforeRenderingPostProcessing;
    public Material denoiseMaterial, blitMaterial;
    public int denoiseIterations = 3;

    [Range(0, 1)]
    public float alpha = 1;

    [FormerlySerializedAs("ShaderTags")]
    public string[] shaderTags;

    private ShaderTagId[] shaderTagIds;

    public override void Create(){
        m_SSRDenoise = new SSRDenoiseRenderPass();
        m_SSRDenoise.renderPassEvent = injectionPoint;
        shaderTagIds = new ShaderTagId[shaderTags.Length];
        int i = 0;
        foreach (var tag in shaderTags){
            shaderTagIds[i] = new ShaderTagId(tag);
            i++;
        }
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData){
        if ((renderingData.cameraData.cameraType == CameraType.Game || renderingData.cameraData.cameraType == CameraType.SceneView) && shaderTagIds?.Length > 0){
            m_SSRDenoise.SetUp(denoiseMaterial, blitMaterial, shaderTagIds, denoiseIterations, alpha);
            renderer.EnqueuePass(m_SSRDenoise);
        }
    }

}
