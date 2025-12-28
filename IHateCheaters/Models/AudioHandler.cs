using System.Reflection;
using UnityEngine;

// ReSharper disable MustUseReturnValue

namespace IHateCheaters.Models;

public static class AudioHandler
{
    private static AudioClip? _notificationClip;
    private static AudioSource? _audioSource;

    public static void PlayNotification(GameObject? parent = null)
    {
        if (!_notificationClip)
            _notificationClip = LoadWavFromResource("IHateCheaters.Resources.notification.wav");

        if (!_notificationClip)
        {
            Debug.LogError("Failed to load notification.wav");
            return;
        }

        GameObject audioObject = parent ?? new GameObject("NotificationAudio");
        if (!_audioSource)
        {
            _audioSource = audioObject.AddComponent<AudioSource>();
        }

        _audioSource.PlayOneShot(_notificationClip);
    }

    private static AudioClip? LoadWavFromResource(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using Stream? stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) return null;

        var wavData = new byte[stream.Length];

        stream.Read(wavData);
        return WavToAudioClip(wavData, resourceName);
    }

    private static AudioClip WavToAudioClip(byte[] wavFile, string clipName)
    {
        var channels = BitConverter.ToInt16(wavFile, 22);
        var sampleRate = BitConverter.ToInt32(wavFile, 24);
        const int dataStartIndex = 44;
        var sampleCount = (wavFile.Length - dataStartIndex) / 2;

        var samples = new float[sampleCount];
        for (var i = 0; i < sampleCount; i++)
        {
            var sample = BitConverter.ToInt16(wavFile, dataStartIndex + i * 2);
            samples[i] = sample / 32768f;
        }

        var clip = AudioClip.Create(clipName, sampleCount / channels, channels, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}