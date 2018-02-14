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

***

## More complex signals

---

### Sinewave

We need to introduce _frequency_ (f) - how often the waveform repeats per second

To generate a repeating waveform we also need _phase_ - conventionally measured as an angle between 0 to Two PI radians

Phase revolves every 1/f seconds

The change in phase from one sample to the next we will call _delta_

The current _phase angle_ will be called _theta_

The amplitude is calculated as _sin theta_

---

## Higher order functions

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

We can refactor the code duplication to a higher-order function that takes our generator function as an argument

If we make the generator function the first argument we can use partial application to generate a family of sound generator functions



