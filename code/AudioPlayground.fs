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

let sampleRate = 44100

type StreamOrConstant =
    | Stream of AudioSample
    | Constant of float

let nextValue input =
    match input with 
    | Stream strm -> Seq.head strm
    | Constant value -> value

let generate fn sampleRate frequency = 
    let gen theta = 
        let delta = TWOPI * (frequency |> nextValue) / float sampleRate
        Some (fn theta, (theta + delta) % TWOPI)
    Seq.unfold gen 0.0

let constant value =
    Seq.unfold (fun _ -> Some (value, ())) ()

let zipMap fn seq1 seq2 = 
    Seq.zip seq1 seq2 |> Seq.map (fun (a, b) -> fn a b)

let sum : float seq -> float seq -> float seq =
    zipMap (+)

let gain : float seq -> float seq -> float seq =
    zipMap (*)

let makeSine = generate Math.Sin sampleRate

let makeSquare = 
    let square theta = if theta < Math.PI then -1.0 else 1.0
    generate square sampleRate

let makeSawtooth = 
    let sawtooth theta = (theta / Math.PI) - 1.0
    generate sawtooth sampleRate

let pluck sampleRate frequency =
    // frequency is determined by the length of the buffer
    let bufferLength = sampleRate / int frequency
    // start with noise
    let buffer = makeNoise |> Seq.take bufferLength |> Seq.toArray
    // go round the buffer repeatedly, playing each sample, 
    // then averaging with previous and decaying
    let gen index =
        let nextIndex = (index + 1) % bufferLength
        let value = buffer.[nextIndex]
        buffer.[nextIndex] <- (value + buffer.[index]) / 2.0 * 0.996
        Some(value, nextIndex)
    Seq.unfold gen (bufferLength - 1)
 
[<EntryPoint>]
let main _ =
    let output = new WasapiOut(AudioClientShareMode.Shared, 1)
    let sine = makeSine (Constant 440.0)
    sine |> SeqProvider  |> output.Init
    output.Play ()
    Thread.Sleep 2000
    output.Stop ()
    0