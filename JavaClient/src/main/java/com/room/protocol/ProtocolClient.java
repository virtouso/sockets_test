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
        out.write(Protocol.CMD_GETFILE);

        byte[] nameBytes = filename.getBytes();
        ProtocolWriter.writeInt(out, nameBytes.length);
        out.write(nameBytes);

        synchronized (readLock) {
            long size = ProtocolReader.readInt64(in);

            try (FileOutputStream fos = new FileOutputStream(savePath)) {
                byte[] buffer = new byte[8192];
                long received = 0;

                while (received < size) {
                    int read = in.read(buffer);
                    if (read == -1) throw new IOException("Unexpected end of stream");
                    fos.write(buffer, 0, read);
                    received += read;
                }
            }
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
                        synchronized (readLock) {
                            // Check if data is available
                            if (in.available() > 0) {
                                // Peek at the next byte without consuming it
                                in.mark(1024); // Mark enough to read a notification
                                int cmd = in.read();
                                in.reset();
                                
                                if (cmd == Protocol.CMD_FILE_UPLOADED) {
                                    // Read the notification
                                    in.read(); // consume the command byte
                                    int nameLen = ProtocolReader.readInt(in);
                                    byte[] nameBytes = ProtocolReader.readBytes(in, nameLen);
                                    String fileName = new String(nameBytes);
                                    
                                    if (onFileUploadedCallback != null) {
                                        onFileUploadedCallback.accept(fileName);
                                    }
                                }
                            }
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

