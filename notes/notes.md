# FSharp Audio Workshop - FSharp Exchange 4 - April 2018

Gauge the audience:

* How much F# experience?
* How much digital audio knowledge?

## What is sound?

Sound is the perception of vibrations transmitted by regular displacement of molecules in the air. Those vibrations move our eardrums, which stimulates nerves in our ear, which we perceive as sound.

## How do we record/replay it?

We can use a microphone to convert the movements of air molecules into an electrical (voltage) signal, and then use an amplifier to produce a larger signal to drive a loudspeaker, which in turn reproduces the changes in air pressure to replay the original sound.

If we plot this changing voltage against time, we get a classic waveform display.

We can observe 2 things:
1) Loudness corresponds to the peak-to-peak amplitude of the signal - the bigger the difference, the louder the sound seems. 
2) Pitch relates to the peak-to-peak frequency of the signal - the more peaks per second, the higher the apparent pitch.  

Electronic circuits are designed to work correctly (linearly) within certain tolerance ranges. If the voltage level of the input signal exceeds the designed level, the output will not be be a true reflection of the input. This is distortion. 

Amplifiers will often distort by clipping. The output will be clamped at a maximum level even if the input changes. So we try to keep our signals within a constrained dynamic range. 

## A few simple waveforms

1) A constant voltage (usually zero) - silence
2) A totally random waveform - white noise
3) Some regular waveforms
    a. Sine
    b. Square
    c. Triangle

Note that sine wave sounds 'purer' that the other waveforms - will explore this later

## How do we represent that digitally?

We can't directly record a continuous signal digitally, but we can sample it. We measure the level of the signal at regular intervals and turn the instantaneous voltage into a number. This process is called quantisation. That number is usually in linear proportion to a fixed reference voltage, but some schemes use a log scale to better model quiet sounds.

To reproduce the original signal we repeat the samples at the same rate that we recorded them, convert them back into a voltage, and smooth the differences between them.  

The more bits we use to quantise the signal, the more accurate the reproduction. CD uses 16 bits, DVD-Audio uses 24 bits, more pro audio systems use 32 or even 64 bits.  Many digital audio production system use floating point and only use fixed-point at the conversion stages. This is what we shall do. 

By convention we will use the range -1.0 .. 1.0 for our permitted  dynamic range. Any values outside this range will be constrained back into the range to prevent overloads.

Sampling theory tells us that we need to record and replay the samples at a frequency of at least twice the highest frequency we want to reproduce.  The human ear is generally reckoned to be able to recognise signals in the range 20 Hz - 20 kHz, so we need a sampling frequency of at least 40 kHz, hence the CD standard sampling rate of 44100Hz.

### Development environment

You need Visual Studio Code with all the Ionide plugins

* Create a new folder
* Open folder in VSCode
* Ctrl+Shift+P - F#: New Project - type: console - call it AudioWorkshop
  * Gives us a new project with Fake & Paket
* `git init` if you want to
* open console (Ctrl+Shift+') - cd to AudioWorkshop and `dotnet run`

## How can we model this in F#?

Let's assume we have a constant sampling rate throughout our system. In that case we can model a single waveform as a list of floats. Let's try this as a first attempt:

## Making sounds in Windows (and other OSes)

Your computer contains the necessary hardware to do the analogue to digital conversion, and the OS has libraries to support it. You tell the OS that you wish to playback some samples, and the hardware takes care of all the timing issues.

It isn't practical for the OS to request this one sample at a time, so you fill a buffer with a number of samples. The OS will call you back when it needs the next buffer filling.

We'll use the open source `NAudio` library (<https://github.com/naudio/NAudio>). We have to provide an implementation of the `IWaveProvider` interface.

### Import the NAudio packeage

* open the .fsproj
* Ctrl+Shift+P - Paket: Add Nuget Package (to current project)
* NAudio

We have an implementation of `IWaveProvider` that fills the buffer with random values. In `main` we create a `WasapiOut` device and pass it our implementation. When we call `Play`, the system calls `Read` repeatedly until we call `Stop` (in this case, after 2 seconds).

Note that `Random.NextDouble()` return values between 0.0 and 1.0, but the audio system expects values in the range -1.0 to 1.0, so we have to provide the `rebase` function.

``` fsharp
module AudioPlayground

open NAudio.Wave
open System
open NAudio.CoreAudioApi
open System.Threading

type NoiseOutput () =
    let waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 1) // samplerate:44.1kHz, mono
    let bytesPerSample = waveFormat.BitsPerSample / 8
    let random = Random()

    interface IWaveProvider with
        member __.WaveFormat with get() = waveFormat

        member __.Read (buffer, offset, count) =
            let mutable writeIndex = 0
            let putSample sample buffer =
                // convert float to byte array
                let bytes = BitConverter.GetBytes((float32)sample)
                // blit into correct position in buffer
                Array.blit bytes 0 buffer (offset + writeIndex) bytes.Length
                // update position
                writeIndex <- writeIndex + bytes.Length

            let nSamples = count / bytesPerSample
            let rescale value = (value * 2.0) - 1.0
            for _ in [0 .. nSamples - 1] do
                let sample = random.NextDouble() |> rescale
                putSample sample buffer
            // return the number of bytes written
            nSamples * bytesPerSample

[<EntryPoint>]
let main _ =
    let output = new WasapiOut(AudioClientShareMode.Shared, 1)
    NoiseOutput () |> output.Init
    output.Play ()
    Thread.Sleep 2000
    output.Stop ()
    0
```
