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
        for (String filename : serverFiles) {
            if (!localFiles.contains(filename)) {
                System.out.println("  Downloading: " + filename);
                Path savePath = clientDir.resolve(filename);
                context.getProtocolClient().downloadFile(filename, savePath.toString());
                downloaded++;
            } else {
                System.out.println("  Skipping (already exists): " + filename);
            }
        }

        if (downloaded == 0) {
            System.out.println("All files are up to date");
        } else {
            System.out.println("Downloaded " + downloaded + " file(s)");
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

