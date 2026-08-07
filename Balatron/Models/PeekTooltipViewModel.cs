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
        public bool HasOutcome => !string.IsNullOrEmpty(OutcomeText) || OutcomeCards is { Count: > 0 };
    }
}
