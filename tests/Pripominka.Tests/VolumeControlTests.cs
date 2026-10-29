namespace Pripominka.Tests;

public class VolumeControlTests
{
    [Fact]
    public void Get_returns_level_between_0_and_1() =>
        Assert.InRange(new VolumeControl().Get().Level, 0f, 1f);

    [Fact]
    public void Set_with_current_state_keeps_volume()
    {
        var vc = new VolumeControl();
        var before = vc.Get();
        vc.Set(before);
        var after = vc.Get();
        Assert.Equal(before.Muted, after.Muted);
        Assert.InRange(after.Level, before.Level - 0.01f, before.Level + 0.01f);
    }
}
