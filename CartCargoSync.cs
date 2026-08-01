using System;
using System.Collections.Generic;
using System.Text;

namespace BetterCarts;

// the ONE entity parameter Better Carts writes. Vanilla c1..c5 and Iron Cart c6..c8 are one key per slot; a single packed key keeps the save footprint to one entry per Cart and cannot collide with a future mod's cN
internal static class CartCargoSync {
    internal const string CargoKey = "bc_cargo";
    internal const int MaxExtras = 20;

    private const char Separator = ',';

    internal static string Pack(List<Guid> extras) {
        if (extras == null || extras.Count == 0) {
            return string.Empty;
        }
        StringBuilder builder = new StringBuilder();
        foreach (Guid id in extras) {
            if (builder.Length > 0) {
                builder.Append(Separator);
            }
            builder.Append(id.ToString());
        }
        return builder.ToString();
    }

    internal static void Unpack(string raw, List<Guid> into) {
        into.Clear();
        if (string.IsNullOrEmpty(raw)) {
            return;
        }
        foreach (string part in raw.Split(Separator)) {
            if (part.Length != 0 && Guid.TryParse(part, out Guid id) && !into.Contains(id)) {
                into.Add(id);
            }
        }
    }
}
