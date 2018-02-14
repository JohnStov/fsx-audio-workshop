module AudioPlayground

open NAudio.Wave
open System
open NAudio.CoreAudioApi
open System.Threading

type AudioSample = float array

type ArrayProvider (waveform: AudioSample) =
    // samplerate:44.1kHz, mono
    let waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 1)
    let bytesPerSample = waveFormat.BitsPerSample / 8
    let mutable readIndex = 0

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

            let (|NotEnd|_|) (waveform : AudioSample) index =
                if index < waveform.Length then Some index else None

            let nSamples = count / bytesPerSample
            for _ in [0 .. nSamples - 1] do
                let sample = 
                    match readIndex with
                    | NotEnd waveform i -> waveform.[i]
                    | _ -> 0.0
                readIndex <- readIndex + 1
                putSample sample buffer
            // return the number of bytes written
            nSamples * bytesPerSample

let makeNoise nSamples =
    let random = Random()
    let rescale value = (value * 2.0) - 1.0
    Array.init nSamples (fun _ -> random.NextDouble() |> rescale)

[<EntryPoint>]
let main _ =
    let output = new WasapiOut(AudioClientShareMode.Shared, 1)
    let noise = makeNoise 44100
    noise |> ArrayProvider  |> output.Init
    output.Play ()
    Thread.Sleep 2000
    output.Stop ()
    0