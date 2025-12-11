package com.room.commands.handlers;

import com.room.commands.CommandContext;
import com.room.commands.CommandHandler;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;

public class SyncToHandler implements CommandHandler {
    @Override
    public void execute(CommandContext context, String[] args) throws IOException {
        if (!context.isAuthenticated()) {
            System.out.println("Error: You must authenticate first. Use 'auth <username> <password>'");
            return;
        }

        if (args.length != 2) {
            System.out.println("Usage: sync-to <directory>");
            return;
        }

        Path dir = Paths.get(args[1]);
        if (!Files.isDirectory(dir)) {
            throw new IOException("Not a directory: " + args[1]);
        }

        Files.list(dir).forEach(path -> {
            if (Files.isRegularFile(path)) {
                try {
                    System.out.println("  Uploading: " + path.getFileName());
                    context.getProtocolClient().uploadFile(path.toString());
                } catch (IOException e) {
                    throw new RuntimeException("Failed to upload " + path.getFileName() + ": " + e.getMessage(), e);
                }
            }
        });

        System.out.println("Sync to server completed");
    }
}




