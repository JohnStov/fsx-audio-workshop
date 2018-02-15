module AudioPlayground

open NAudio.Wave
open System
open NAudio.CoreAudioApi
open System.Threading

type AudioStream = float seq

type StreamProvider (waveform: AudioStream) =
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
            for _ in [1 .. nSamples] do
                let sample = if enumerator.MoveNext() then enumerator.Current else 0.0
                putSample sample buffer
            // return the number of bytes written
            nSamples * bytesPerSample
let TWOPI = 2.0 * Math.PI
let sampleRate = 44100

let generate fn sampleRate (frequency : AudioStream) = 
    let enumerator = frequency.GetEnumerator()
    let gen theta = 
        let f = if enumerator.MoveNext() then enumerator.Current else 0.0
        let delta = TWOPI * f / float sampleRate
        Some (fn theta, (theta + delta) % TWOPI)
    Seq.unfold gen 0.0

let Constant value =
    Seq.unfold (fun _ -> Some (value, ())) ()
let makeSine : AudioStream -> AudioStream = generate Math.Sin sampleRate

let vibrato =
    let sin = makeSine (Constant 3.0)
    sin |> Seq.map (fun x -> (x * 20.0) + 440.0)

let wobblySine =
    vibrato |> makeSine
 
[<EntryPoint>]
let main _ =
    let output = new WasapiOut(AudioClientShareMode.Shared, 1)
    wobblySine |> StreamProvider |> output.Init
    output.Play ()
    Thread.Sleep 2000
    output.Stop ()
    0