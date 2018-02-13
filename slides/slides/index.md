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

' NAudio needs a callback
' You do this by implementing the `IWaveProvider` interface
' Tells NAudio about the structure of the data you'll be giving it.
' Asks you to fill a buffer when more samples are needed
' snippet aud1

