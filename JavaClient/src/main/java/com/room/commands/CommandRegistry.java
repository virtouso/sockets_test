package com.room.commands;

import com.room.commands.handlers.*;

import java.util.HashMap;
import java.util.Map;

public class CommandRegistry {
    private final Map<String, CommandHandler> handlers;

    public CommandRegistry() {
        this.handlers = new HashMap<>();
        registerHandlers();
    }

    private void registerHandlers() {
        register("help", new HelpHandler());
        register("auth", new AuthHandler());
        register("list", new ListHandler());
        register("download", new DownloadHandler());
        register("upload", new UploadHandler());
        register("ping", new PingHandler());
        register("quit", new QuitHandler());
        register("exit", new QuitHandler());
    }

    public void register(String command, CommandHandler handler) {
        handlers.put(command, handler);
    }

    public CommandHandler get(String command) {
        return handlers.get(command);
    }

    public boolean hasCommand(String command) {
        return handlers.containsKey(command);
    }
}

