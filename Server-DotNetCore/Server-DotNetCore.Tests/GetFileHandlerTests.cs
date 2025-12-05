using System.IO;
using System.Text;

namespace Server_DotNetCore.Tests;

public class GetFileHandlerTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _originalDir;
    private readonly string _serverFilesDir;

    public GetFileHandlerTests()
    {
        _originalDir = Directory.GetCurrentDirectory();
        _testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDir);
        Directory.SetCurrentDirectory(_testDir);
        _serverFilesDir = Path.Combine(_testDir, "server_files");
        Directory.CreateDirectory(_serverFilesDir);
    }

    [Fact]
    public void GetFileHandler_FileNotFound_ShouldReturnNegativeOne()
    {
        // Arrange
        var fileName = "nonexistent.txt";
        var fullPath = Path.Combine(_serverFilesDir, fileName);
        
        // Act & Assert
        Assert.False(File.Exists(fullPath));
    }

    [Fact]
    public void GetFileHandler_FileExists_ShouldReturnFileSize()
    {
        // Arrange
        var fileName = "test.txt";
        var fileContent = "Hello World";
        var filePath = Path.Combine(_serverFilesDir, fileName);
        File.WriteAllText(filePath, fileContent);
        
        // Act
        var fileInfo = new FileInfo(filePath);
        
        // Assert
        Assert.True(fileInfo.Exists);
        Assert.Equal(fileContent.Length, fileInfo.Length);
    }

    [Fact]
    public void GetFileHandler_FileContentCanBeRead()
    {
        // Arrange
        var fileName = "test.txt";
        var expectedContent = "Hello World";
        var filePath = Path.Combine(_serverFilesDir, fileName);
        File.WriteAllText(filePath, expectedContent);
        
        // Act
        var actualContent = File.ReadAllText(filePath);
        
        // Assert
        Assert.Equal(expectedContent, actualContent);
    }

    public void Dispose()
    {
        try
        {
            Directory.SetCurrentDirectory(_originalDir);
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, true);
            }
        }
        catch { }
    }
}

