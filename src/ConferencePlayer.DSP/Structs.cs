using System;
using System.Runtime.InteropServices;

namespace ConferencePlayer.DSP;

/// <summary>
/// Represents a detected feedback event with zero heap allocation.
/// Used by the Howling Detector to report candidates.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct HowlingEvent
{
    public double FrequencyHz;
    public double MagnitudeDb;
    public bool IsNew;

    public HowlingEvent(double frequencyHz, double magnitudeDb, bool isNew)
    {
        FrequencyHz = frequencyHz;
        MagnitudeDb = magnitudeDb;
        IsNew = isNew;
    }
}

/// <summary>
/// Represents the parameters of a biquad notch filter.
/// Used by the Notch Bank to manage filter states.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct NotchFilterParams
{
    public double FrequencyHz;
    public double Q;
    public double GainDb;
    public bool IsActive;

    public NotchFilterParams(double frequencyHz, double q, double gainDb, bool isActive = true)
    {
        FrequencyHz = frequencyHz;
        Q = q;
        GainDb = gainDb;
        IsActive = isActive;
    }
}
