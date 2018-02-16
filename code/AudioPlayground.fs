module AudioPlayground

open NAudio.Wave
open System
open NAudio.CoreAudioApi
open NAudio.Midi

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

let zipMap fn seq1 seq2 =
    Seq.zip seq1 seq2 |> Seq.map (fun (x, y) -> fn x y)

let gain = 
    zipMap (*)
    
let offset = 
    zipMap (+)

let vibrato =
     makeSine (Constant 3.0) |> gain (Constant 20.0) |> offset (Constant 440.0)

let wobblySine =
    vibrato |> makeSine

let waitForKeyPress () = 
    let mutable goOn = true
    while goOn do
        let key = Console.ReadKey(true)
        if key.KeyChar = ' ' 
        then 
            goOn <- false

let getInDevice () = 
    let deviceRange = [0.. MidiIn.NumberOfDevices-1]
    match deviceRange |> List.tryFind (fun n -> MidiIn.DeviceInfo(n).ProductName = "MPKmini2") with
    | Some id -> 
        printfn "Play some music"
        Some (new MidiIn(id))
    | None -> 
        printfn "You forgot to plug in the keyboard"
        None

let noteStream (evt : IObservable<MidiInMessageEventArgs>) = 
    let mutable note = 0
    let noteNumberToFrequency noteNumber =
        match noteNumber with
        | 0 -> 0.0
        | _ -> Math.Pow(2.0, (float (noteNumber-69)) / 12.0) * 440.0

    evt.Add(fun msg -> 
        note <- match msg.MidiEvent with
                | :? NoteOnEvent as noteOn -> 
                    if note = noteOn.NoteNumber && noteOn.Velocity = 0 
                    then 0 
                    else noteOn.NoteNumber
                | :? NoteEvent as noteOff -> 
                    if note = noteOff.NoteNumber 
                    then 0 
                    else note
                | _ -> note
        )

    Seq.unfold (fun _ -> Some(noteNumberToFrequency note, ())) ()

let runWith (input : MidiIn) (output : IWavePlayer) =
    input.MessageReceived |> noteStream |> makeSine |> StreamProvider |> output.Init
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
