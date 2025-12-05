package com.room.client;

import com.room.commands.CommandContext;
import com.room.commands.CommandRegistry;
import com.room.protocol.ProtocolClient;
import com.room.commands.handlers.DownloadHandler;

import java.io.BufferedInputStream;
import java.io.InputStream;
import java.io.OutputStream;
import java.util.Scanner;

public class Client {
    private final CommandRegistry commandRegistry;
    private final CommandContext context;
    private final ProtocolClient protocolClient;

    public Client(InputStream in, OutputStream out) {
        this.commandRegistry = new CommandRegistry();
        // Wrap input stream with BufferedInputStream to support mark/reset for notification listening
        BufferedInputStream bufferedIn = new BufferedInputStream(in, 8192);
        this.protocolClient = new ProtocolClient(bufferedIn, out);
        this.context = new CommandContext(protocolClient);
        
        // Set up auto-download callback for notifications
        protocolClient.setOnFileUploadedCallback(fileName -> {
            System.out.println("\n[Notification] File uploaded: " + fileName);
            System.out.println("[Auto-download] Downloading missing files...");
            try {
                DownloadHandler downloadHandler = new DownloadHandler();
                downloadHandler.execute(context, new String[0]);
            } catch (Exception e) {
                System.err.println("Error during auto-download: " + e.getMessage());
            }
            System.out.print("> "); // Restore prompt
        });
        
        // Start notification listener
        protocolClient.startNotificationListener();
    }

    public void run() {
        System.out.println("Connected to server");
        System.out.println("Client files directory: " + com.room.config.ClientConfig.getClientFilesDir());
        System.out.println("Type 'help' for available commands");

        Scanner scanner = new Scanner(System.in);

        while (true) {
            System.out.print("> ");
            if (!scanner.hasNextLine()) {
                break;
            }

            String line = scanner.nextLine().trim();
            if (line.isEmpty()) {
                continue;
            }

            String[] parts = line.split("\\s+");
            if (parts.length == 0) {
                continue;
            }

            String cmd = parts[0];

            try {
                var handler = commandRegistry.get(cmd);
                if (handler != null) {
                    handler.execute(context, parts);
                } else {
                    System.out.println("Unknown command: " + cmd + ". Type 'help' for available commands");
                }
            } catch (Exception e) {
                System.out.println("Error: " + e.getMessage());
                e.printStackTrace();
            }
        }
    }
}

