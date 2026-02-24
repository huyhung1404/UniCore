# UniAudio

**UniAudio** is a robust, lightweight, and high-performance audio management system for Unity, built on top of **UniSignal**, **UniTask**, and **Addressables**.

It provides a purely signal-driven API, asynchronous asset loading, and zero-allocation object pooling, ensuring minimal overhead and scalable audio playback for both small and large projects.

---

## ✨ Features

* 📡 **Signal-Driven API**
  Play, stop, or change sounds by dispatching simple data structs (`ISignalEvent`). No tight coupling or singletons required in user code.

* 🚀 **Zero-Allocation Object Pooling**
  `SoundEmitter` components are safely pooled and reused, eliminating the need to instantiate new GameObjects or AudioSources during gameplay.

* ⏳ **Asynchronous Loading**
  Audio clips, configs, and node data are loaded smoothly on-demand via **Addressables** and **UniTask**, preventing CPU spikes and frame drops.

* 🎛️ **Audio Mixer Integration**
  Built-in support for `MasterVolume`, `MusicVolume`, and `SFXVolume` tied to Unity's AudioMixer.

* ⚙️ **Data-Driven Configuration**
  Define `AudioConfiguration` and audio node graphs (`BaseAudioNode`) as ScriptableObjects to decouple sound logic from code.

---

## 🧠 Core Concepts

### Signal Control

All audio interactions are performed by dispatching signals to the `AudioSystem`.

```csharp
// Play a sound
SignalBus.Dispatch(new PlaySoundSignal
{
    clip = "SFX_Click", // Addressable key
    config = "DefaultSFX", // Addressable key
    position = transform.position,
    soundId = 100 // Optional: ID for later control
});

// Stop a specific sound
SignalBus.Dispatch(new StopSoundSignal { soundId = 100 });

// Change sound dynamically with a fade transition
SignalBus.Dispatch(new ChangeSoundSignal { soundId = 100, clip = "SFX_Hover" });
```

### Volume Management

Volume control is also managed via signals and persists using `PlayerPrefs`:

```csharp
SignalBus.Dispatch(new ChangeMasterVolumeSignal { volume = 0.8f });
SignalBus.Dispatch(new ChangeMusicVolumeSignal { volume = 0.5f });
SignalBus.Dispatch(new ChangeSFXVolumeSignal { volume = 1.0f });
```

---

## 🎮 Setup & Usage

### 1. Requirements

Make sure your project has the following dependencies configured:
* `com.unity.addressables`
* `com.cysharp.unitask`
* `com.huyhung1404.unicore` (UniSignal & UniAudio modules)

The package relies on the scripting define symbols `HAS_UNITASK` and `HAS_ADDRESSABLES`.

### 2. AudioSystem Initialization

`AudioSystem` initializes automatically before the scene loads using `[RuntimeInitializeOnLoadMethod]`, creating a `DontDestroyOnLoad` GameObject to handle audio for the entire application lifespan.

### 3. AudioSettings

Create an `AudioSettings` ScriptableObject in a `Resources` folder to configure:
* The default `AudioMixer` 
* The default pool initial size
* The Addressables group address for your audio assets.

---

## 🐞 Addressables Setup

UniAudio relies on standard Addressables strings to resolve clips and configs.
Your audio assets must be labeled and placed in an Addressable group matching your `AudioSettings.GroupAddress`.

* `Nodes/`: Addressable path for `BaseAudioNode` data.
* `Configs/`: Addressable path for `AudioConfiguration`.

---

## 🎯 Design Philosophy

UniAudio is built on the following principles:

* **Separation of Concerns:** Gameplay code just fires signals; it doesn't care how the audio is played.
* **Performance:** Rely on Addressables and UniTask to offload heavy loading, and Pooling to eliminate garbage collection.
* **Scalability:** The node-based struct allows complex randomization and sequenced audio behaviors entirely in data.
