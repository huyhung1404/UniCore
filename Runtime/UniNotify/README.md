# UniNotify

**UniNotify** is a lightweight notification system for Unity, primarily featuring **DotPing**, a hierarchical "red dot" badge system designed for menus, inventories, and UI navigation in games.

It provides easy state management, auto-propagation of badges through UI hierarchies, and persistent saving via JSON.

---

## ✨ Features

* 🔴 **Hierarchical Dot Pings**
  Pushing a ping to `Shop/Weapons/Sword` automatically highlights `Shop/Weapons` and `Shop`.

* 💾 **Persistent State**
  Ping data is automatically saved to `PlayerPrefs` using `Newtonsoft.Json`, ensuring players don't lose their unread statuses between sessions.

* 🧩 **Zero-Setup UI Integration**
  Simply attach the `Ping` component to a UI element and assign a `locationId`. The system handles the instantiation and pooling of the ping graphics.

* 📈 **Multiple Graphic Types**
  Supports different visual indicators (e.g., standard `Dot` or `Upgrade` icons).

---

## 🧠 Core Concepts

### PingSystem

The `PingSystem` is the core manager. It should exist in your first scene. It automatically handles data loading and saving for mobile and editor environments on application pause/quit.

### Locations and Hierarchy

Locations are defined by string paths separated by slashes (`/`).

```csharp
// Push a ping
PingSystem.Push("Shop/Offers/DailySale");

// Check if a ping is active
bool hasSale = PingSystem.IsActive("Shop/Offers/DailySale");

// Because "Shop/Offers/DailySale" is active, its parents are automatically active too:
bool isShopActive = PingSystem.IsActive("Shop"); // Returns true
```

### Popping Pings

When a player views or claims a notification, you pop the ping.

```csharp
// Pop a single instance
PingSystem.Pop("Shop/Offers/DailySale");

// Force hide all instances under this ID
PingSystem.Pop("Shop/Offers/DailySale", forceHide: true);
```

---

## 🎮 Setup & Usage

### 1. Prefab Configuration

Specify the `PingGraphic` prefabs (like your red dot image) in the `PingSystem` component in your scene setting. Add `PingSystem` to a GameObject that will survive across scenes (it uses `DontDestroyOnLoad`).

### 2. UI Setup

For any UI Button or tab that needs a red dot:
1. Attach the `Ping` component to the GameObject.
2. Set the `locationId` (e.g., `Shop/Offers/DailySale`).
3. Set the `graphicType` Enum to the desired visual style.

When `PingSystem.Push("Shop/Offers/DailySale")` is called anywhere in your code, the `Ping` component will automatically spawn or enable the red dot graphic as a child. When `Pop()` is called, it will return the graphic to the pool.

---

## 🎯 Design Philosophy

* **Decoupled Data and View:** Pings are managed abstractly by IDs. The UI independently listens and updates without needing explicit references to the systems generating the pings.
* **Effortless Propagation:** Dealing with tree-like UI menus is complex. UniNotify aims to make this seamless by automatically bubbling up notifications to parent IDs.
