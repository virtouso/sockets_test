package com.room.protocol;

import java.io.*;
import java.util.ArrayList;
import java.util.List;
import java.util.concurrent.atomic.AtomicBoolean;
import java.util.function.Consumer;

public class ProtocolClient {
    private final InputStream in;
    private final OutputStream out;
    private final AtomicBoolean notificationListenerRunning = new AtomicBoolean(false);
    private final AtomicBoolean downloadInProgress = new AtomicBoolean(false);
    private Thread notificationThread;
    private Consumer<String> onFileUploadedCallback;
    private final Object readLock = new Object();

    public ProtocolClient(InputStream in, OutputStream out) {
        this.in = in;
        this.out = out;
    }

    public void auth(String user, String pass) throws IOException {
        out.write(Protocol.CMD_AUTH);

        byte[] u = user.getBytes();
        ProtocolWriter.writeInt(out, u.length);
        out.write(u);

        byte[] p = pass.getBytes();
        ProtocolWriter.writeInt(out, p.length);
        out.write(p);

        synchronized (readLock) {
            int result = in.read();
            if (result == -1 || result != 1) {
                throw new IOException("Authentication failed");
            }
        }
    }

    public List<String> listFiles() throws IOException {
        out.write(Protocol.CMD_LISTFILES);

        synchronized (readLock) {
            int count = ProtocolReader.readInt(in);
            List<String> files = new ArrayList<>(count);

            for (int i = 0; i < count; i++) {
                int len = ProtocolReader.readInt(in);
                byte[] nameBytes = ProtocolReader.readBytes(in, len);
                files.add(new String(nameBytes));
            }

            return files;
        }
    }

    public void downloadFile(String filename, String savePath) throws IOException {
        // Set download flag to prevent notification interference
        downloadInProgress.set(true);
        
        // Wait longer to ensure notification listener has seen the flag and completely stopped
        // This is critical - notification listener must not touch stream during downloads
        try {
            Thread.sleep(300); // Longer wait to ensure notification listener has fully stopped
        } catch (InterruptedException e) {
            Thread.currentThread().interrupt();
        }
        
        try {
            out.write(Protocol.CMD_GETFILE);

            byte[] nameBytes = filename.getBytes();
            ProtocolWriter.writeInt(out, nameBytes.length);
            out.write(nameBytes);

            synchronized (readLock) {
                // Double-check notification listener is not interfering
                // This ensures we have exclusive access to the stream
                long size = ProtocolReader.readInt64(in);
                
                if (size < 0) {
                    throw new IOException("File not found on server: " + filename);
                }

                try (FileOutputStream fos = new FileOutputStream(savePath)) {
                    // Read file data in chunks using ProtocolReader for reliable reading
                    long remaining = size;
                    byte[] chunkBuffer = new byte[8192];
                    long totalReceived = 0;
                    
                    while (remaining > 0) {
                        // Check download flag is still set (should be, but verify)
                        if (!downloadInProgress.get()) {
                            throw new IOException("Download interrupted - flag was cleared unexpectedly");
                        }
                        
                        int chunkSize = (int) Math.min(chunkBuffer.length, remaining);
                        
                        // Read exactly chunkSize bytes
                        int totalRead = 0;
                        while (totalRead < chunkSize) {
                            int bytesRead = in.read(chunkBuffer, totalRead, chunkSize - totalRead);
                            if (bytesRead == -1) {
                                throw new IOException("Unexpected end of stream. Expected " + size + " bytes, received " + totalReceived);
                            }
                            totalRead += bytesRead;
                            totalReceived += bytesRead;
                        }
                        
                        fos.write(chunkBuffer, 0, totalRead);
                        fos.flush(); // Ensure data is written to disk immediately
                        remaining -= totalRead;
                    }
                    
                    // Final flush to ensure all data is written
                    fos.flush();
                }
            }
        } finally {
            // Always clear the download flag
            downloadInProgress.set(false);
        }
    }

    public void uploadFile(String filename) throws IOException {
        File file = new File(filename);
        if (!file.exists() || !file.isFile()) {
            throw new IOException("File not found: " + filename);
        }

        out.write(Protocol.CMD_PUTFILE);

        byte[] nameBytes = file.getName().getBytes();
        ProtocolWriter.writeInt(out, nameBytes.length);
        out.write(nameBytes);

        ProtocolWriter.writeLong(out, file.length());

        try (FileInputStream fis = new FileInputStream(file)) {
            byte[] buffer = new byte[8192];
            int read;
            while ((read = fis.read(buffer)) > 0) {
                out.write(buffer, 0, read);
            }
        }
    }

    public boolean ping() throws IOException {
        out.write(Protocol.CMD_PING);
        synchronized (readLock) {
            int r = in.read();
            if (r == -1) {
                throw new IOException("connection closed");
            }
            return r == 1;
        }
    }

    public void setOnFileUploadedCallback(Consumer<String> callback) {
        this.onFileUploadedCallback = callback;
    }

    public void startNotificationListener() {
        if (notificationListenerRunning.compareAndSet(false, true)) {
            notificationThread = new Thread(() -> {
                try {
                    while (notificationListenerRunning.get()) {
                        // Completely skip all operations if download is in progress
                        // Wait longer when download is active to avoid any interference
                        if (downloadInProgress.get()) {
                            Thread.sleep(500); // Longer sleep when download is active - completely pause
                            continue;
                        }
                        
                        // Check download status BEFORE entering synchronized block
                        // This prevents any stream operations when downloads are active
                        if (downloadInProgress.get()) {
                            continue;
                        }
                        
                        java.util.List<String> notifications = new java.util.ArrayList<>();
                        synchronized (readLock) {
                            // Quadruple-check download status inside lock - must not be downloading
                            if (downloadInProgress.get()) {
                                continue;
                            }
                            
                            // Only check if there's data available and download is not in progress
                            // Check again after acquiring lock
                            if (downloadInProgress.get()) {
                                continue;
                            }
                            
                            if (in.available() == 0) {
                                continue; // No data available, skip
                            }
                            
                            // Final check before any stream operations
                            if (downloadInProgress.get()) {
                                continue;
                            }
                            
                            // Process ALL available notifications in a loop
                            // But stop immediately if download starts
                            while (in.available() > 0 && !downloadInProgress.get()) {
                                // Check download status BEFORE any stream operations
                                if (downloadInProgress.get()) {
                                    break; // Download started, abort immediately
                                }
                                
                                // Peek at the next byte without consuming it
                                // Use larger mark limit (64KB) to match buffer size
                                // But check again after mark() in case download started
                                in.mark(65536);
                                
                                // Check again after mark() - download might have started
                                if (downloadInProgress.get()) {
                                    try {
                                        in.reset(); // Reset before aborting
                                    } catch (IOException e) {
                                        // Reset failed, stream might be corrupted - abort
                                    }
                                    break;
                                }
                                
                                int cmd = in.read();
                                
                                // Check again before reset()
                                if (downloadInProgress.get()) {
                                    // Can't reset safely, abort
                                    break;
                                }
                                
                                try {
                                    in.reset();
                                } catch (IOException e) {
                                    // Reset failed - might have read past mark limit
                                    // This means it's not a notification, abort
                                    break;
                                }
                                
                                if (cmd != Protocol.CMD_FILE_UPLOADED) {
                                    break; // Not a notification, stop processing
                                }
                                
                                // Final check before reading notification data
                                if (downloadInProgress.get()) {
                                    break; // Download started, abort immediately
                                }
                                
                                // Read the notification
                                in.read(); // consume the command byte
                                int nameLen = ProtocolReader.readInt(in);
                                if (nameLen < 0 || nameLen > 1024 * 1024) {
                                    break; // Invalid length
                                }
                                byte[] nameBytes = ProtocolReader.readBytes(in, nameLen);
                                notifications.add(new String(nameBytes));
                            }
                        }
                        // Process notifications outside lock to avoid deadlock
                        // Execute callbacks in a separate thread to avoid blocking notification listener
                        if (!notifications.isEmpty() && onFileUploadedCallback != null) {
                            final java.util.List<String> notificationsCopy = new java.util.ArrayList<>(notifications);
                            new Thread(() -> {
                                for (String fileName : notificationsCopy) {
                                    try {
                                        onFileUploadedCallback.accept(fileName);
                                    } catch (Exception e) {
                                        System.err.println("Error in notification callback: " + e.getMessage());
                                        e.printStackTrace();
                                    }
                                }
                            }).start();
                        }
                        Thread.sleep(100); // small delay to avoid busy waiting
                    }
                } catch (IOException e) {
                    // connection closed or error
                    notificationListenerRunning.set(false);
                } catch (InterruptedException e) {
                    Thread.currentThread().interrupt();
                    notificationListenerRunning.set(false);
                }
            });
            notificationThread.setDaemon(true);
            notificationThread.start();
        }
    }

    public void stopNotificationListener() {
        if (notificationListenerRunning.compareAndSet(true, false)) {
            if (notificationThread != null) {
                notificationThread.interrupt();
            }
        }
    }
}

