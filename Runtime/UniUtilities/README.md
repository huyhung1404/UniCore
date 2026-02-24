# UniUtilities

**UniUtilities** is a collection of essential tools, extensions, and design patterns used across the **UniCore** ecosystem to accelerate Unity game development. 

It provides lightweight, high-performance solutions for common problems such as object pooling, date-time manipulation, and component caching.

---

## ✨ Features

* 📦 **Universal Pooling System**
  A high-performance, type-agnostic object pooling system. It can pool anything: C# Classes, Structs, Interfaces, and Unity `GameObjects`/`Components` via customizable `PoolPolicy` definitions.

* ⏱️ **DateTime Utilities**
  Comprehensive tools to handle Unix timestamps, TimeSpan formatting, and timezone-aware calculations for daily resets, cooldowns, and server syncs.

* 🔠 **TextMeshPro Utilities**
  Extensions to manipulate and format `TMP_Text` components efficiently without generating unnecessary garbage.

* 🧩 **Component Utilities**
  Optimized extension methods for `GameObject` and `Component` types to retrieve or add components safely and performantly.

* 🏷️ **Addressable Sub-Assets**
  Helper tools to correctly load and manage sub-assets (like Sprites within a SpriteSheet) when using Unity Addressables.

---

## 🧠 Core Concepts

### Universal Pooling System

Unlike traditional `GameObject` pools, the **Universal Pooling System** can pool pure C# objects and structs.

It relies on a `PoolPolicy<T>` to define how an object is created, retrieved, returned, and destroyed.

**Creating a Pool:**
```csharp
// Get or create a pool for a specific type using its default policy
var bulletPool = PoolManager.GetPool<Bullet>(key: "NormalBullet", init: 20);

// Rent an item
Bullet bullet = bulletPool.Rent();

// Return an item when done
bulletPool.Return(bullet);
```

**Policies Provided:**
* `ClassPolicy<T>`: Uses the default parameterless constructor.
* `StructPolicy<T>`: Creates new structs.
* `InterfacePolicy<T>`: Wraps other policies for interface-based pooling.
* `UnityObjectPolicy<T>`: Instantiates and destroys Unity `UnityEngine.Object` (Prefabs).

---

## 🎯 Design Philosophy

* **Zero-Allocation Ready:** Helper methods and the pooling system are strictly designed to minimize Garbage Collection in hot paths.
* **Modular:** You can use just the pooling system, just the DateTime utilities, or everything together without tight coupling to the rest of the package.
* **Extensible:** The policy-based pooling design ensures you can adapt the pool to custom object lifecycles trivially.
