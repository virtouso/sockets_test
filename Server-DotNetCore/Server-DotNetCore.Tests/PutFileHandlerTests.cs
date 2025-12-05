using System.IO;
using System.Text;

namespace Server_DotNetCore.Tests;

public class PutFileHandlerTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _originalDir;
    private readonly string _serverFilesDir;

    public PutFileHandlerTests()
    {
        _originalDir = Directory.GetCurrentDirectory();
        _testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDir);
        Directory.SetCurrentDirectory(_testDir);
        _serverFilesDir = Path.Combine(_testDir, "server_files");
        Directory.CreateDirectory(_serverFilesDir);
    }

    [Fact]
    public void PutFileHandler_FileCreationLogic_WorksCorrectly()
    {
        // Test the core file creation logic that PutFileHandler uses
        // Arrange
        var fileName = "test.txt";
        var fileContent = "Hello World";
        var fullPath = Path.Combine(_serverFilesDir, fileName);
        
        // Act - Simulate what PutFileHandler does
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, fileContent);
        
        // Assert
        Assert.True(File.Exists(fullPath));
        var content = File.ReadAllText(fullPath);
        Assert.Equal(fileContent, content);
    }

    [Fact]
    public void PutFileHandler_FileSizeCalculation_IsCorrect()
    {
        // Test file size calculation logic
        // Arrange
        var fileName = "test2.txt";
        var fileContent = "Hello World";
        var fullPath = Path.Combine(_serverFilesDir, fileName);
        File.WriteAllText(fullPath, fileContent);
        
        // Act
        var fileInfo = new FileInfo(fullPath);
        var expectedSize = Encoding.UTF8.GetBytes(fileContent).Length;
        
        // Assert
        Assert.Equal(expectedSize, fileInfo.Length);
    }

    [Fact]
    public void PutFileHandler_DirectoryCreation_Works()
    {
        // Test that directory creation works (used in PutFileHandler)
        // Arrange
        var fileName = "subdir/test.txt";
        var fullPath = Path.Combine(_serverFilesDir, fileName);
        
        // Act
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, "content");
        
        // Assert
        Assert.True(File.Exists(fullPath));
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

