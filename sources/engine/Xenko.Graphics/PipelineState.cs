// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using Xenko.Core.Extensions;
using Xenko.Core.ReferenceCounting;
using Xenko.Shaders;

namespace Xenko.Graphics
{
    public class PipelineStateDescription : IEquatable<PipelineStateDescription>
    {
        // Root Signature
        public RootSignature RootSignature;

        // Effect/Shader
        public EffectBytecode EffectBytecode;

        // Rendering States
        public BlendStateDescription BlendState;
        public uint SampleMask = 0xFFFFFFFF;
        public RasterizerStateDescription RasterizerState;
        public DepthStencilStateDescription DepthStencilState;

        // Input layout
        public InputElementDescription[] InputElements;

        public PrimitiveType PrimitiveType;

        public RenderOutputDescription Output;

        public unsafe PipelineStateDescription Clone()
        {
            InputElementDescription[] inputElements;
            if (InputElements != null)
            {
                inputElements = new InputElementDescription[InputElements.Length];
                for (int i = 0; i < inputElements.Length; ++i)
                    inputElements[i] = InputElements[i];
            }
            else
            {
                inputElements = null;
            }

            return new PipelineStateDescription
            {
                RootSignature = RootSignature,
                EffectBytecode = EffectBytecode,
                BlendState = BlendState,
                SampleMask = SampleMask,
                RasterizerState = RasterizerState,
                DepthStencilState = DepthStencilState,

                InputElements = inputElements,

                PrimitiveType = PrimitiveType,

                Output = Output,
            };
        }

        public void SetDefaults()
        {
            BlendState.SetDefaults();
            RasterizerState.SetDefault();
            DepthStencilState.SetDefault();
        }

        public bool Equals(PipelineStateDescription other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            if (!(RootSignature == other.RootSignature
                && EffectBytecode == other.EffectBytecode
                && BlendState.Equals(other.BlendState)
                && SampleMask == other.SampleMask
                && RasterizerState.Equals(other.RasterizerState)
                && DepthStencilState.Equals(other.DepthStencilState)
                && PrimitiveType == other.PrimitiveType
                && Output == other.Output))
                return false;

            if ((InputElements != null) != (other.InputElements != null))
                return false;
            if (InputElements != null)
            {
                for (int i = 0; i < InputElements.Length; ++i)
                {
                    if (!InputElements[i].Equals(other.InputElements[i]))
                        return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PipelineStateDescription)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = RootSignature != null ? RootSignature.GetHashCode() : 0;
                hashCode = (hashCode*397) ^ (EffectBytecode != null ? EffectBytecode.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ BlendState.GetHashCode();
                hashCode = (hashCode*397) ^ (int)SampleMask;
                hashCode = (hashCode*397) ^ RasterizerState.GetHashCode();
                hashCode = (hashCode*397) ^ DepthStencilState.GetHashCode();
                if (InputElements != null)
                    foreach (var inputElement in InputElements)
                        hashCode = (hashCode*397) ^ inputElement.GetHashCode();
                hashCode = (hashCode*397) ^ (int)PrimitiveType;
                hashCode = (hashCode*397) ^ Output.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(PipelineStateDescription left, PipelineStateDescription right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(PipelineStateDescription left, PipelineStateDescription right)
        {
            return !Equals(left, right);
        }
    }

    public partial class PipelineState : GraphicsResourceBase
    {
        public int InputBindingCount { get; private set; }

        public static PipelineState New(GraphicsDevice graphicsDevice, ref PipelineStateDescription pipelineStateDescription)
        {
            // Hash the current state
            var hashedState = new PipelineStateDescriptionWithHash(pipelineStateDescription);

            // Store SamplerState in a cache (D3D seems to have quite bad concurrency when using CreateSampler while rendering)
            PipelineState pipelineState;
            lock (graphicsDevice.CachedPipelineStates)
            {
                if (graphicsDevice.CachedPipelineStates.TryGetValue(hashedState, out pipelineState))
                {
                    // TODO: Appropriate destroy
                    pipelineState.AddReferenceInternal();
                }
                else
                {
                    pipelineState = new PipelineState(graphicsDevice, pipelineStateDescription);
                    graphicsDevice.CachedPipelineStates.Add(hashedState, pipelineState);
                }
            }
            return pipelineState;
        }
    }
}
