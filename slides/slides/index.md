

*** 

# FSharp Audio Workshop

### John Stovin

![twitter](images/Twitter.png)@johnstovin

### FSharp Exchange London

### 4-5 April 2018

' Gauge the audience:
' * F# experience level?
' * Digital audio knowledge?

***

## What is sound?

Regular back & forth displacement of air molecules

-> Moves your eardrum

-> Stimulates aural nerves

-> Perception of _sound_

---

## How can we capture sound?

Microphone:

* electro-mechanical device
* turns vibration into electrical signal

---

## Audio waveform example

![Waveform](images/waveform.png)]

y-axis - voltage (around 0)
x -axis - time

---

## Observations

1. _Loudness_ corresponds to peak-to-peak voltage
2. _Pitch_ corresponds to peak-to-peak frequency

' Electronic circuits are designed to work correctly (linearly) within certain tolerance ranges. If the voltage level of the input signal exceeds the designed level, the output will not be be a true reflection of the input. This is distortion.

' Amplifiers will often distort by clipping. The output will be clamped at a maximum level even if the input changes. So we try to keep our signals within a constrained dynamic range. 

---

## Example waveforms

1. No signal - silence
![No Signal](images/no_signal.png)]
2. Random values - noise
![Noise](images/noise.png)]
3. Sine wave
![Sine](images/sine.png)]
4. Square wave
![Square](images/square.png)]
5. Triangle wave
![Triangle](images/triangle.png)]

' Note that sine wave sounds 'purer' that the other waveforms - will explore this later

---

## Digital recording

_Sample_ the analogue signal at regular intervals

Convert the instantaneous voltage into a number between 0 and some limit (+/- 32767 - 8-bit) - _Quantisation_, _A/D Conversion_

' More bits -> higher accuracy, better quality reproduction

Store the sequence of numbers generated

For playback - regenerate instantaneous voltage synchronised to clock - _D/A Conversion_

---

## Digital Synthesis

Generate sequences of samples algorithmically, then perform D/A conversion

' In reality, we prefer floating point values - it makes computation easier - then we convert them to fixed point at the last minute

***

## Let's get coding!

' I prefer VSCode with Ionide plugins
' Create a new folder
' Open folder in VSCode
' Ctrl+Shift+P - F#: New Project - type: console - call it AudioWorkshop
'   Gives us a new project with Fake & Paket
' `git init` if you want to
' open console (Ctrl+Shift+') - cd to AudioWorkshop and `dotnet run`

---

## Audio library

We'll use **NAudio**: <https://github.com/naudio/NAudio>

' open the project file
' Ctrl+Shift+P - Paket: Add Nuget Package (to current project)
' NAudio
' Adds the dependency to the currecnt project

---

## Implementing a provider

' snippet aud1

' NAudio needs a callback
' You do this by implementing the `IWaveProvider` interface
' Tells NAudio about the structure of the data you'll be giving it.
' Asks you to fill a buffer when more samples are needed

' There's a for loop and a mutable index
' Note the |> operator in main

***

## Generalising

Let's change this to provide an arbitrary set of samples and have the system play them?

' snippet aud2 - replace everything

---

## Sidenote

Random access in lists is really expensive

' I originally implemented AudioSample as float list
' F# can't traverse the list to the end faste enough
' change it and show what happens

---

## Lazy evaluation

I don't want to have to define my entire sound before I play it

`seq` is F#'s wrapper around `IEnumerable`

Let's reimplement...

' snippet aud3

Note the use of an _Active Pattern_ in the output function

`makeNoise` uses a recursive  _seq compehension_ to generate an infinite lazy sequence of random values

' snippet aud3a

We can use `take` to create a finite sequence from our infinite sequence

***

## More complex signals -  Sinewave

' snippet aud4

We need to introduce _frequency_ (f) - how often the waveform repeats per second

To generate a repeating waveform we also need _phase_ - conventionally measured as an angle between 0 to Two PI radians

Phase revolves every 1/f seconds

The change in phase from one sample to the next we will call _delta_

The current _phase angle_ will be called _theta_

The amplitude is calculated as _sin theta_

---

## Higher order functions

``` fsharp
let makeSine sampleRate frequency =
    let delta = TWOPI * frequency / float sampleRate
    let gen theta = Some (Math.Sin theta, (theta + delta) % TWOPI)
    Seq.unfold gen 0.0
```
In functional languages, we don't like write our own loops.

The standard library provides functions that allow us to provide repeated operations. Many of these are _higher order functions_ - functions that take other functions as an argument.

`Seq.unfold` generates `seq<'T>`. It takes an initial state and a function.

The function signature is `'State -> ('T * 'State) option`

If the function returns `None`, the sequence terminates.

If it returns `Some ('T * 'State)`, the `'T` value is appended to the sequence, and the function is called again with the new value of `'State`.

---

### Squarewave

We can create a function to generate a square wave.

If theta < PI, the value is -1.0, otherwise it is +1.0

We can create a new function to generate squarewaves by substituting our square function for Math.Sin

---

### Refactoring to higher order functions

' snippet aud4a

``` fsharp
let sampleRate = 44100

let generate fn sampleRate frequency =
    let delta = TWOPI * frequency / float sampleRate
    let gen theta = Some (fn theta, (theta + delta) % TWOPI)
    Seq.unfold gen 0.0

let makeSine = generate Math.Sin sampleRate

let makeSquare =
    let square theta =
        if theta < Math.PI then -1.0 else 1.0
     generate square sampleRate

let makeSawtooth =
    let sawtooth theta =
        (theta / Math.PI) - 1.0
    generate sawtooth sampleRate
```

We can refactor the code duplication to a higher-order function that takes our generator function as an argument

If we make the generator function the first argument we can use partial application to generate a family of sound generator functions

---

## Other sound generating algorithms - Karplus-Strong

<https://en.wikipedia.org/wiki/Karplus%E2%80%93Strong_string_synthesis>

Developed in the early 1980s. This is a simple algorithm to generate plucked or hammered instrument sounds

' snippet aud5

***

## Modulation

Can we produce vibrato

Need to be able to change the frequency of our oscillator over time. The obviosu way to do this is to make the frequency parameter a stream.

```fsharp
let generate fn sampleRate (frequency : AudioStream) = 
    let enumerator = frequency.GetEnumerator()
    let gen theta =
        let f = if enumerator.MoveNext() then enumerator.Current else 0.0
        let delta = TWOPI * f / float sampleRate
        Some (fn theta, (theta + delta) % TWOPI)
    Seq.unfold gen 0.0

let Constant value =
    Seq.unfold (fun _ -> Some(value, ())) ()

let sin440 = makeSine (Constant 440.0)
```

' snippet aud6

---

## Vibrato

We need a stream that varies between 420.0 - 460.0, at 3 Hz

``` fsharp
let vibrato =
    let sin = makeSine (Constant 3.0)
    sin |> Seq.map (fun x -> (x * 20.0) + 440.0)

let wobblySine =
    vibrato |> makeSine
```

---

## A bit of refactoring

``` fsharp
let gain seq1 seq2 =
    Seq.zip seq1 seq2 |> Seq.map (fun (a, b) -> a * b)

let offset seq1 seq2 =
    Seq.zip seq1 seq2 |> Seq.map (fun (a, b) -> a + b)

let vibrato =
     makeSine (Constant 3.0) |> gain (Constant 20.0) |> offset (Constant 440.0)
```

---

### More refactoring

``` fsharp
let zipMap fn seq1 seq2 =
    Seq.zip seq1 seq2 |> Seq.map (fun (x, y) -> fn x y)

let gain =
    zipMap (*)

let offset =
    zipMap (+)
```

***

## MIDI

MIDI (_Musical Instrument Digital Interface_) is a protocol for controlling musical interfaces, originally across a dedicated hardware bus, but now over a variety of transports.

My keyboard transmits MIDI over USB.

NAudio has a MIDI support.

---

## MIDI Notes

Part of the MIDI protocol covers musical notes. By default it assumes the standard _equal-tempered_ scale. 

Each semitone is mapped onto a _note number_ in the range 0-127. _Middle C_ is 60. Concert A (440 Hz) is 69.

Each note message also has a _velocity_ part (how hard the key is hit). Lifting a key either sends a Note Off message, or another Note On message with a velocity of 0.

---

## MIDI Controls

MIDI supports 127 control devices. Each control can send control change events with a value in the range 0-127.

***

## Reactive Programming

' Who here knows about Rx?

' Who has used it?

One of the great features of FSharp is that events also implement IObservable. This means that you have the power of Rx available automatically.

Rx gives you the power to compose events in the same way that IEnumerable allows you to compose sequences.

---

`IObservable<T>` is the semantic inverse of `IEnumerable<T>`

`IObservable<T>.Subscribe()` <-> `IEnumerable<T>.GetEnumerator()`

`IObserver` is the semantic inverse of `IEnumerator`

`IObserver.OnNext(T)` <-> `IEnumerator.MoveNext()`

`IObserver.OnError(Exception)` <-> `IEnumerator.MoveNext()` throws

`IObserver.OnEnded()` <-> `IEnumerator.MoveNext()` returns `false`
***

## Let's get Reactive

Let's try to make this sound generator playable by responding to user input.

---

