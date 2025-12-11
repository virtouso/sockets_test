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
    private volatile boolean autoDownloadInProgress = false;

    public Client(InputStream in, OutputStream out) {
        this.commandRegistry = new CommandRegistry();
        // Wrap input stream with BufferedInputStream to support mark/reset for notification listening
        // Use larger buffer (64KB) to ensure mark/reset works reliably
        BufferedInputStream bufferedIn = new BufferedInputStream(in, 65536);
        this.protocolClient = new ProtocolClient(bufferedIn, out);
        this.context = new CommandContext(protocolClient);
        
        // Set up auto-download callback for notifications
        // Download ALL missing files when a notification arrives (same as download command)
        protocolClient.setOnFileUploadedCallback(fileName -> {
            System.out.println("\n[Notification] File uploaded: " + fileName);
            
            // Prevent concurrent auto-downloads
            if (autoDownloadInProgress) {
                System.out.println("[Auto-download] Already downloading, queuing notification for: " + fileName);
                return;
            }
            
            // Execute downloads in a separate thread using the exact same mechanism as download command
            new Thread(() -> {
                autoDownloadInProgress = true;
                try {
                    // Wait longer to ensure notification listener has fully released all locks
                    // This is critical to prevent interference
                    Thread.sleep(500);
                    
                    System.out.println("[Auto-download] Downloading all missing files...");
                    
                    // Use the exact same code path as typing "download" command
                    // This downloads ALL missing files, not just the one from notification
                    var handler = commandRegistry.get("download");
                    if (handler != null) {
                        handler.execute(context, new String[]{"download"});
                    }
                } catch (Exception e) {
                    System.err.println("[Auto-download] Error: " + e.getMessage());
                    e.printStackTrace();
                } finally {
                    autoDownloadInProgress = false;
                    System.out.print("> "); // Restore prompt
                }
            }).start();
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

