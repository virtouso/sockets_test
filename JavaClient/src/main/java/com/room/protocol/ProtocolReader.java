package com.room.protocol;

import java.io.IOException;
import java.io.InputStream;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;

public class ProtocolReader {
    public static int readInt(InputStream in) throws IOException {
        byte[] b = readBytes(in, 4);
        ByteBuffer buffer = ByteBuffer.wrap(b).order(ByteOrder.LITTLE_ENDIAN);
        return buffer.getInt();
    }

    public static long readInt64(InputStream in) throws IOException {
        byte[] b = readBytes(in, 8);
        ByteBuffer buffer = ByteBuffer.wrap(b).order(ByteOrder.LITTLE_ENDIAN);
        return buffer.getLong();
    }

    public static byte[] readBytes(InputStream in, int length) throws IOException {
        byte[] buffer = new byte[length];
        int read, total = 0;
        while (total < length) {
            read = in.read(buffer, total, length - total);
            if (read == -1) {
                throw new IOException("Stream closed before reading enough bytes");
            }
            total += read;
        }
        return buffer;
    }
}






