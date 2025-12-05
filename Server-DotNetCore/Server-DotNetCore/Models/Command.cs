namespace Server_DotNetCore.Models;

public enum Command : byte
{
    Auth = 1,
    ListFiles = 2,
    GetFile = 3,
    PutFile = 5,
    Ping = 4,
    FileUploaded = 6,
}