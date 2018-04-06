// aud1

module AudioPlayground

open NAudio.Wave
open System
open NAudio.CoreAudioApi
open System.Threading

type NoiseProvider () =
    // samplerate:44.1kHz, mono
    let waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 1)
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
    NoiseProvider () |> output.Init
    output.Play ()
    Thread.Sleep 2000
    output.Stop ()
    0

// aud2

module AudioPlayground

open NAudio.Wave
open System
open NAudio.CoreAudioApi
open System.Threading

type AudioStream = float array

type StreamProvider (waveform: AudioStream) =
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

            let nSamples = count / bytesPerSample
            for _ in [0 .. nSamples - 1] do
                // guard against buffer overrun
                let sample = if readIndex < waveform.Length then waveform.[readIndex] else 0.0
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
    noise |> StreamProvider |> output.Init
    output.Play ()
    Thread.Sleep 2000
    output.Stop ()
    0

// aud3

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

let makeNoise =
    let random = Random()
    let rescale value = (value * 2.0) - 1.0
    let rec noise () = 
        seq { 
            yield random.NextDouble() |> rescale
            yield! noise () 
        }
    noise ()

[<EntryPoint>]
let main _ =
    let output = new WasapiOut(AudioClientShareMode.Shared, 1)
    makeNoise |> StreamProvider |> output.Init
    output.Play ()
    Thread.Sleep 2000
    output.Stop ()
    0

// aud4

let TWOPI = 2.0 * Math.PI

let makeSine sampleRate frequency = 
    let delta = TWOPI * frequency / float sampleRate
    let gen theta = Some (Math.Sin theta, (theta + delta) % TWOPI)
    Seq.unfold gen 0.0

// aud4a

let sampleRate = 44100

let generate fn sampleRate frequency =
    let delta = TWOPI * frequency / float sampleRate
    let gen theta = Some (fn theta, (theta + delta) % TWOPI)
    Seq.unfold gen 0.0

let makeSine = generate Math.Sin

let makeSquare =
    let square theta =
        if theta < Math.PI then -1.0 else 1.0
    generate square

let makeSawtooth =
    let sawtooth theta =
        (theta / Math.PI) - 1.0
    generate sawtooth

// aud5

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

// aud6

let generate fn sampleRate (frequency : AudioStream) = 
    let enumerator = frequency.GetEnumerator()
    let gen theta = 
        let f = if enumerator.MoveNext() then enumerator.Current else 0.0
        let delta = TWOPI * f / float sampleRate
        Some (fn theta, (theta + delta) % TWOPI)
    Seq.unfold gen 0.0

let makeSine = generate Math.Sin sampleRate

let Constant value =
    Seq.unfold (fun _ -> Some(value, ())) ()

let sin440 = makeSine (Constant 440.0)

// aud6a

let vibrato =
    let sin = makeSine (Constant 3.0)
    sin |> Seq.map (fun x -> (x * 20.0) + 440.0)

let wobblySine =
    vibrato |> makeSine

// aud6b

let gain seq1 seq2 =
    Seq.zip seq1 seq2 |> Seq.map (fun (a, b) -> a * b)

let offset seq1 seq2 =
    Seq.zip seq1 seq2 |> Seq.map (fun (a, b) -> a + b)

let vibrato =
    makeSine (Constant 3.0) |> gain (Constant 20.0) |> offset (Constant 440.0)

// aud6c

let zipMap fn seq1 seq2 =
    Seq.zip seq1 seq2 |> Seq.map (fun (x, y) -> fn x y)

let gain seq1 seq2 =
    zipMap (*) seq1 seq2

let offset seq1 seq2 =
    zipMap (+) seq1 seq2

// aud7

let getInDevice () = 
    let deviceRange = [0..MidiIn.NumberOfDevices-1]
    match deviceRange |> List.tryFind (fun n -> MidiIn.DeviceInfo(n).ProductName = "MPKmini2") with
    | Some id -> 
      printfn "Play some music"
      Some (new MidiIn(id))
    | None -> 
      printfn "You forgot to plug in the keyboard"
      None

// aud8

let noteNumberToFrequency noteNumber =
    match noteNumber with
    | 0 -> 0.0
    | _ -> Math.Pow(2.0, (float (noteNumber-69)) / 12.0) * 440.0

// aud9a

let midiEvents (evt : IObservable<MidiInMessageEventArgs>) =
    evt |> Observable.map (fun e -> e.MidiEvent)

// aud9b

let noteEvents (evt : IObservable<MidiEvent>) =
    evt |> Observable.filter (fun e -> e :? NoteEvent)
    |> Observable.map (fun e -> e :?> NoteEvent)

// aud10

let (|NoteOff|_|) (evt : NoteEvent) =
    match evt.CommandCode, evt.Velocity with
    | MidiCommandCode.NoteOff, _ -> Some evt
    | MidiCommandCode.NoteOn, 0 -> Some evt
    | _, _ -> None

let (|NoteOn|_|) (evt : NoteEvent) =
    match evt.CommandCode, evt.Velocity with
    | MidiCommandCode.NoteOn, v when v > 0 -> Some evt
    | _, _ -> None

// aud11

let noteStream (evt : IObservable<NoteEvent>) =
    let mutable note = 0

    evt.Add(fun event ->
        note <- match event with
                | NoteOff _ ->  0
                | NoteOn n -> n.NoteNumber
                | _ -> note)

    Seq.unfold (fun _ -> Some(noteNumberToFrequency note, ())) ()

// aud12

let waitForKeyPress () =
    let mutable goOn = true
    while goOn do
        let key = Console.ReadKey(true)
        if key.KeyChar = ' '
        then
            goOn <- false

let runWith (input : MidiIn) (output : IWavePlayer) =
    input.MessageReceived |> midiEvents |> noteEvents |> noteStream |> makeSine |> StreamProvider |> output.Init

    output.Play ()
    input.Start ()

    waitForKeyPress ()

    output.Stop ()
    input.Stop ()

[<EntryPoint>]
let main _ =
    match getInDevice () with
    | None -> -1
    | Some input ->
        runWith input (new WasapiOut(AudioClientShareMode.Shared, 1))
        0

// aud13

let vibrato depth modulation freq =
    makeSine modulation |> gain depth |> offset freq

let wobblySine depth modulation freq =
    vibrato depth modulation freq |> makeSine

// aud14

let controlStream controller (evt : IObservable<MidiInMessageEventArgs>) =
    let mutable controlValue = 0

    evt.Add(fun msg ->
        controlValue <- match msg.MidiEvent with
                        | :? ControlChangeEvent as ctrl -> 
                             if int ctrl.Controller = controller
                             then ctrl.ControllerValue
                             else controlValue
                        | _ -> controlValue
    )

    Seq.unfold (fun _ -> Some(float controlValue, ())) ()

//aud15

let runWith (input : MidiIn) (output : IWavePlayer) =
    let depth = input.MessageReceived |> (controlStream 1)
    let modulation = input.MessageReceived |> (controlStream 2)
    input.MessageReceived |> midiEvents |> noteEvents |> noteStream |> (wobblySine depth modulation) |> StreamProvider |> output.Init

    output.Play ()
    input.Start ()

    waitForKeyPress ()

    output.Stop ()
    input.Stop ()

// aud16

let merge (strm : AudioStream) (evt: IObservable<AudioStream>) =
    let mutable enumerator = strm.GetEnumerator()
    let nextValue () = if enumerator.MoveNext() then enumerator.Current else 0.0

    evt.Add (fun msg -> enumerator <- msg.GetEnumerator())

    Seq.unfold (fun _ -> Some(nextValue (), ()) ) ()

// aud17

let trigger (generator: float -> AudioStream) (trig: IObservable<NoteEvent>) : AudioStream =
    let noteOns = trig |> Observable.filter (fun evt -> evt :? NoteOnEvent && evt.Velocity > 0)
    let freqs = noteOns |> Observable.map (fun evt -> noteNumberToFrequency evt.NoteNumber)

    let streams = freqs |> Observable.map generator

    let initial = Constant (0.0)
    merge initial streams

// aud18

let runWith (input : MidiIn) (output : IWavePlayer) =
    let events = input.MessageReceived |> midiEvents 
    let notes = events |> noteEvents
    notes |> trigger (pluck sampleRate) |> StreamProvider |> output.Init

    output.Play ()
    input.Start ()

    waitForKeyPress ()

    output.Stop ()
    input.Stop ()