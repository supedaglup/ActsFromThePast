using System.Collections.Generic;
using ActsFromThePast.Acts;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class GuardianBoss : CustomEncounterModel
{
    public GuardianBoss() : base(RoomType.Boss)
    {
    }

    public override bool IsValidForAct(ActModel act) => act is ExordiumAct;

    /// <summary>Dread Mode: the effect this boss spreads to every other fight in its act.</summary>
    public override MegaCrit.Sts2.Core.Models.Dread.DominionModel? GetDominion() => ModelDb.Dominion<ActsFromThePast.Dread.AftpGuardianDominion>();

    public override string BossNodePath => "res://ActsFromThePast/map_boss_icons/guardian";

    public override IEnumerable<MonsterModel> AllPossibleMonsters
    {
        get
        {
            return new List<MonsterModel>
            {
                ModelDb.Monster<Guardian>()
            };
        }
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<Guardian>().ToMutable(), null)
        };
    }
}