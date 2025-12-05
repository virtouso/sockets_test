package com.room.commands.handlers;

import com.room.commands.CommandContext;
import com.room.commands.CommandHandler;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;

public class SyncFromHandler implements CommandHandler {
    @Override
    public void execute(CommandContext context, String[] args) throws IOException {
        if (!context.isAuthenticated()) {
            System.out.println("Error: You must authenticate first. Use 'auth <username> <password>'");
            return;
        }

        if (args.length != 2) {
            System.out.println("Usage: sync-from <directory>");
            return;
        }

        Path dir = Paths.get(args[1]);
        Files.createDirectories(dir);

        var files = context.getProtocolClient().listFiles();

        for (String filename : files) {
            System.out.println("  Downloading: " + filename);
            context.getProtocolClient().downloadFile(filename, dir.resolve(filename).toString());
        }

        System.out.println("Sync from server completed");
    }
}

