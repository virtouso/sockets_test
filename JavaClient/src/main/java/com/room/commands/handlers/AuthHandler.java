package com.room.commands.handlers;

import com.room.commands.CommandContext;
import com.room.commands.CommandHandler;

import java.io.IOException;

public class AuthHandler implements CommandHandler {
    @Override
    public void execute(CommandContext context, String[] args) throws IOException {
        if (args.length != 3) {
            System.out.println("Usage: auth <username> <password>");
            return;
        }

        context.getProtocolClient().auth(args[1], args[2]);
        context.setAuthenticated(true);
        System.out.println("Authentication successful");
    }
}

