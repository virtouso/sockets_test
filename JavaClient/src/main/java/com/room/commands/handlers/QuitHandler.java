package com.room.commands.handlers;

import com.room.commands.CommandContext;
import com.room.commands.CommandHandler;

public class QuitHandler implements CommandHandler {
    @Override
    public void execute(CommandContext context, String[] args) {
        System.out.println("Goodbye!");
        System.exit(0);
    }
}

