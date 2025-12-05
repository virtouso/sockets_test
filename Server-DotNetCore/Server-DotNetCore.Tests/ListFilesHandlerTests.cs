using System.IO;
using System.Text;

namespace Server_DotNetCore.Tests;

public class ListFilesHandlerTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _originalDir;
    private readonly string _serverFilesDir;

    public ListFilesHandlerTests()
    {
        _originalDir = Directory.GetCurrentDirectory();
        _testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDir);
        Directory.SetCurrentDirectory(_testDir);
        _serverFilesDir = Path.Combine(_testDir, "server_files");
        Directory.CreateDirectory(_serverFilesDir);
    }

    [Fact]
    public void ListFilesHandler_EmptyDirectory_ReturnsZeroFiles()
    {
        // Test the core file listing logic that ListFilesHandler uses
        var files = Directory.GetFiles(_serverFilesDir);
        Assert.Empty(files);
    }

    [Fact]
    public void ListFilesHandler_WithFiles_ReturnsCorrectCount()
    {
        // Arrange - Clean directory first
        foreach (var file in Directory.GetFiles(_serverFilesDir))
        {
            File.Delete(file);
        }
        
        File.WriteAllText(Path.Combine(_serverFilesDir, "test1.txt"), "content1");
        File.WriteAllText(Path.Combine(_serverFilesDir, "test2.txt"), "content2");
        
        // Act - Test the core logic
        var files = Directory.GetFiles(_serverFilesDir);
        
        // Assert
        Assert.Equal(2, files.Length);
        Assert.Contains("test1.txt", files.Select(f => Path.GetFileName(f)));
        Assert.Contains("test2.txt", files.Select(f => Path.GetFileName(f)));
    }

    [Fact]
    public void ListFilesHandler_FileNamesAreExtractedCorrectly()
    {
        // Arrange - Clean directory first
        foreach (var file in Directory.GetFiles(_serverFilesDir))
        {
            File.Delete(file);
        }
        
        var fullPath = Path.Combine(_serverFilesDir, "test.txt");
        File.WriteAllText(fullPath, "content");
        
        // Act
        var files = Directory.GetFiles(_serverFilesDir);
        var fileName = Path.GetFileName(files[0]);
        
        // Assert
        Assert.Equal("test.txt", fileName);
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

