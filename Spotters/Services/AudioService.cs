using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.VisualBasic.ApplicationServices;
using NAudio.Wave;

namespace Spotters.Services;

public sealed class AudioService
{
    private List<WaveInEvent> waveIns = new List<WaveInEvent>();

    private int GetDeviceIndexByName(string deviceName)
    {
        for (int i = 0; i < WaveIn.DeviceCount; i++)
        {
            if (WaveIn.GetCapabilities(i).ProductName == deviceName)
                return i;
        }
        return -1;
    }

    public void InitializeAudioCapture(HubConnection signalRconnection, List<UserAudioMapping> _usersAudioMapping)
    {
        foreach (var user in _usersAudioMapping)
        {
            int deviceIndex = GetDeviceIndexByName(user.AudioDeviceProductName);

            if (deviceIndex == -1)
            {
                MessageBox.Show($"User {user.UserName} must configure audio input", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var waveIn = new WaveInEvent
            {
                DeviceNumber = deviceIndex,
                WaveFormat = new WaveFormat(44100, 1)
            };

            waveIn.DataAvailable += (sender, e) =>
            {
                float volume = CalculateVolume(e.Buffer);
                SendVolumeToSignalR(signalRconnection, user, volume);
            };

            waveIn.StartRecording();

            waveIns.Add(waveIn);
        }
    }

    private void SendVolumeToSignalR(HubConnection signalRconnection, UserAudioMapping user, float volume)
    {
        if (signalRconnection.State == HubConnectionState.Connected)
        {
            if (user.ActiveCharacter == null)
                user.Characters.First().Active = true; // Ensure there's an active character    

            signalRconnection.InvokeAsync("SendVolume", user.UserName, volume, user.ActiveCharacter?.Name, user.ActiveCharacter?.Visible);

            foreach (var inactiveCharacter in user.Characters.Where(it => !it.Active))
            {
                signalRconnection.InvokeAsync("SendVolume", user.UserName, 0, inactiveCharacter.Name, inactiveCharacter.Visible);
            }
        }
    }

    private float CalculateVolume(byte[] buffer)
    {
        int maxAmplitude = 0;

        for (int i = 0; i < buffer.Length; i += 2)
        {
            short sample = BitConverter.ToInt16(buffer, i);

            int amplitude;

            // Evitar el problema con -32768
            if (sample == short.MinValue)
            {
                amplitude = short.MaxValue;
            }
            else
            {
                amplitude = Math.Abs(sample);
            }

            maxAmplitude = Math.Max(maxAmplitude, amplitude);
        }

        return maxAmplitude / 32768f;
    }
}
