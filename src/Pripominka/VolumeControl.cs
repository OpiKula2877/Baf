using NAudio.CoreAudioApi;

namespace Pripominka;

public sealed class VolumeControl : IVolume
{
    public VolumeState Get()
    {
        using var device = DefaultDevice();
        var volume = device.AudioEndpointVolume;
        return new VolumeState(volume.MasterVolumeLevelScalar, volume.Mute);
    }

    public void Set(VolumeState state)
    {
        using var device = DefaultDevice();
        var volume = device.AudioEndpointVolume;
        volume.Mute = state.Muted;
        volume.MasterVolumeLevelScalar = Math.Clamp(state.Level, 0f, 1f);
    }

    // Výchozí zařízení se může změnit, proto ho získáváme při každém volání.
    private static MMDevice DefaultDevice()
    {
        using var enumerator = new MMDeviceEnumerator();
        return enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
    }
}
