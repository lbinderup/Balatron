using System.Collections.Generic;

namespace Balatron.Models
{
    /// <summary>
    /// Content of the themed hover popup: name, effect text, and — for
    /// consumables with random effects — the concrete outcome of using the
    /// card right now, rendered as real cards.
    /// </summary>
    public sealed class PeekTooltipViewModel
    {
        public string Title { get; init; }
        public string Subtitle { get; init; }
        public string Body { get; init; }
        public string OutcomeText { get; init; }
        public IReadOnlyList<PeekCardViewModel> OutcomeCards { get; init; }

        /// <summary>Cards the effect destroys — rendered crossed out, ahead of an "=".</summary>
        public IReadOnlyList<PeekCardViewModel> DestroyedCards { get; init; }

        public bool HasDestroyed => DestroyedCards is { Count: > 0 };
        public bool HasCreated => OutcomeCards is { Count: > 0 };

        /// <summary>Only show the "=" when destruction actually yields something.</summary>
        public bool ShowsEquals => HasDestroyed && HasCreated;

        public bool HasOutcome => !string.IsNullOrEmpty(OutcomeText) || HasCreated || HasDestroyed;
    }
}
