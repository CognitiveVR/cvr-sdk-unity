//	Copyright (c) 2012 Calvin Rien
//        http://the.darktable.com
//
//	This software is provided 'as-is', without any express or implied warranty. In
//	no event will the authors be held liable for any damages arising from the use
//	of this software.
//
//	Permission is granted to anyone to use this software for any purpose,
//	including commercial applications, and to alter it and redistribute it freely,
//	subject to the following restrictions:
//
//	1. The origin of this software must not be misrepresented; you must not claim
//	that you wrote the original software. If you use this software in a product,
//	an acknowledgment in the product documentation would be appreciated but is not
//	required.
//
//	2. Altered source versions must be plainly marked as such, and must not be
//	misrepresented as being the original software.
//
//	3. This notice may not be removed or altered from any source distribution.
//
//  =============================================================================
//
//  derived from Gregorio Zanon's script
//  http://forum.unity3d.com/threads/119295-Writing-AudioListener.GetOutputData-to-wav-problem?p=806734&viewfull=1#post806734

//This version has been altered to put the wav into bytes instead of writing to disk

using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;

namespace CognitiveVR
{
    public static class MicrophoneUtility
    {
        //const int HEADER_SIZE = 44;

        public static void Save(AudioClip clip, out byte[] fileBytes)
        {
            byte[] data;
            byte[] headers;

            if (Microphone.devices.Length == 0)
            {
                fileBytes = new byte[0];
                return;
            }

            //TODO create thread for writing audioclip to byte[]
            //http://stackoverflow.com/questions/19048492/notify-when-thread-is-complete-without-locking-calling-thread
            data = ConvertAndWrite(clip);
            headers = WriteHeader(clip, data);

            fileBytes = new byte[data.Length + headers.Length];
            headers.CopyTo(fileBytes, 0);
            data.CopyTo(fileBytes, headers.Length);
        }

        /*static FileStream CreateEmpty(string filepath)
        {
            var fileStream = new FileStream(filepath, FileMode.Create);
            byte emptyByte = new byte();

            for (int i = 0; i < HEADER_SIZE; i++) //preparing the header
            {
                fileStream.WriteByte(emptyByte);
            }

            return fileStream;
        }*/

        static byte[] ConvertAndWrite(AudioClip clip)
        {
            if (clip == null)
            {
                return new byte[0];
            }
            var samples = new float[clip.samples];

            clip.GetData(samples, 0);

            short[] intData = new short[samples.Length];
            //converting in 2 float[] steps to Int16[], //then Int16[] to Byte[]

            byte[] bytesData = new byte[samples.Length * 2];
            //bytesData array is twice the size of
            //dataSource array because a float converted in Int16 is 2 bytes.

            int rescaleFactor = 32767; //to convert float to Int16

            for (int i = 0; i < samples.Length; i++)
            {
                intData[i] = (short)(samples[i] * rescaleFactor);
                byte[] byteArr = new byte[2];
                byteArr = BitConverter.GetBytes(intData[i]);
                byteArr.CopyTo(bytesData, i * 2);
            }
            return bytesData;
        }

        static byte[] WriteHeader(AudioClip clip, byte[] data)
        {
            if (clip == null)
            {
                return new byte[0];
            }
            //could alternatively do this with a memorystream
            List<byte> returnBytes = new List<byte>();

            var hz = clip.frequency;
            var channels = clip.channels;
            var samples = clip.samples;

            byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF"); //4
            returnBytes.AddRange(riff);

            byte[] chunkSize = BitConverter.GetBytes(data.Length - 8); //4
            returnBytes.AddRange(chunkSize);

            byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE"); //4
            returnBytes.AddRange(wave);

            byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt "); //4
            returnBytes.AddRange(fmt);

            byte[] subChunk1 = BitConverter.GetBytes(16); //4
            returnBytes.AddRange(subChunk1);

            ushort one = 1;
            byte[] audioFormat = BitConverter.GetBytes(one); //2
            returnBytes.AddRange(audioFormat);

            byte[] numChannels = BitConverter.GetBytes(channels); //2
            byte[] returnchannels = new byte[2];
            returnchannels[0] = numChannels[0];
            returnchannels[1] = numChannels[1];
            returnBytes.AddRange(returnchannels);

            byte[] sampleRate = BitConverter.GetBytes(hz); //4
            returnBytes.AddRange(sampleRate);

            byte[] byteRate = BitConverter.GetBytes(hz * channels * 2);  //4 // sampleRate * bytesPerSample*number of channels, here 44100*2*2
            returnBytes.AddRange(byteRate);

            ushort blockAlign = (ushort)(channels * 2); //2
            returnBytes.AddRange(BitConverter.GetBytes(blockAlign));

            ushort bps = 16;
            byte[] bitsPerSample = BitConverter.GetBytes(bps); //2
            returnBytes.AddRange(bitsPerSample);

            byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data"); //4
            returnBytes.AddRange(datastring);

            byte[] subChunk2 = BitConverter.GetBytes(samples * channels * 2); //4
            returnBytes.AddRange(subChunk2);

            return returnBytes.ToArray();
        }


        //https://forum.unity3d.com/threads/check-current-microphone-input-volume.133501/
        //http://answers.unity3d.com/questions/1188892/error-executing-result-instance-m-sound-lockoffset.html

        static int _sampleWindow = 128;
        //get data from microphone into audioclip
        public static float LevelMax(AudioClip clip)
        {
            float levelMax = 0;
            float[] waveData = new float[_sampleWindow];
            int micPosition = Microphone.GetPosition(null) - (_sampleWindow + 1); // null means the first microphone
            if (micPosition < 0) { return 0; } //not enough samples

            clip.GetData(waveData, micPosition);
            //TODO known error. caused by unity's audio thread?


            // Getting a peak on the last 128 samples
            for (int i = 0; i < _sampleWindow; i++)
            {
                float wavePeak = waveData[i] * waveData[i];
                if (levelMax < wavePeak)
                {
                    levelMax = wavePeak;
                }
            }

            levelMax *= 128;
            return levelMax;
        }

        /// <summary>
        /// encode wav bytes into base64 string
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string EncodeWav(byte[] bytes)
        {
            return Convert.ToBase64String(bytes);
        }
    }
}