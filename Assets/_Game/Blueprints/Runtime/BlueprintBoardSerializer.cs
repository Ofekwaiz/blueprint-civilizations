using System;
using UnityEngine;

namespace BlueprintCivilizations.Blueprints
{
    public sealed class BlueprintSerializationResult
    {
        internal BlueprintSerializationResult(bool success, BlueprintBoardState board, string error)
        {
            Success = success;
            Board = board;
            Error = error ?? "";
        }

        public bool Success { get; }
        public BlueprintBoardState Board { get; }
        public string Error { get; }
    }

    public static class BlueprintBoardSerializer
    {
        public static string Serialize(BlueprintBoardState board, bool prettyPrint = false)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            return JsonUtility.ToJson(board, prettyPrint);
        }

        public static BlueprintSerializationResult TryDeserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new BlueprintSerializationResult(false, null, "Blueprint Board JSON is empty.");
            try
            {
                var board = JsonUtility.FromJson<BlueprintBoardState>(json);
                if (board == null) return new BlueprintSerializationResult(false, null, "Blueprint Board JSON did not contain a board object.");
                board.NormalizeAfterDeserialization();
                return new BlueprintSerializationResult(true, board, "");
            }
            catch (Exception exception) when (exception is ArgumentException || exception is FormatException)
            {
                return new BlueprintSerializationResult(false, null, $"Blueprint Board JSON is invalid: {exception.Message}");
            }
        }
    }

    public interface IBlueprintBoardStorage
    {
        bool TryRead(string key, out string json);
        void Write(string key, string json);
        void Flush();
    }

    /// <summary>Unity restart-safe storage adapter. Save orchestration remains outside the board rules.</summary>
    public sealed class PlayerPrefsBlueprintBoardStorage : IBlueprintBoardStorage
    {
        public bool TryRead(string key, out string json)
        {
            if (!PlayerPrefs.HasKey(key)) { json = ""; return false; }
            json = PlayerPrefs.GetString(key, "");
            return !string.IsNullOrWhiteSpace(json);
        }

        public void Write(string key, string json) => PlayerPrefs.SetString(key, json);
        public void Flush() => PlayerPrefs.Save();
    }

    public sealed class BlueprintBoardPersistenceService
    {
        private readonly IBlueprintBoardStorage storage;
        public BlueprintBoardPersistenceService(IBlueprintBoardStorage storage) => this.storage = storage ?? throw new ArgumentNullException(nameof(storage));

        public void Save(string key, BlueprintBoardState board)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Storage key is required.", nameof(key));
            storage.Write(key, BlueprintBoardSerializer.Serialize(board));
            storage.Flush();
        }

        public BlueprintSerializationResult Load(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return new BlueprintSerializationResult(false, null, "Storage key is required.");
            return storage.TryRead(key, out string json)
                ? BlueprintBoardSerializer.TryDeserialize(json)
                : new BlueprintSerializationResult(false, null, $"No saved Blueprint Board exists for key '{key}'.");
        }

        public IDisposable BindAutoSave(string key, BlueprintPlacementService placement)
        {
            if (placement == null) throw new ArgumentNullException(nameof(placement));
            return new BlueprintBoardAutoSaveBinding(this, key, placement);
        }
    }

    /// <summary>Persists the initial board and every successful command, Undo, or Redo until disposed.</summary>
    public sealed class BlueprintBoardAutoSaveBinding : IDisposable
    {
        private readonly BlueprintBoardPersistenceService persistence;
        private readonly string key;
        private BlueprintPlacementService placement;

        internal BlueprintBoardAutoSaveBinding(BlueprintBoardPersistenceService persistence, string key, BlueprintPlacementService placement)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Storage key is required.", nameof(key));
            this.persistence = persistence;
            this.key = key;
            this.placement = placement;
            placement.EventRaised += OnBoardChanged;
            persistence.Save(key, placement.State);
        }

        public void Dispose()
        {
            if (placement == null) return;
            placement.EventRaised -= OnBoardChanged;
            placement = null;
        }

        private void OnBoardChanged(BlueprintEvent _) => persistence.Save(key, placement.State);
    }
}
