package com.room.commands.handlers;

import com.room.commands.CommandContext;
import com.room.commands.CommandHandler;

public class HelpHandler implements CommandHandler {
    @Override
    public void execute(CommandContext context, String[] args) {
        System.out.println("Available commands:");
        System.out.println("  auth <username> <password>  - Authenticate with server");
        System.out.println("  list                       - List files on server");
        System.out.println("  download <filename>         - Download a file from server");
        System.out.println("  upload <filename>           - Upload a file to server");
        System.out.println("  ping                        - Ping server");
        System.out.println("  help                        - Show this help");
        System.out.println("  quit/exit                   - Exit client");
    }
}

