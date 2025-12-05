package com.room.commands;

import com.room.protocol.ProtocolClient;

@FunctionalInterface
public interface CommandHandler {
    void execute(CommandContext context, String[] args) throws Exception;
}

