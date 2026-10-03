namespace Course.PartyControl.Tests;

public class SkillKeyTests
{
    [Fact]
    public void TryUseSkill_KeyOnSecondRow_CastsSecondCharactersSkillWhileFirstIsLeader()
    {
        var party = TestParties.Create();

        Assert.True(party.TryUseSkill('s', out var cast)); // row ASDF, second key -> slot 1
        Assert.Equal(new SkillCast(1, "A2"), cast);
        Assert.Equal(0, party.LeaderIndex);
    }

    [Fact]
    public void TryUseSkill_SameKeyAfterLeaderSwitch_StillBelongsToTheSameCharacter()
    {
        var party = TestParties.Create();
        party.TrySwitchLeader(2);

        Assert.True(party.TryUseSkill('Q', out var cast));
        Assert.Equal(new SkillCast(0, "Q1"), cast);
    }

    [Fact]
    public void TryUseSkill_UnboundKey_ReturnsFalse()
    {
        var party = TestParties.Create();

        Assert.False(party.TryUseSkill('P', out _));
    }

    [Fact]
    public void TryUseSkill_EmptySlot_ReturnsFalse()
    {
        var party = TestParties.Create();
        party.Characters[0].SkillSlots[0] = null;

        Assert.False(party.TryUseSkill('Q', out _));
    }

    [Fact]
    public void TryUseSkill_CharacterOutOfTheLineUp_ReturnsFalse()
    {
        var party = TestParties.Create();
        party.Characters[2].Active = false;

        Assert.False(party.TryUseSkill('Z', out _));
    }

    [Fact]
    public void Bind_RebindsAKeyWithoutCodeChanges()
    {
        var party = TestParties.Create();
        party.Keys.Bind('1', 2, 3);

        Assert.True(party.TryUseSkill('1', out var cast));
        Assert.Equal(new SkillCast(2, "Z4"), cast);
    }
}
