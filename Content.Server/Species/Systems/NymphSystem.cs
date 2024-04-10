using Content.Server.Cargo.Components;
using Content.Server.Mind;
using Content.Shared.Bank.Components;
using Content.Shared.Species.Components;
using Content.Shared.Body.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Species.Systems;

public sealed partial class NymphSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoManager= default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NymphComponent, RemovedFromPartInBodyEvent>(OnRemovedFromPart);
    }

    private void OnRemovedFromPart(EntityUid uid, NymphComponent comp, RemovedFromPartInBodyEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (TerminatingOrDeleted(uid) || TerminatingOrDeleted(args.OldBody))
            return;

        if (!_protoManager.TryIndex<EntityPrototype>(comp.EntityPrototype, out var entityProto))
            return;

        var coords = Transform(uid).Coordinates;
        var nymph = EntityManager.SpawnAtPosition(entityProto.ID, coords);

        if (comp.TransferMind == true && _mindSystem.TryGetMind(args.OldBody, out var mindId, out var mind))
        {
            _mindSystem.TransferTo(mindId, nymph, mind: mind);


            // Frontier
            EnsureComp<CargoSellBlacklistComponent>(nymph);

            // Frontier
            // bank account transfer
            if (TryComp<BankAccountComponent>(args.OldBody, out var bank))
            {
                // Do this carefully since changing the value of a bank account component on a entity will save the balance immediately through subscribers.
                var oldBankBalance = bank.Balance;
                var newBank = EnsureComp<BankAccountComponent>(nymph);
                newBank.Balance = oldBankBalance;
            }
        }

        QueueDel(uid);
    }
}
