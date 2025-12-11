package com.room.commands.handlers;

import com.room.commands.CommandContext;
import com.room.commands.CommandHandler;

import java.io.IOException;

public class ListHandler implements CommandHandler {
    @Override
    public void execute(CommandContext context, String[] args) throws IOException {
        if (!context.isAuthenticated()) {
            System.out.println("Error: You must authenticate first. Use 'auth <username> <password>'");
            return;
        }

        var files = context.getProtocolClient().listFiles();
        System.out.println("Files on server:");
        for (String f : files) {
            System.out.println("  - " + f);
        }
    }
}






