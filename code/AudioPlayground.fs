module AudioPlayground

open NAudio.Wave
open System
open NAudio.CoreAudioApi
open System.Threading

type AudioSample = float seq

type SeqProvider (waveform: AudioSample) =
    // samplerate:44.1kHz, mono
    let waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 1)
    let bytesPerSample = waveFormat.BitsPerSample / 8
    let enumerator = waveform.GetEnumerator()

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
            for _ in [0 .. nSamples - 1] do
                // guard against buffer overrun
                let sample = 
                    match enumerator.MoveNext() with
                    | true -> enumerator.Current 
                    | false -> 0.0
                putSample sample buffer
            // return the number of bytes written
            nSamples * bytesPerSample

let makeNoise =
    let random = Random()
    let rescale value = (value * 2.0) - 1.0
    let gen _ = Some (random.NextDouble() |> rescale, ()) 
    Seq.unfold gen ()

let makeNoise1Sec = 
    makeNoise |> Seq.take 44100

let TWOPI = 2.0 * Math.PI

let makeSine sampleRate frequency = 
    let delta = TWOPI * frequency / float sampleRate
    let gen theta = Some (Math.Sin theta, (theta + delta) % TWOPI)
    Seq.unfold gen 0.0

[<EntryPoint>]
let main _ =
    let output = new WasapiOut(AudioClientShareMode.Shared, 1)
    makeSine 44100 440.0 |> SeqProvider  |> output.Init
    output.Play ()
    Thread.Sleep 2000
    output.Stop ()
    0