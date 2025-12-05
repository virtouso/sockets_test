using Server_DotNetCore.Models;

namespace Server_DotNetCore.Tests;

public class CommandTests
{
    [Fact]
    public void Command_ShouldHaveCorrectValues()
    {
        // test to dont change code ids by mistake
        Assert.Equal((byte)1, (byte)Command.Auth);
        Assert.Equal((byte)2, (byte)Command.ListFiles);
        Assert.Equal((byte)3, (byte)Command.GetFile);
        Assert.Equal((byte)4, (byte)Command.Ping);
        Assert.Equal((byte)5, (byte)Command.PutFile);
        Assert.Equal((byte)6, (byte)Command.FileUploaded);
    }

    [Fact]
    public void Command_ValuesShouldBeUnique()
    {
        // to make sure command codes are uniqe
        var values = Enum.GetValues<Command>().Select(c => (byte)c).ToList();
        Assert.Equal(values.Count, values.Distinct().Count());
    }
}

