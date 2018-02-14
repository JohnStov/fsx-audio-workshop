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

            let (|HasMore|_|) (enumerator : Collections.Generic.IEnumerator<float>) =
                if enumerator.MoveNext() then Some enumerator.Current else None

            let nSamples = count / bytesPerSample
            for _ in [1 .. nSamples] do
                let sample = 
                    match enumerator with
                    | HasMore value -> value
                    | _ -> 0.0
                putSample sample buffer
            // return the number of bytes written
            nSamples * bytesPerSample

let makeNoise =
    let random = Random()
    let rescale value = (value * 2.0) - 1.0
    let rec noise () = 
        seq { 
            yield random.NextDouble() |> rescale
            yield! noise () 
        }
    noise ()

let makeNoise1Sec = 
    makeNoise |> Seq.take 44100

let TWOPI = 2.0 * Math.PI

let generate fn sampleRate frequency = 
    let delta = TWOPI * frequency / float sampleRate
    let gen theta = Some (fn theta, (theta + delta) % TWOPI)
    Seq.unfold gen 0.0

let makeSine = generate Math.Sin

let square theta = 
    if theta < Math.PI then -1.0 else 1.0

let makeSquare = generate square

let sawtooth theta = 
    (theta / Math.PI) - 1.0

let makeSawtooth = generate sawtooth
 
[<EntryPoint>]
let main _ =
    let output = new WasapiOut(AudioClientShareMode.Shared, 1)
    makeSquare 44100 440.0 |> SeqProvider  |> output.Init
    output.Play ()
    Thread.Sleep 2000
    output.Stop ()
    0