using System;
using System.Collections.Generic;

namespace EpicLootSpawner
{
    // Immutable serialized drafts also retain invalid text, so Undo can repair an unfinished field.
    internal sealed class DesignHistory
    {
        private readonly List<string> entries = new List<string>();
        private readonly int capacity;
        private int position = -1;
        public bool CanUndo => position > 0;
        public bool CanRedo => position >= 0 && position < entries.Count - 1;
        public string Current => position >= 0 ? entries[position] : null;
        public IEnumerable<string> Snapshots => entries;
        public DesignHistory(int capacity = 60) { this.capacity = Math.Max(2, capacity); }
        public void Record(string draft)
        {
            if (draft == Current) return;
            if (CanRedo) entries.RemoveRange(position + 1, entries.Count - position - 1);
            entries.Add(draft);
            if (entries.Count > capacity) entries.RemoveAt(0);
            position = entries.Count - 1;
        }
        public string PeekUndo() => CanUndo ? entries[position - 1] : Current;
        public string PeekRedo() => CanRedo ? entries[position + 1] : Current;
        public void Undo() { if (CanUndo) position--; }
        public void Redo() { if (CanRedo) position++; }
        public void Clear() { entries.Clear(); position = -1; }
    }
}
