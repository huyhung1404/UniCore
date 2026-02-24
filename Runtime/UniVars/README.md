# UniVars

**UniVars** is a lightweight, reactive global variable store for Unity, built on top of the **UniSignal** event bus system.

It provides a decoupled way to define, store, and listen to state changes across entirely distinct game systems or UI hierarchies without heavy dependencies.

---

## ✨ Features

* 🌍 **Global State Store**
  Avoid injecting dependencies or creating complex Singleton managers just to share simple states (e.g., player health, current score).

* ⚡ **Reactive Variables**
  When a variable's value changes, `UniVars` automatically dispatches a `VariableChangedSignal<T>` through the `SignalSystem`, allowing any system to react instantly.

* 🛡️ **Type-Safe Access**
  Variables are defined and fetched using strong typing, ensuring compile-time safety.

---

## 🧠 Core Concepts

### VarsSystem

The entry point to global variables is `VarsSystem.Global`, which provides a global instantiation of a `VariableStore`.

### VariableStore

A dictionary-like container that holds `Variable<T>` instances mapped to string keys. 

### Variable<T>

A wrapper around a generic type `T`. It overrides standard assignment using a `Set(T)` method. Setting a value that differs from the current value triggers a `VariableChangedSignal<T>`.

---

## 🎮 Setup & Usage

### 1. Defining and Setting Variables

You can define variables anywhere in your code. Good practice is to define them during initialization.

```csharp
// Define a 'Score' variable with an initial value of 0.
var scoreVar = VarsSystem.Global.Define("Score", 0);

// Later in gameplay...
scoreVar.Set(10); // Dispatches a VariableChangedSignal<int>
```

Alternatively, you can fetch an already defined variable:

```csharp
var scoreVar = VarsSystem.Global.Get<int>("Score");
if (scoreVar != null)
{
    scoreVar.Set(50);
}
```

### 2. Listening to Variable Changes

Because `UniVars` integrates tightly with `UniSignal`, you can listen to changes by implementing `ISignalListener<VariableChangedSignal<T>>`.

```csharp
using UniCore.Signal;
using UniCore.Vars;
using UnityEngine;

public class ScoreUI : MonoBehaviour, ISignalListener<VariableChangedSignal<int>>
{
    private void OnEnable()
    {
        SignalSystem.Register(this);
    }

    private void OnDisable()
    {
        SignalSystem.Unregister(this);
    }

    public void OnSignal(VariableChangedSignal<int> signal)
    {
        // Check if the changed variable is the one we care about
        if (signal.Key == "Score")
        {
            Debug.Log($"Score changed from {signal.OldValue} to {signal.NewValue}");
        }
    }
}
```

---

## 🎯 Design Philosophy

* **Decoupled Architecture:** Using UniVars prevents the "spaghetti code" problem where UI needs direct references to the Player object just to read health or score.
* **Driven by UniSignal:** Relying on the zero-allocation `UniSignal` package ensures that responding to state changes generates no garbage, maintaining smooth framerates for your project.
