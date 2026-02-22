using System;

namespace ConferencePlayer.DSP;

/// <summary>
/// Module 1: Ultra-Early Howling Detection (HD) Engine.
/// Detects resonant frequencies using NINOS²-T sparsity measure and PHPR/PNPR verification.
/// </summary>
public interface IHowlingDetector
{
    /// <summary>
    /// Processes the input signal to detect howling candidates.
    /// This method must be zero-allocation.
    /// </summary>
    /// <param name="input">The input audio buffer (e.g. microphone signal).</param>
    /// <param name="resultsBuffer">A pre-allocated buffer to store detected howling events.</param>
    /// <returns>The number of howling events detected and written to <paramref name="resultsBuffer"/>.</returns>
    int Detect(ReadOnlySpan<double> input, Span<HowlingEvent> resultsBuffer);

    /// <summary>
    /// Resets internal state (buffers, history).
    /// </summary>
    void Reset();
}

/// <summary>
/// Module 2: Reactive Precision Notch Suppression (NHS).
/// Manages a bank of fixed and live biquad notch filters.
/// </summary>
public interface INotchBank
{
    /// <summary>
    /// Applies the active notch filters to the audio buffer in-place.
    /// </summary>
    /// <param name="buffer">The audio buffer to process.</param>
    void Process(Span<double> buffer);

    /// <summary>
    /// Adds a fixed notch filter (e.g. from room calibration).
    /// </summary>
    /// <param name="frequencyHz">Center frequency.</param>
    /// <param name="q">Quality factor.</param>
    /// <param name="gainDb">Gain in dB (negative for cut).</param>
    /// <returns>True if added successfully.</returns>
    bool AddFixedNotch(double frequencyHz, double q, double gainDb);

    /// <summary>
    /// Adds or updates a live notch filter based on a detected howling frequency.
    /// Logic includes adaptive bandwidth merge and deepening.
    /// </summary>
    /// <param name="frequencyHz">The detected howling frequency.</param>
    /// <returns>True if a new filter was engaged or an existing one updated.</returns>
    bool AddLiveNotch(double frequencyHz);

    /// <summary>
    /// Clears all live notch filters.
    /// </summary>
    void ClearLiveNotches();
}

/// <summary>
/// Module 3: Adaptive Feedback Cancellation (AFC) via Noise Injection.
/// Uses NLMS adaptive filtering and Generalized Levinson-Durbin (GLD) for unbiased estimation.
/// </summary>
public interface IAdaptiveFilter
{
    /// <summary>
    /// Applies the FIR cancellation to the microphone input.
    /// y_clean = y_mic - (w_tilde * v_ref)
    /// </summary>
    /// <param name="micInput">The raw microphone input.</param>
    /// <param name="cleanOutput">The buffer to store the cleaned signal.</param>
    void ProcessInput(ReadOnlySpan<double> micInput, Span<double> cleanOutput);

    /// <summary>
    /// Adds any necessary injection signal (e.g. white noise burst) to the loudspeaker output.
    /// </summary>
    /// <param name="outputBuffer">The buffer going to the speakers (modified in-place).</param>
    void AddInjectionSignal(Span<double> outputBuffer);

    /// <summary>
    /// Triggers the 16ms burst of zero-mean white Gaussian noise and GLD estimation.
    /// </summary>
    void TriggerCalibration();

    /// <summary>
    /// Gets whether the noise injection is currently active.
    /// </summary>
    bool IsInjectingNoise { get; }
}

/// <summary>
/// Module 4: Emergency Rescue Gain Controller.
/// Applies immediate broadband gain reduction if system remains unstable.
/// </summary>
public interface IRescueGainController
{
    /// <summary>
    /// Applies gain reduction to the buffer in-place.
    /// </summary>
    /// <param name="buffer">The audio buffer to process.</param>
    void Process(Span<double> buffer);

    /// <summary>
    /// Triggers an immediate broadband gain reduction (fast drop).
    /// </summary>
    void TriggerRescue();

    /// <summary>
    /// Resets the gain to unity (0dB).
    /// </summary>
    void Reset();

    /// <summary>
    /// Gets the current gain application value (linear multiplier).
    /// </summary>
    double CurrentGain { get; }
}

/// <summary>
/// The main DSP pipeline orchestrator.
/// </summary>
public interface IAudioPipeline
{
    /// <summary>
    /// Processes a single frame of audio through the 4-layer hybrid defense pipeline.
    /// strictly adhering to the integration order:
    /// 1. AFC (Module 3) cancellation on input.
    /// 2. Howling Detection (Module 1).
    /// 3. Update Module 2/3/4 based on detection.
    /// 4. Notch Filters (Module 2) on output.
    /// 5. Rescue Gain (Module 4) on output.
    /// </summary>
    /// <param name="input">Microphone input buffer.</param>
    /// <param name="output">Processed output buffer for speakers.</param>
    void Process(ReadOnlySpan<double> input, Span<double> output);

    // Expose modules for configuration/monitoring if needed, or keep encapsulated.
    // For now, keeping it minimal as requested.
}
