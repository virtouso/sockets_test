package com.room.commands.handlers;

import com.room.commands.CommandContext;
import com.room.commands.CommandHandler;
import com.room.config.ClientConfig;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.HashSet;
import java.util.List;
import java.util.Set;

public class DownloadHandler implements CommandHandler {
    @Override
    public void execute(CommandContext context, String[] args) throws IOException {
        if (!context.isAuthenticated()) {
            System.out.println("Error: You must authenticate first. Use 'auth <username> <password>'");
            return;
        }

        Path clientDir = Paths.get(ClientConfig.getClientFilesDir());
        Files.createDirectories(clientDir);

        List<String> serverFiles = context.getProtocolClient().listFiles();
        Set<String> localFiles = getLocalFiles(clientDir);

        System.out.println("Server has " + serverFiles.size() + " file(s)");
        System.out.println("Local has " + localFiles.size() + " file(s)");

        int downloaded = 0;
        int failed = 0;
        final int maxRetries = 3;
        
        for (String filename : serverFiles) {
            if (!localFiles.contains(filename)) {
                System.out.println("  Downloading: " + filename);
                Path savePath = clientDir.resolve(filename);
                
                boolean success = false;
                IOException lastError = null;
                for (int attempt = 1; attempt <= maxRetries; attempt++) {
                    try {
                        context.getProtocolClient().downloadFile(filename, savePath.toString());
                        downloaded++;
                        success = true;
                        break;
                    } catch (IOException e) {
                        lastError = e;
                        if (attempt < maxRetries) {
                            System.out.println("    Retry " + attempt + "/" + (maxRetries - 1) + " for " + filename + "...");
                        }
                    }
                }
                
                if (!success) {
                    System.out.println("  Warning: Failed to download " + filename + " after " + maxRetries + " attempts: " + lastError.getMessage());
                    failed++;
                }
            } else {
                System.out.println("  Skipping (already exists): " + filename);
            }
        }

        if (downloaded == 0 && failed == 0) {
            System.out.println("All files are up to date");
        } else if (downloaded > 0 && failed == 0) {
            System.out.println("Downloaded " + downloaded + " file(s)");
        } else if (downloaded > 0 && failed > 0) {
            System.out.println("Downloaded " + downloaded + " file(s), failed " + failed + " file(s)");
        } else {
            System.out.println("Failed to download " + failed + " file(s)");
        }
    }

    private Set<String> getLocalFiles(Path dir) throws IOException {
        Set<String> files = new HashSet<>();
        if (!Files.exists(dir)) {
            return files;
        }
        Files.list(dir).forEach(path -> {
            if (Files.isRegularFile(path)) {
                files.add(path.getFileName().toString());
            }
        });
        return files;
    }
}

