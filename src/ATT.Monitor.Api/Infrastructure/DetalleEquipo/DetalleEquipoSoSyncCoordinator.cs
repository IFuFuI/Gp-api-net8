using System.Collections.Concurrent;

namespace ATT.Monitor.Api.Infrastructure.DetalleEquipo;

public sealed class DetalleEquipoSoSyncCoordinator : IDetalleEquipoSoSyncCoordinator
{
    private readonly object _lock = new();
    private readonly ConcurrentDictionary<string, Entry> _byKey = new();

    private sealed class Entry
    {
        public required string IdAtm { get; init; }
        public required Guid IdSesion { get; init; }
        public DetalleEquipoSoSyncUiState State { get; set; }
        public DateTimeOffset RequestedUtc { get; init; }
        public DateTimeOffset? InFlightUtc { get; set; }
        public DateTimeOffset? CompletedUtc { get; set; }
    }

    private static string Key(string idAtm, Guid idSesion) =>
        $"{idAtm.Trim().ToUpperInvariant()}|{idSesion:N}";

    public DetalleEquipoSoSyncRegisterResult RegisterRequest(string idAtm, Guid idSesion)
    {
        if (string.IsNullOrWhiteSpace(idAtm) || idSesion == Guid.Empty)
            return new DetalleEquipoSoSyncRegisterResult { Kind = DetalleEquipoSoSyncRegisterKind.AlreadyQueued };

        idAtm = idAtm.Trim();
        NormalizeStaleUnsafe();

        var key = Key(idAtm, idSesion);
        lock (_lock)
        {
            NormalizeStaleUnsafe();
            if (_byKey.TryGetValue(key, out var existing))
            {
                if (existing.State == DetalleEquipoSoSyncUiState.Completed)
                    return new DetalleEquipoSoSyncRegisterResult { Kind = DetalleEquipoSoSyncRegisterKind.AlreadyCompleted };
                return new DetalleEquipoSoSyncRegisterResult { Kind = DetalleEquipoSoSyncRegisterKind.AlreadyQueued };
            }

            _byKey[key] = new Entry
            {
                IdAtm = idAtm,
                IdSesion = idSesion,
                State = DetalleEquipoSoSyncUiState.Pending,
                RequestedUtc = DateTimeOffset.UtcNow
            };
            return new DetalleEquipoSoSyncRegisterResult { Kind = DetalleEquipoSoSyncRegisterKind.NewlyQueued };
        }
    }

    public DetalleEquipoSoSyncStatusDto GetStatus(string idAtm, Guid idSesion)
    {
        if (string.IsNullOrWhiteSpace(idAtm) || idSesion == Guid.Empty)
            return new DetalleEquipoSoSyncStatusDto { State = DetalleEquipoSoSyncUiState.Unknown };

        var key = Key(idAtm.Trim(), idSesion);
        lock (_lock)
        {
            NormalizeStaleUnsafe();
            if (!_byKey.TryGetValue(key, out var e))
                return new DetalleEquipoSoSyncStatusDto { State = DetalleEquipoSoSyncUiState.Unknown };
            return new DetalleEquipoSoSyncStatusDto { State = e.State };
        }
    }

    public DetalleEquipoSoSyncClaimed? TryClaimNext(string idAtm)
    {
        if (string.IsNullOrWhiteSpace(idAtm))
            return null;

        idAtm = idAtm.Trim();
        lock (_lock)
        {
            NormalizeStaleUnsafe();
            Entry? best = null;
            string? bestKey = null;
            foreach (var kv in _byKey)
            {
                var e = kv.Value;
                if (!string.Equals(e.IdAtm, idAtm, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (e.State != DetalleEquipoSoSyncUiState.Pending)
                    continue;
                if (best == null || e.RequestedUtc < best.RequestedUtc)
                {
                    best = e;
                    bestKey = kv.Key;
                }
            }

            if (bestKey == null || best == null)
                return null;

            best.State = DetalleEquipoSoSyncUiState.InFlight;
            best.InFlightUtc = DateTimeOffset.UtcNow;
            return new DetalleEquipoSoSyncClaimed(best.IdSesion, best.IdAtm);
        }
    }

    public void MarkSubmitSuccess(string idAtm, Guid idSesion)
    {
        if (string.IsNullOrWhiteSpace(idAtm) || idSesion == Guid.Empty)
            return;

        var key = Key(idAtm.Trim(), idSesion);
        lock (_lock)
        {
            if (!_byKey.TryGetValue(key, out var e))
                return;
            if (e.State != DetalleEquipoSoSyncUiState.InFlight)
                return;
            e.State = DetalleEquipoSoSyncUiState.Completed;
            e.CompletedUtc = DateTimeOffset.UtcNow;
        }
    }

    public void MarkSubmitFailure(string idAtm, Guid idSesion)
    {
        if (string.IsNullOrWhiteSpace(idAtm) || idSesion == Guid.Empty)
            return;

        var key = Key(idAtm.Trim(), idSesion);
        lock (_lock)
        {
            if (!_byKey.TryGetValue(key, out var e))
                return;
            if (e.State != DetalleEquipoSoSyncUiState.InFlight)
                return;
            e.State = DetalleEquipoSoSyncUiState.Pending;
            e.InFlightUtc = null;
        }
    }

    public void AbandonSession(Guid idSesion)
    {
        if (idSesion == Guid.Empty)
            return;

        lock (_lock)
        {
            foreach (var kv in _byKey.ToArray())
            {
                if (kv.Value.IdSesion == idSesion)
                    _byKey.TryRemove(kv.Key, out _);
            }
        }
    }

    private void NormalizeStaleUnsafe()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var kv in _byKey.ToArray())
        {
            var e = kv.Value;
            if (e.State == DetalleEquipoSoSyncUiState.InFlight
                && e.InFlightUtc.HasValue
                && (now - e.InFlightUtc.Value).TotalMinutes > 12)
            {
                e.State = DetalleEquipoSoSyncUiState.Pending;
                e.InFlightUtc = null;
            }

            if (e.State == DetalleEquipoSoSyncUiState.Completed
                && e.CompletedUtc.HasValue
                && (now - e.CompletedUtc.Value).TotalHours > 2)
            {
                _byKey.TryRemove(kv.Key, out _);
            }
        }
    }
}
