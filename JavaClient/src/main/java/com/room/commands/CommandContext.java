package com.room.commands;

import com.room.protocol.ProtocolClient;

public class CommandContext {
    private final ProtocolClient protocolClient;
    private boolean authenticated;

    public CommandContext(ProtocolClient protocolClient) {
        this.protocolClient = protocolClient;
        this.authenticated = false;
    }

    public ProtocolClient getProtocolClient() {
        return protocolClient;
    }

    public boolean isAuthenticated() {
        return authenticated;
    }

    public void setAuthenticated(boolean authenticated) {
        this.authenticated = authenticated;
    }
}

