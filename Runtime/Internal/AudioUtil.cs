using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cognitive3D.Components
{
    internal class AudioUtil
    {
        /// <summary>
        /// Convert stereo audio data to mono by averaging channels
        /// </summary>
        internal static float[] ConvertStereoToMono(float[] stereoData)
        {
            if (stereoData == null || stereoData.Length == 0)
                return new float[0];
            
            float[] monoData = new float[stereoData.Length / 2];
            for (int i = 0; i < monoData.Length; i++)
            {
                monoData[i] = (stereoData[i * 2] + stereoData[i * 2 + 1]) * 0.5f;
            }
            return monoData;
        }
        
        /// <summary>
        /// Convert float audio samples to 16-bit PCM bytes
        /// </summary>
        internal static int ConvertToPCM16(float[] samples, int offset, int count, byte[] buffer)
        {
            int rescale = 32767;
            int bufferIndex = 0;
            
            for (int s = offset; s < offset + count && s < samples.Length; s++)
            {
                short val = (short)(Mathf.Clamp(samples[s], -1f, 1f) * rescale);
                buffer[bufferIndex++] = (byte)(val & 0xFF);
                buffer[bufferIndex++] = (byte)((val >> 8) & 0xFF);
            }
            
            return bufferIndex;
        }
    }
}