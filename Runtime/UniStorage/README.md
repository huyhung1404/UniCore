# UniStorage

**UniStorage** is a modular, pipeline-based data storage system for Unity, designed to securely save and load player data, configurations, and game progression.

It separates concerns—serialization, encryption, protection, and storage medium—allowing you to easily swap implementations without changing gameplay code.

---

## ✨ Features

* 🧩 **Modular Pipeline Architecture**
  Storage operations are divided into distinct steps: `Serialize` -> `Encrypt` -> `Protect` -> `Storage`. Load operations perform the inverse.

* 🛡️ **Built-in Security**
  Interfaces for `IEncryptor` and `IProtector` allow you to implement AES encryption, hash validation, or anti-tamper mechanisms easily.

* ♻️ **Data Versioning**
  Built-in support for data migrations. If the loaded data version differs from the current configuration, it triggers an `OnVersionChanged` event, allowing you to run upgrade logic.

* ⚙️ **Data-Driven Settings**
  Configure the pipeline and its implementations entirely via a `StorageSettings` ScriptableObject.

---

## 🧠 Core Concepts

### Storage Pipeline

The core mechanism of UniStorage is its pipeline. When you save an object:
1. **Serializer:** Converts the C# object to a `byte[]` (e.g., using `JsonSerializer`).
2. **Encryptor:** Encrypts the `byte[]` using a cryptographic key.
3. **Protector:** Adds checksums or signatures to prevent tampering.
4. **Storage Provider:** Writes the resultant `byte[]` to the destination (e.g., Local File System, Cloud, or PlayerPrefs).

Loading performs these steps in reverse asynchronously or synchronously depending on the provider.

### ISettings and Implementations

The system is configured via `StorageSettings.asset`, which implements `ISettings` and holds references to:
- `ISerializer`
- `IEncryptor`
- `IProtector`
- `IStorageProvider`
- `IKey` (to generate/provide encryption keys)

---

## 🎮 Setup & Usage

### 1. Initialization

UniStorage can automatically initialize itself by loading `StorageSettings.asset` from a `Resources` folder, or you can initialize it manually:

```csharp
// Manual initialization with custom settings
StorageSystem.SetSettings(myCustomSettings);
```

By default, if no settings are found or provided, it falls back to:
* `JsonSerializer`
* `NoEncryptor`
* `NoProtector`
* `LocalStorage`

### 2. Saving and Loading Data

Use the static `StorageSystem` API to save and load data models.

```csharp
// Define your data model
[System.Serializable]
public class PlayerProfile
{
    public string playerName;
    public int coins;
    public int level;
}

// Save data
var myProfile = new PlayerProfile { playerName = "Hero", coins = 500, level = 10 };
StorageSystem.Save("player_profile.dat", myProfile);

// Load data
var loadedProfile = StorageSystem.Load<PlayerProfile>("player_profile.dat");
```

### 3. Handling Version Changes

If you update the `version` field in your `StorageSettings`, you can handle data migrations when old data is loaded:

```csharp
void OnEnable()
{
    StorageSystem.OnVersionChanged += HandleDataMigration;
}

void OnDisable()
{
    StorageSystem.OnVersionChanged -= HandleDataMigration;
}

void HandleDataMigration(object data, int oldVersion, int newVersion)
{
    if (data is PlayerProfile profile)
    {
        Debug.Log($"Upgrading profile from {oldVersion} to {newVersion}");
        // Perform data structural migrations here
    }
}
```

---

## 🎯 Design Philosophy

* **Flexibility:** Don't lock the project into a specific JSON library or encryption algorithm. Define the interfaces and let the project decide.
* **Security:** Player data tempering is a common issue. Having native steps for Encryption and Protection standardizes secure save files.
* **Simplicity for the User:** Gameplay systems simply call `StorageSystem.Save<T>()` and don't care about paths, keys, or formats.
